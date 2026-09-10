from pathlib import Path
import sys


def timelines(prefix: str, hook: str, count: int) -> str:
    return "\n\n".join(
        f"""public readonly partial struct {prefix}{index:03} : ITimeline
{{
    public static void Define(scoped Builder builder)
    {{
        builder.Before<{hook}>();
    }}
}}"""
        for index in range(count)
    )


def ids(prefix: str, count: int) -> str:
    return ", ".join(f"{prefix}{index:03}.Id" for index in range(count))


output = Path(sys.argv[1])
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(
    """using Tl;

public struct OneState { public int Value; }
public struct SixteenState { public int Value; }
public struct WideState { public int Value; }

public readonly struct OneHook : IHook
{
    public static void Forward(ref OneState state) => state.Value++;
    public static void Backward(ref OneState state) => state.Value--;
}

public readonly struct SixteenHook : IHook
{
    public static void Forward(ref SixteenState state) => state.Value++;
    public static void Backward(ref SixteenState state) => state.Value--;
}

public readonly struct WideHook : IHook
{
    public static void Forward(ref WideState state) => state.Value++;
    public static void Backward(ref WideState state) => state.Value--;
}

"""
    + timelines("One", "OneHook", 1)
    + "\n\n"
    + timelines("Sixteen", "SixteenHook", 16)
    + "\n\n"
    + timelines("Wide", "WideHook", 256)
    + f"""

public partial class RoutingBenchmarks
{{
    private static readonly ushort[] s_oneIds = [{ids("One", 1)}];
    private static readonly ushort[] s_sixteenIds = [{ids("Sixteen", 16)}];
    private static readonly ushort[] s_wideIds = [{ids("Wide", 256)}];
}}
"""
    + "\n",
    encoding="utf-8",
)
