namespace Kinds;

public static class KindId
{
    public const byte Locomotion = 0;
    public const byte Lift = 1;
    public const byte Damage = 2;
    public const byte StatChange = 3;
    public const byte Fx = 4;
}

public struct Speed
{
    public float Value;
    public byte Priority;
}

public struct Locomotion : IKind<Speed>
{
    public static bool Each => false;

    public static unsafe Speed Evaluate(Library library, Entry* entry, Slot* slot, int dir, int entity, in Columns columns)
        => new() { Value = library.LaneAt(entry->Timeline, entry->Step, slot->Tick, dir) * columns.WalkStat[entity], Priority = library.ClipOf(entry->Timeline, entry->Step)->Priority };

    public static void Combine(ref Speed total, in Speed next)
    {
        if (next.Priority >= total.Priority) total = next;
    }

    public static unsafe void Apply(in Speed total, int entity, in Columns columns) => columns.MotionX[entity] += total.Value;
}

public struct Lift : IKind<float>
{
    public static bool Each => false;

    public static unsafe float Evaluate(Library library, Entry* entry, Slot* slot, int dir, int entity, in Columns columns)
        => library.LaneAt(entry->Timeline, entry->Step, slot->Tick, dir) * columns.JumpPower[entity];

    public static void Combine(ref float total, in float next) => total += next;

    public static unsafe void Apply(in float total, int entity, in Columns columns) => columns.MotionY[entity] += total;
}

public struct Damage : IKind<int>
{
    public static bool Each => false;

    public static unsafe int Evaluate(Library library, Entry* entry, Slot* slot, int dir, int entity, in Columns columns)
        => dir > 0 ? library.ClipOf(entry->Timeline, entry->Step)->Hit : 0;

    public static void Combine(ref int total, in int next) => total += next;

    public static unsafe void Apply(in int total, int entity, in Columns columns) => columns.Health[entity] -= total;
}

public struct StatChange : IKind<int>
{
    public static bool Each => true;

    public static unsafe int Evaluate(Library library, Entry* entry, Slot* slot, int dir, int entity, in Columns columns)
        => dir * library.ClipOf(entry->Timeline, entry->Step)->Hit;

    public static void Combine(ref int total, in int next) => total = next;

    public static unsafe void Apply(in int total, int entity, in Columns columns) => columns.Stamina[entity] += total;
}

public struct Fx : IKind<float>
{
    public static bool Each => false;

    public static unsafe float Evaluate(Library library, Entry* entry, Slot* slot, int dir, int entity, in Columns columns)
        => library.LaneAt(entry->Timeline, entry->Step, slot->Tick, dir);

    public static void Combine(ref float total, in float next) => total += next;

    public static unsafe void Apply(in float total, int entity, in Columns columns) => columns.Fx[entity] += total;
}
