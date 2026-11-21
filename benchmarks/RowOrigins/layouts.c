#include <immintrin.h>
#include <omp.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#define TIMELINES 500
#define BLOCK 64
#define INLINE_SLOTS 4
static int PAD = 0;
static int16_t *timelineOfRow;

typedef struct { uint16_t timeline, tick; uint8_t channel, flags, rate, phase; } slot8_t;
typedef struct { uint16_t timeline, tick; } slot4_t;
typedef struct { uint32_t head; float speed; int32_t prio; float lift; int32_t damage; int32_t stat; int32_t end; int32_t pad; } fused_t;
typedef struct { float x, y; } motion_t;
typedef struct { int32_t health, stamina; } vitals_t;
typedef struct { float walk, jump; } stats_t;

static double now_ms(void) { struct timespec t; clock_gettime(CLOCK_MONOTONIC, &t); return t.tv_sec * 1e3 + t.tv_nsec / 1e6; }
static uint64_t rng = 0x9E3779B97F4A7C15ull;
static uint32_t rnd(void) { rng ^= rng << 13; rng ^= rng >> 7; rng ^= rng << 17; return (uint32_t)rng; }
static int cmpd(const void *a, const void *b) { double x = *(const double *)a, y = *(const double *)b; return x < y ? -1 : x > y; }

static int E, A, THREADS;
static int duration[TIMELINES], rowBase[TIMELINES], blockBase[TIMELINES], pulseBase[TIMELINES];
static fused_t *rows;
static fused_t *blockRows;
static uint8_t *pulseBits;
static int32_t *pulseVal;
static slot8_t **slot8;
static slot8_t *inline8;
static slot4_t *slot4;
static uint16_t *origin;
static motion_t *motion;
static vitals_t *vitals;
static stats_t *stats;
static int32_t *pulseTotal;
static uint32_t frame;

static void build(void)
{
    int total = 0;
    for (int t = 0; t < TIMELINES; t++) { duration[t] = 30 + (t * 7) % 31; rowBase[t] = total; blockBase[t] = PAD ? t * BLOCK : total; total += duration[t]; }
    rows = aligned_alloc(64, sizeof(fused_t) * (size_t)total);
    blockRows = aligned_alloc(64, sizeof(fused_t) * (size_t)TIMELINES * BLOCK);
    memset(blockRows, 0, sizeof(fused_t) * (size_t)TIMELINES * BLOCK);
    pulseBits = calloc(total, 1);
    pulseVal = calloc(total, 4);
    timelineOfRow = calloc(65536, 2);
    for (int t = 0; t < TIMELINES; t++) for (int k = 0; k < duration[t]; k++) timelineOfRow[blockBase[t] + k] = t;
    for (int t = 0; t < TIMELINES; t++)
    {
        pulseBase[t] = rowBase[t];
        for (int k = 0; k < duration[t]; k++)
        {
            fused_t r = {0};
            if (rnd() % 3) { r.head |= 1; r.speed = (float)(rnd() % 64) * 0.25f; r.prio = rnd() % 4; }
            if (rnd() % 2) { r.head |= 2; r.lift = (float)((int)(rnd() % 33) - 16) * 0.125f + 0.5f; }
            r.stat = (rnd() % 5 == 0) ? (int)(rnd() % 7) - 3 : 0;
            if (rnd() % 8 == 0) { pulseBits[rowBase[t] + k] = 1; pulseVal[rowBase[t] + k] = 1 + rnd() % 9; r.damage = pulseVal[rowBase[t] + k]; }
            r.end = k == duration[t] - 1;
            rows[rowBase[t] + k] = r;
            fused_t b = r;
            b.prio = (r.head & 1) ? (r.prio << 8) : -1;
            if (!(r.head & 2)) b.lift = 0.0f;
            if (!(r.head & 1)) b.speed = 0.0f;
            blockRows[blockBase[t] + k] = b;
        }
    }
}

static void seed(void)
{
    rng = 0x1234567ull;
    for (int e = 0; e < E; e++)
    {
        motion[e] = (motion_t){0, 0};
        vitals[e] = (vitals_t){1000000, 0};
        stats[e] = (stats_t){1.0f + (e % 7) * 0.5f, 2.0f + (e % 3)};
        for (int a = 0; a < A; a++)
        {
            int t = rnd() % TIMELINES;
            int s = rnd() % (duration[t] - 1);
            slot8_t *p = A <= INLINE_SLOTS ? inline8 + (size_t)e * INLINE_SLOTS + a : slot8[e] + a;
            *p = (slot8_t){(uint16_t)(t + 1), (uint16_t)s, 0, 0, 0, 0};
            slot4[(size_t)e * A + a] = (slot4_t){(uint16_t)(t + 1), (uint16_t)s};
            origin[(size_t)e * A + a] = (uint16_t)(blockBase[t] + s - frame);
        }
    }
}

static inline slot8_t *slots8_of(int e) { return A <= INLINE_SLOTS ? inline8 + (size_t)e * INLINE_SLOTS : slot8[e]; }

static inline int step_slot(uint16_t *timeline, uint16_t *tick)
{
    int id = *timeline;
    int t = *tick + 1;
    if (t >= duration[id - 1]) t = 0;
    *tick = (uint16_t)t;
    return t;
}

static inline void accumulate(int id, int tick, float *speed, int *prio, float *lift)
{
    const fused_t *r = rows + rowBase[id - 1] + tick;
    uint32_t h = r->head;
    if ((h & 1) && r->prio >= *prio) { *speed = r->speed; *prio = r->prio; }
    if (h & 2) *lift += r->lift;
}

static inline int pulse_at(int id, int tick)
{
    int pb = pulseBase[id - 1] + tick;
    return pulseBits[pb] ? pulseVal[pb] : 0;
}

static inline void apply(int e, float speed, float lift, int damage, int stat)
{
    motion[e].x += speed * stats[e].walk;
    motion[e].y += lift * stats[e].jump;
    vitals[e].health -= damage;
    vitals[e].stamina += stat;
}

static void today_two_pass(void)
{
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        slot8_t *s = slots8_of(e);
        int dmg = 0;
        for (int a = 0; a < A; a++)
        {
            if (s[a].timeline == 0 || s[a].flags || s[a].rate) continue;
            int t = step_slot(&s[a].timeline, &s[a].tick);
            dmg += pulse_at(s[a].timeline, t);
        }
        pulseTotal[e] = dmg;
    }
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        slot8_t *s = slots8_of(e);
        float speed = 0, lift = 0; int prio = 0, stat = 0;
        for (int a = 0; a < A; a++)
        {
            if (s[a].timeline == 0) continue;
            accumulate(s[a].timeline, s[a].tick, &speed, &prio, &lift);
            stat += rows[rowBase[s[a].timeline - 1] + s[a].tick].stat;
        }
        apply(e, speed, lift, 0, stat);
    }
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++) vitals[e].health -= pulseTotal[e];
}

static void today_fused(void)
{
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        slot8_t *s = slots8_of(e);
        float speed = 0, lift = 0; int prio = 0, stat = 0, dmg = 0;
        for (int a = 0; a < A; a++)
        {
            if (s[a].timeline == 0 || s[a].flags || s[a].rate) continue;
            int t = step_slot(&s[a].timeline, &s[a].tick);
            dmg += pulse_at(s[a].timeline, t);
            accumulate(s[a].timeline, t, &speed, &prio, &lift);
            stat += rows[rowBase[s[a].timeline - 1] + t].stat;
        }
        apply(e, speed, lift, dmg, stat);
    }
}

static void four_two_pass(void)
{
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        slot4_t *s = slot4 + (size_t)e * A;
        int dmg = 0;
        for (int a = 0; a < A; a++) { int t = step_slot(&s[a].timeline, &s[a].tick); dmg += pulse_at(s[a].timeline, t); }
        pulseTotal[e] = dmg;
    }
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        slot4_t *s = slot4 + (size_t)e * A;
        float speed = 0, lift = 0; int prio = 0, stat = 0;
        for (int a = 0; a < A; a++)
        {
            accumulate(s[a].timeline, s[a].tick, &speed, &prio, &lift);
            stat += rows[rowBase[s[a].timeline - 1] + s[a].tick].stat;
        }
        apply(e, speed, lift, pulseTotal[e], stat);
    }
}

static void four_fused(void)
{
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        slot4_t *s = slot4 + (size_t)e * A;
        float speed = 0, lift = 0; int prio = 0, stat = 0, dmg = 0;
        for (int a = 0; a < A; a++)
        {
            int t = step_slot(&s[a].timeline, &s[a].tick);
            const fused_t *r = rows + rowBase[s[a].timeline - 1] + t;
            if ((r->head & 1) && r->prio >= prio) { speed = r->speed; prio = r->prio; }
            if (r->head & 2) lift += r->lift;
            dmg += r->damage;
            stat += r->stat;
        }
        apply(e, speed, lift, dmg, stat);
    }
}

static void relative_scalar(void)
{
    uint32_t f = frame;
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        uint16_t *o = origin + (size_t)e * A;
        float lift = 0; int key = -1, bestRow = -1, dmg = 0, stat = 0;
        for (int a = 0; a < A; a++)
        {
            int index = (uint16_t)(o[a] + f);
            const fused_t *r = blockRows + index;
            if (r->prio >= 0 && (r->prio | a) > key) { key = r->prio | a; bestRow = index; }
            lift += r->lift; dmg += r->damage; stat += r->stat;
            if (r->end) o[a] = (uint16_t)(o[a] - duration[timelineOfRow[index]]);
        }
        float speed = bestRow >= 0 ? blockRows[bestRow].speed : 0;
        apply(e, speed, lift, dmg, stat);
    }
}

static void relative_simd(void)
{
    uint32_t f = frame;
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        uint16_t *o = origin + (size_t)e * A;
        __m256 fsum = _mm256_setzero_ps();
        __m256i isum = _mm256_setzero_si256();
        __m256i best = _mm256_set1_epi32(-1);
        __m256i lane = _mm256_setr_epi32(0, 0, 1, 0, 0, 0, 0, 0);
        uint64_t ended[4] = {0, 0, 0, 0};
        int bestRow = -1;
        for (int a = 0; a < A; a++)
        {
            int index = (uint16_t)(o[a] + f);
            __m256i v = _mm256_load_si256((const __m256i *)(blockRows + index));
            fsum = _mm256_add_ps(fsum, _mm256_castsi256_ps(v));
            isum = _mm256_add_epi32(isum, v);
            __m256i k = _mm256_or_si256(v, _mm256_and_si256(_mm256_set1_epi32(a), _mm256_cmpgt_epi32(v, _mm256_set1_epi32(-1))));
            __m256i nb = _mm256_max_epi32(best, k);
            int took = _mm256_movemask_ps(_mm256_castsi256_ps(_mm256_cmpgt_epi32(nb, best))) & 4;
            bestRow = took ? index : bestRow;
            best = nb;
            ended[a >> 6] |= (uint64_t)(blockRows[index].end != 0) << (a & 63);
        }
        (void)lane;
        float lanesF[8]; int32_t lanesI[8];
        _mm256_storeu_ps(lanesF, fsum); _mm256_storeu_si256((__m256i *)lanesI, isum);
        float speed = bestRow >= 0 ? blockRows[bestRow].speed : 0;
        apply(e, speed, lanesF[3], lanesI[4], lanesI[5]);
        for (int w = 0; w < 4; w++)
            for (uint64_t m = ended[w]; m; m &= m - 1)
            {
                int a = w * 64 + __builtin_ctzll(m);
                o[a] = (uint16_t)(o[a] - duration[timelineOfRow[(uint16_t)(o[a] + f)]]);
            }
    }
}


static void relative_lean(void)
{
    uint32_t f = frame;
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        uint16_t *o = origin + (size_t)e * A;
        __m256 fsum = _mm256_setzero_ps();
        __m256i isum = _mm256_setzero_si256();
        __m256i best = _mm256_set1_epi32(-1);
        uint64_t ended[4] = {0, 0, 0, 0};
        for (int a = 0; a < A; a++)
        {
            int index = (uint16_t)(o[a] + f);
            const fused_t *r = blockRows + index;
            __m256i v = _mm256_load_si256((const __m256i *)r);
            fsum = _mm256_add_ps(fsum, _mm256_castsi256_ps(v));
            isum = _mm256_add_epi32(isum, v);
            best = _mm256_max_epi32(best, _mm256_or_si256(v, _mm256_set1_epi32(a)));
            ended[a >> 6] |= (uint64_t)(uint32_t)r->end << (a & 63);
        }
        float lanesF[8]; int32_t lanesI[8], keys[8];
        _mm256_storeu_ps(lanesF, fsum); _mm256_storeu_si256((__m256i *)lanesI, isum); _mm256_storeu_si256((__m256i *)keys, best);
        float speed = keys[2] >= 0 ? blockRows[(uint16_t)(o[keys[2] & 255] + f)].speed : 0;
        apply(e, speed, lanesF[3], lanesI[4], lanesI[5]);
        for (int w = 0; w < 4; w++)
            for (uint64_t m = ended[w]; m; m &= m - 1)
            {
                int a = w * 64 + __builtin_ctzll(m);
                o[a] = (uint16_t)(o[a] - duration[timelineOfRow[(uint16_t)(o[a] + f)]]);
            }
    }
}

static void relative_read_only(void)
{
    uint32_t f = frame;
#pragma omp parallel for num_threads(THREADS) schedule(static, 128)
    for (int e = 0; e < E; e++)
    {
        uint16_t *o = origin + (size_t)e * A;
        __m256 fsum = _mm256_setzero_ps();
        __m256i isum = _mm256_setzero_si256();
        __m256i best = _mm256_set1_epi32(-1);
        for (int a = 0; a < A; a++)
        {
            __m256i v = _mm256_load_si256((const __m256i *)(blockRows + (uint16_t)(o[a] + f)));
            fsum = _mm256_add_ps(fsum, _mm256_castsi256_ps(v));
            isum = _mm256_add_epi32(isum, v);
            best = _mm256_max_epi32(best, v);
        }
        float lanesF[8]; int32_t lanesI[8], keys[8];
        _mm256_storeu_ps(lanesF, fsum); _mm256_storeu_si256((__m256i *)lanesI, isum); _mm256_storeu_si256((__m256i *)keys, best);
        apply(e, (float)keys[2], lanesF[3], lanesI[4], lanesI[5]);
    }
}

static uint64_t checksum(void)
{
    uint64_t h = 1469598103934665603ull;
    for (int e = 0; e < E; e++)
    {
        uint32_t w[4];
        memcpy(w, &motion[e], 8); memcpy(w + 2, &vitals[e], 8);
        for (int i = 0; i < 4; i++) h = (h ^ w[i]) * 1099511628211ull;
    }
    return h;
}

typedef void (*kernel_t)(void);

int main(int argc, char **argv)
{
    E = 50000; A = 100; THREADS = omp_get_max_threads();
    int steps = 40, warm = 10;
    const char *only = NULL;
    for (int i = 1; i + 1 < argc; i += 2)
    {
        if (!strcmp(argv[i], "-e")) E = atoi(argv[i + 1]);
        if (!strcmp(argv[i], "-a")) A = atoi(argv[i + 1]);
        if (!strcmp(argv[i], "-t")) THREADS = atoi(argv[i + 1]);
        if (!strcmp(argv[i], "-k")) only = argv[i + 1];
        if (!strcmp(argv[i], "-s")) steps = atoi(argv[i + 1]);
        if (!strcmp(argv[i], "-pad")) PAD = atoi(argv[i + 1]);
    }
    build();
    motion = aligned_alloc(64, sizeof(motion_t) * (size_t)E);
    vitals = aligned_alloc(64, sizeof(vitals_t) * (size_t)E);
    stats = aligned_alloc(64, sizeof(stats_t) * (size_t)E);
    pulseTotal = aligned_alloc(64, 4 * (size_t)E + 64);
    slot4 = aligned_alloc(64, sizeof(slot4_t) * (size_t)E * A + 64);
    origin = aligned_alloc(64, 2 * (size_t)E * A + 64);
    if (A <= INLINE_SLOTS) inline8 = aligned_alloc(64, sizeof(slot8_t) * (size_t)E * INLINE_SLOTS);
    else { slot8 = malloc(sizeof(void *) * (size_t)E); for (int e = 0; e < E; e++) slot8[e] = malloc(sizeof(slot8_t) * A); }
    const char *names[] = {"today-8B-two-pass", "8B-fused", "4B-two-pass", "4B-fused", "2B-relative-scalar", "2B-relative-simd", "2B-lean-simd", "2B-read-only-floor"};
    kernel_t kernels[] = {today_two_pass, today_fused, four_two_pass, four_fused, relative_scalar, relative_simd, relative_lean, relative_read_only};
    int bytes[] = {8, 8, 4, 4, 2, 2, 2, 2};
    double *samples = malloc(sizeof(double) * steps);
    for (int k = 0; k < 8; k++)
    {
        if (only && !strstr(names[k], only)) continue;
        frame = 1000;
        seed();
        for (int s = 0; s < warm + steps; s++)
        {
            frame++;
            double t0 = now_ms();
            kernels[k]();
            double t1 = now_ms();
            if (s >= warm) samples[s - warm] = t1 - t0;
        }
        qsort(samples, steps, sizeof(double), cmpd);
        double slotMB = (double)E * A * bytes[k] / 1e6;
        printf("%-20s entities=%d active=%d threads=%d stepMs=%.3f minMs=%.3f nsPerSlot=%.3f slotMB=%.1f hash=%016llx\n",
               names[k], E, A, THREADS, samples[steps / 2], samples[0], samples[steps / 2] * 1e6 / ((double)E * A), slotMB, (unsigned long long)checksum());
    }
    return 0;
}
