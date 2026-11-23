using KindBench;
using Tl;

const int N = 1000;
const int Steps = 600;
var path = Path.Combine(AppContext.BaseDirectory, "shapes.tlib");
if (!File.Exists(path)) { Console.WriteLine("FAIL: shapes.tlib missing; run benchmarks/KindSystems/bake.sh"); return 2; }
using var world = new TimelineWorld(File.ReadAllBytes(path));
using var kinds = new PlaybackTable(N);
var ones = new ulong[N];
Array.Fill(ones, 0b11ul);
using var store = new KindStore(world, kinds, ["KindBench.K0", "KindBench.Dmg"], ones);
var bullets = store.Pulses<int, DmgFold>("KindBench.Dmg");
var kindFx = new float[N];
var hp = new int[N];
var expected = new int[N];
for (var e = 0; e < N; e++)
{
    kinds.Start(e, 3, (ushort)(e % 40));
}
var hash = 14695981039346656037ul;
long bytesBefore = 0;
for (var step = 0; step < Steps; step++)
{
    if (step == Steps - 100) bytesBefore = GC.GetAllocatedBytesForCurrentThread();
    if (step % 7 == 3)
        for (var e = step % 17; e < N; e += 17)
        {
            bullets.To(e, 2);
            bullets.To(e, 3);
            expected[e] -= 5;
        }
    world.Advance(legacy, true);
    world.Tick(kinds);
    new KSys { Fx = kindFx, Hp = hp }.Run(world, kinds, true);
    for (var e = 0; e < N; e++)
    {
        var a = BitConverter.SingleToInt32Bits((step + 1) * 6f);
        if (a != BitConverter.SingleToInt32Bits(kindFx[e]) || hp[e] != expected[e])
        {
            Console.WriteLine($"FAIL: step {step} entity {e}: expected movement {(step + 1) * 6f} kinds {kindFx[e]} hp {hp[e]} expected {expected[e]}");
            return 1;
        }
        hash = (hash ^ (uint)a) * 1099511628211ul;
        hash = (hash ^ (uint)hp[e]) * 1099511628211ul;
    }
}
var allocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
Console.WriteLine($"PASS: kinds match the scalar walk oracle bit for bit for {Steps} steps; bullets summed into one health write; state hash {hash}; allocated {allocated} B over the last 100 steps");
return allocated == 0 ? 0 : 3;
