using System;
using System.Runtime.CompilerServices;
using OperationOrderProof;

var schedule = BuildSchedule();
var forward = new[]
{
    new EntityState(1),
    new EntityState(2),
    new EntityState(3),
    new EntityState(4),
    new EntityState(5),
    new EntityState(1, 1),
    new EntityState(2),
};
var reverse = new[]
{
    new EntityState(1, 1),
    new EntityState(2, 1),
    new EntityState(3, 1),
    new EntityState(4, 1),
    new EntityState(5, 1),
    new EntityState(1),
    new EntityState(2, 1),
};

CheckParity(schedule, forward, 1, "forward");
CheckParity(schedule, reverse, -1, "reverse");
CheckRequiredCases(schedule);
CheckNaiveTypeBatchingFails(schedule);
CheckCrossEntityRestriction(schedule);
var allocated = CheckWarmAllocation(schedule, forward);

Console.WriteLine("Passed forward and reverse parity for A-B-A, B-A, simultaneous clips, gaps, 256 tracks, and mixed completed/live entities.");
Console.WriteLine("Pure type batching was refuted; occurrence-ordinal stages preserved every per-entity trace. Arbitrary cross-entity effect order was refuted.");
Console.WriteLine($"Asset {Unsafe.SizeOf<Asset>()} B; FrameSlice {Unsafe.SizeOf<FrameSlice>()} B; Occurrence {Unsafe.SizeOf<Occurrence>()} B; Selection {Unsafe.SizeOf<Selection>()} B; EntityState {Unsafe.SizeOf<EntityState>()} B.");
Console.WriteLine($"Warm selection, stage dispatch, and commit allocated {allocated} B over 100 passes.");

static Schedule BuildSchedule()
{
    var occurrences = new Occurrence[264];
    occurrences[0] = new Occurrence(OperationId.A, 0, 10);
    occurrences[1] = new Occurrence(OperationId.B, 1, 11);
    occurrences[2] = new Occurrence(OperationId.A, 2, 12);
    occurrences[3] = new Occurrence(OperationId.B, 0, 20);
    occurrences[4] = new Occurrence(OperationId.A, 1, 21);
    occurrences[5] = new Occurrence(OperationId.B, 4, 30);
    occurrences[6] = new Occurrence(OperationId.A, 7, 31);
    occurrences[7] = new Occurrence(OperationId.B, 3, 40);
    for (var track = 0; track < 256; track++)
        occurrences[8 + track] = new Occurrence(
            (track & 1) == 0 ? OperationId.A : OperationId.B,
            (byte)track,
            (uint)(1000 + track));

    return new Schedule(
        new[]
        {
            new Asset(0, 0),
            new Asset(0, 1),
            new Asset(1, 1),
            new Asset(2, 1),
            new Asset(3, 2),
            new Asset(5, 1),
        },
        new[]
        {
            new FrameSlice(0, 3),
            new FrameSlice(3, 2),
            new FrameSlice(5, 2),
            new FrameSlice(7, 0),
            new FrameSlice(7, 1),
            new FrameSlice(8, 256),
        },
        occurrences);
}

static void CheckParity(Schedule schedule, EntityState[] initial, int direction, string name)
{
    var oracleEntities = (EntityState[])initial.Clone();
    var batchedEntities = (EntityState[])initial.Clone();
    var oracle = new Trace(initial.Length, 256);
    var batched = new Trace(initial.Length, 256);
    ScalarOracle.Tick(schedule, oracleEntities, direction, oracle);
    StageDispatcher.Tick(schedule, batchedEntities, direction, batched);
    for (var entity = 0; entity < initial.Length; entity++)
    {
        Check(oracleEntities[entity].Position == batchedEntities[entity].Position, $"{name} position {entity}");
        Check(oracle.Count(entity) == batched.Count(entity), $"{name} count {entity}");
        for (var occurrence = 0; occurrence < oracle.Count(entity); occurrence++)
            Check(oracle.Get(entity, occurrence) == batched.Get(entity, occurrence), $"{name} trace {entity}:{occurrence}");
    }
}

static void CheckRequiredCases(Schedule schedule)
{
    var states = new[]
    {
        new EntityState(1),
        new EntityState(2),
        new EntityState(3),
        new EntityState(4),
        new EntityState(5),
        new EntityState(1, 1),
        new EntityState(2),
    };
    var trace = new Trace(states.Length, 256);
    StageDispatcher.Tick(schedule, states, 1, trace);
    Check(trace.Count(0) == 3, "A-B-A count");
    Check(Operation(trace.Get(0, 0)) == OperationId.A
        && Operation(trace.Get(0, 1)) == OperationId.B
        && Operation(trace.Get(0, 2)) == OperationId.A, "A-B-A order");
    Check(trace.Count(1) == 2 && Operation(trace.Get(1, 0)) == OperationId.B
        && Operation(trace.Get(1, 1)) == OperationId.A, "B-A order");
    Check(trace.Count(2) == 2 && Track(trace.Get(2, 0)) == 4 && Track(trace.Get(2, 1)) == 7, "simultaneous clips");
    Check(trace.Count(3) == 0 && states[3].Position == 1, "gap advances");
    Check(trace.Count(4) == 256 && Track(trace.Get(4, 0)) == 0 && Track(trace.Get(4, 255)) == 255, "256 tracks");
    Check(trace.Count(5) == 0 && states[5].Position == 1, "completed entity");
    Check(trace.Count(6) == 2 && states[6].Position == 1, "live entity beside completed entity");

    trace.Clear();
    StageDispatcher.Tick(schedule, states.AsSpan(0, 3), -1, trace);
    Check(Operation(trace.Get(0, 0)) == OperationId.A && Track(trace.Get(0, 0)) == 2
        && Operation(trace.Get(0, 1)) == OperationId.B
        && Operation(trace.Get(0, 2)) == OperationId.A && Track(trace.Get(0, 2)) == 0, "reverse A-B-A");
    Check(Operation(trace.Get(1, 0)) == OperationId.A && Operation(trace.Get(1, 1)) == OperationId.B, "reverse B-A");
    Check(Track(trace.Get(2, 0)) == 7 && Track(trace.Get(2, 1)) == 4, "reverse simultaneous clips");
}

static void CheckNaiveTypeBatchingFails(Schedule schedule)
{
    var entity = new[] { new EntityState(1) };
    Selector.Select(schedule, entity, 1);
    Span<OperationId> naive = stackalloc OperationId[3];
    var written = 0;
    for (var operation = OperationId.A; operation <= OperationId.B; operation++)
    {
        for (var stage = 0; stage < entity[0].Selection.Count; stage++)
        {
            ref readonly var occurrence = ref schedule.Get(in entity[0].Selection, stage);
            if (occurrence.Operation == operation)
                naive[written++] = operation;
        }
    }
    Check(naive[0] == OperationId.A && naive[1] == OperationId.A && naive[2] == OperationId.B, "naive type-major witness");
    Check(!(naive[0] == OperationId.A && naive[1] == OperationId.B && naive[2] == OperationId.A), "pure type batching must fail A-B-A");
}

static void CheckCrossEntityRestriction(Schedule schedule)
{
    var scalarEntities = new[] { new EntityState(1), new EntityState(2) };
    var stagedEntities = (EntityState[])scalarEntities.Clone();
    var scalarGlobal = new GlobalTrace(5);
    var stagedGlobal = new GlobalTrace(5);
    ScalarOracle.Tick(schedule, scalarEntities, 1, new Trace(2, 3), scalarGlobal);
    StageDispatcher.Tick(schedule, stagedEntities, 1, new Trace(2, 3), stagedGlobal);
    Check(scalarGlobal.Count == stagedGlobal.Count, "global trace count");
    var equal = true;
    for (var occurrence = 0; occurrence < scalarGlobal.Count; occurrence++)
        equal &= scalarGlobal.Get(occurrence) == stagedGlobal.Get(occurrence);
    Check(!equal, "cross-entity side effects require scalar entity-major fallback");
}

static long CheckWarmAllocation(Schedule schedule, EntityState[] initial)
{
    var states = new EntityState[initial.Length];
    var trace = new Trace(initial.Length, 256);
    for (var warmup = 0; warmup < 10; warmup++)
    {
        initial.CopyTo(states, 0);
        trace.Clear();
        StageDispatcher.Tick(schedule, states, 1, trace);
    }
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var iteration = 0; iteration < 100; iteration++)
    {
        initial.CopyTo(states, 0);
        trace.Clear();
        StageDispatcher.Tick(schedule, states, 1, trace);
    }
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(allocated == 0, "warm allocation");
    return allocated;
}

static OperationId Operation(ulong token) => (OperationId)(token >> 48);

static byte Track(ulong token) => (byte)(token >> 40);

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException(name);
}
