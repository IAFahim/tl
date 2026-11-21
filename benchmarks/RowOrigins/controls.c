#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define T 300
#define MAXS 32
#define NULLROW 0

typedef struct { float speed; int32_t prio; float lift; int32_t damage; int32_t stat; int32_t end; } row_t;
typedef struct { int timeline, tick, reverse, paused, rate2, alive; } ref_slot;
typedef struct { uint16_t origin; uint8_t side, ended; int timeline, tick, reverse, paused, rate2, alive; } org_slot;
typedef struct { float speed, lift; int32_t damage, stat; int key; } totals_t;

static uint64_t rng = 0xC0FFEE1234ull;
static uint32_t rnd(void) { rng ^= rng << 13; rng ^= rng >> 7; rng ^= rng << 17; return (uint32_t)rng; }

static int dur[T], next_tl[T], base[T], N;
static row_t *values[2];
static row_t *table[2];
static int16_t *rowTimeline;

static row_t value_of(int t, int k, int dir) { return values[dir > 0 ? 0 : 1][base[t] + k]; }
static int layout_index(int t, int k, int rev) { return 1 + (rev ? N + base[t] + (dur[t] - 1 - k) : base[t] + k); }
static void logical_of(int index, int *t, int *k, int *rev)
{
    int i = index - 1;
    *rev = i >= N;
    if (*rev) i -= N;
    *t = rowTimeline[i];
    *k = *rev ? dur[*t] - 1 - (i - base[*t]) : i - base[*t];
}

static void build(void)
{
    N = 0;
    for (int t = 0; t < T; t++) { dur[t] = 3 + (int)(rnd() % 58); base[t] = N; N += dur[t]; next_tl[t] = rnd() % 3 ? t : (int)(rnd() % T); }
    if (1 + 2 * N > 65536) { fprintf(stderr, "rows exceed u16\n"); exit(1); }
    rowTimeline = malloc(sizeof(int16_t) * N);
    for (int t = 0; t < T; t++) for (int k = 0; k < dur[t]; k++) rowTimeline[base[t] + k] = (int16_t)t;
    for (int d = 0; d < 2; d++)
    {
        values[d] = calloc(N, sizeof(row_t));
        for (int i = 0; i < N; i++)
        {
            row_t r = {0};
            r.prio = -1;
            if (rnd() % 3) { r.speed = (float)(rnd() % 64) * 0.25f; r.prio = (int)(rnd() % 4); }
            if (rnd() % 2) r.lift = (float)((int)(rnd() % 33) - 16) * 0.37f + 0.11f;
            if (rnd() % 6 == 0) r.damage = 1 + (int)(rnd() % 9);
            if (rnd() % 5 == 0) r.stat = (int)(rnd() % 7) - 3;
            values[d][i] = r;
        }
    }
    for (int w = 0; w < 2; w++)
    {
        table[w] = calloc(1 + 2 * N, sizeof(row_t));
        table[w][NULLROW].prio = -1;
        for (int t = 0; t < T; t++)
            for (int k = 0; k < dur[t]; k++)
                for (int rev = 0; rev < 2; rev++)
                {
                    int worldDir = w == 0 ? 1 : -1;
                    int dir = rev ? -worldDir : worldDir;
                    row_t r = value_of(t, k, dir);
                    r.end = dir > 0 ? k == dur[t] - 1 : k == 0;
                    table[w][layout_index(t, k, rev)] = r;
                }
    }
}

static void contribute(totals_t *x, row_t r, int seq)
{
    if (r.prio >= 0 && ((r.prio << 10) | seq) > x->key) { x->key = (r.prio << 10) | seq; x->speed = r.speed; }
    x->lift += r.lift;
    x->damage += r.damage;
    x->stat += r.stat;
}

static void advance(int *t, int *k, int dir)
{
    int n = *k + dir;
    if (n >= 0 && n < dur[*t]) { *k = n; return; }
    *t = next_tl[*t];
    *k = dir > 0 ? 0 : dur[*t] - 1;
}

static totals_t ref_step(ref_slot *s, int n, int w)
{
    totals_t x = {0, 0, 0, 0, -1};
    int seq = 0;
    for (int i = 0; i < n; i++)
        if (s[i].alive && s[i].rate2 && !s[i].paused)
        {
            int dir = s[i].reverse ? -w : w;
            for (int c = 0; c < 2; c++) { advance(&s[i].timeline, &s[i].tick, dir); contribute(&x, value_of(s[i].timeline, s[i].tick, dir), seq++); }
        }
    for (int i = 0; i < n; i++)
        if (s[i].alive && !s[i].rate2 && !s[i].paused)
        {
            int dir = s[i].reverse ? -w : w;
            advance(&s[i].timeline, &s[i].tick, dir);
            contribute(&x, value_of(s[i].timeline, s[i].tick, dir), 256 + i);
        }
    return x;
}

static int wslot(int w) { return w > 0 ? 0 : 1; }

static void to_side(org_slot *o, uint16_t F)
{
    if (o->side) return;
    logical_of((uint16_t)(o->origin + F), &o->timeline, &o->tick, &o->reverse);
    o->side = 1;
    o->origin = (uint16_t)(NULLROW - F);
}

static void to_dense(org_slot *o, uint16_t F, int w)
{
    if (!o->side || !o->alive || o->paused || o->rate2) return;
    int index = layout_index(o->timeline, o->tick, o->reverse);
    o->origin = (uint16_t)(index - F);
    o->ended = (uint8_t)table[wslot(w)][index].end;
    o->side = 0;
}


static totals_t org_step(org_slot *s, int n, uint16_t F0, int w)
{
    uint16_t Fv = F0;
    uint16_t *F = &Fv;
    for (int i = 0; i < n; i++)
    {
        if (s[i].side || !s[i].ended) continue;
        int t, k, rev;
        logical_of((uint16_t)(s[i].origin + *F), &t, &k, &rev);
        int dir = rev ? -w : w;
        advance(&t, &k, dir);
        s[i].origin = (uint16_t)(layout_index(t, k, rev) - (uint16_t)(*F + w));
        s[i].ended = 0;
    }
    *F = (uint16_t)(*F + w);
    for (int i = 0; i < n; i++) if (s[i].side) s[i].origin = (uint16_t)(NULLROW - *F);
    totals_t x = {0, 0, 0, 0, -1};
    int seq = 0;
    for (int i = 0; i < n; i++)
        if (s[i].side && s[i].alive && s[i].rate2 && !s[i].paused)
        {
            int dir = s[i].reverse ? -w : w;
            for (int c = 0; c < 2; c++) { advance(&s[i].timeline, &s[i].tick, dir); contribute(&x, value_of(s[i].timeline, s[i].tick, dir), seq++); }
        }
    const row_t *tab = table[wslot(w)];
    for (int i = 0; i < n; i++)
    {
        int index = (uint16_t)(s[i].origin + *F);
        row_t r = tab[index];
        contribute(&x, r, 256 + i);
        s[i].ended = (uint8_t)r.end;
    }
    return x;
}

int main(int argc, char **argv)
{
    int entities = argc > 1 ? atoi(argv[1]) : 1000;
    int steps = argc > 2 ? atoi(argv[2]) : 3000;
    build();
    ref_slot (*rs)[MAXS] = calloc(entities, sizeof *rs);
    org_slot (*os)[MAXS] = calloc(entities, sizeof *os);
    int *count = calloc(entities, sizeof(int));
    uint16_t F = 40000;
    int w = 1;
    for (int e = 0; e < entities; e++)
    {
        count[e] = 1 + rnd() % 24;
        for (int i = 0; i < count[e]; i++)
        {
            int t = rnd() % T, k = rnd() % dur[t], rev = rnd() % 4 == 0;
            rs[e][i] = (ref_slot){t, k, rev, 0, 0, 1};
            os[e][i] = (org_slot){0, 1, 0, t, k, rev, 0, 0, 1};
            to_dense(&os[e][i], F, w);
        }
    }
    long compared = 0, controls[8] = {0};
    for (int step = 0; step < steps; step++)
    {
        if (rnd() % 97 == 0)
        {
            w = -w;
            controls[7]++;
            for (int e = 0; e < entities; e++)
                for (int i = 0; i < count[e]; i++)
                    if (!os[e][i].side) os[e][i].ended = (uint8_t)table[wslot(w)][(uint16_t)(os[e][i].origin + F)].end;
        }
        for (int e = 0; e < entities; e++)
        {
            if (rnd() % 8 == 0)
            {
                int op = rnd() % 7, i = rnd() % count[e];
                ref_slot *r = &rs[e][i];
                org_slot *o = &os[e][i];
                if (op == 6 && count[e] < MAXS)
                {
                    int t = rnd() % T, k = rnd() % dur[t], rev = rnd() % 4 == 0, j = count[e]++;
                    rs[e][j] = (ref_slot){t, k, rev, 0, 0, 1};
                    os[e][j] = (org_slot){0, 1, 0, t, k, rev, 0, 0, 1};
                    to_dense(&os[e][j], F, w);
                    controls[6]++;
                }
                else if (op < 6 && r->alive)
                {
                    controls[op]++;
                    to_side(o, F);
                    if (op == 0) { r->paused = 1; o->paused = 1; }
                    if (op == 1) { r->paused = 0; o->paused = 0; }
                    if (op == 2) { r->reverse ^= 1; o->reverse ^= 1; }
                    if (op == 3) { r->rate2 ^= 1; o->rate2 ^= 1; }
                    if (op == 4) { int k = rnd() % dur[r->timeline]; r->tick = k; o->tick = k; }
                    if (op == 5) { r->alive = 0; o->alive = 0; }
                    to_dense(o, F, w);
                }
            }
            totals_t a = ref_step(rs[e], count[e], w);
            totals_t b = org_step(os[e], count[e], F, w);
            if (memcmp(&a, &b, sizeof a) != 0)
            {
                printf("MISMATCH step=%d entity=%d ref speed=%a lift=%a dmg=%d stat=%d key=%d | org speed=%a lift=%a dmg=%d stat=%d key=%d\n", step, e, a.speed, a.lift, a.damage, a.stat, a.key, b.speed, b.lift, b.damage, b.stat, b.key);
                return 1;
            }
            compared++;
        }
        F = (uint16_t)(F + w);
    }
    printf("controls oracle: %ld entity-steps bit-exact, rows=%d (u16 limit 65535), pause=%ld resume=%ld reverse=%ld rate=%ld seek=%ld stop=%ld start=%ld worldFlip=%ld\n",
           compared, 1 + 2 * N, controls[0], controls[1], controls[2], controls[3], controls[4], controls[5], controls[6], controls[7]);
    return 0;
}
