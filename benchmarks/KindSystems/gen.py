import json, os, sys

PAIRS = 31
KINDS = PAIRS * 2
GROUP = os.environ.get("TL_GROUP", "")

def domain():
    out = ["using System;", "using Tl;", "", "namespace KindBench", "{", ""]
    out += ["[TimelineSystem]", "public ref partial struct KSys", "{", "    public Span<float> Fx;", "    public Span<int> Hp;", "}", ""]
    out += ["[TimelineSystem]", "public ref partial struct LSys", "{", "    public Span<float> Fx;", "}", ""]
    out += ["public struct LegacyClip { public float Amount; }",
            "public struct LegacyTrack : IBlend<LegacyClip>", "{", "    public float Scale;", "    public void Blend(in LegacyClip a, in LegacyClip b, float t, out LegacyClip r) => r = a;", "}",
            "[TimelineSystemMember(typeof(LSys))]", "public struct Legacy : ITrack<LegacyTrack, LegacyClip>", "{",
            "    public static void ExecuteActive(in Frame<LegacyTrack, LegacyClip> f, ref float fx) => fx += f.Direction * f.Clip.Amount * f.Track.Scale;",
            "    public static void OnClipEnd(in Frame<LegacyTrack, LegacyClip> f)", "    {", "    }", "}", ""]
    out += ["[TimelineKind(Accepts = KindEntries.Pulses, FoldsPulses = true)]", "public struct Dmg", "{",
            "    public static void Combine(ref int t, in int n) => t += n;", "    public static void Apply(in int t, ref int hp) => hp -= t;", "}",
            "public struct DmgFold : IPulseFold<int>", "{", "    public void Fold(ref int total, in int next) => total += next;", "}",
            "public struct HitClip { public int Amount; }",
            "public struct HitTrack : IBlend<HitClip>", "{", "    public float Scale;", "    public void Blend(in HitClip a, in HitClip b, float t, out HitClip r) => r = a;", "}",
            "[TimelineSystemMember(typeof(KSys))]", "public struct Hit : ITrack<HitTrack, HitClip>", "{",
            "    [Pulse(typeof(Dmg), On = ClipEvent.Start)]", "    public static int Start(in Frame<HitTrack, HitClip> f) => f.Clip.Amount;", "}", ""]
    for j in range(KINDS):
        out += [f"[TimelineKind(Group = \"{GROUP}\")]" if GROUP else "[TimelineKind]", f"public struct K{j}", "{",
                "    public static void Combine(ref float t, in float n) => t += n;",
                "    public static void Apply(in float t, ref float fx) => fx += t;", "}",
                f"public struct V{j} {{ public float Amount; }}",
                f"public struct T{j} : IBlend<V{j}>", "{", "    public float Scale;", f"    public void Blend(in V{j} a, in V{j} b, float t, out V{j} r) => r = a;", "}",
                "[TimelineSystemMember(typeof(KSys))]", f"public struct C{j} : ITrack<T{j}, V{j}>", "{",
                f"    [Feeds(typeof(K{j}))]", f"    public static float Evaluate(in Frame<T{j}, V{j}> f) => f.Direction * f.Clip.Amount * f.Track.Scale;", "}", ""]
    out += ["}", ""]
    return "\n".join(out)

def track(t, scale, clips, clip_type):
    return {"namespace": "KindBench", "type": t, "data": {"Scale": scale},
            "clips": [{"namespace": "KindBench", "type": clip_type, "start": a, "end": b, "data": d} for a, b, d in clips]}

def timeline(name, duration, tracks, loop=False):
    o = {"name": name, "duration": duration}
    if loop: o["loop"] = True
    o["tracks"] = tracks
    return o

def write_lib(folder, timelines, kinds=None):
    os.makedirs(folder, exist_ok=True)
    names = []
    for t in timelines:
        json.dump(t, open(os.path.join(folder, t["name"] + ".json"), "w"), indent=1)
        names.append(t["name"] + ".json")
    library = {"schema": 1, "tickRate": 60, "timelines": names}
    if kinds: library["kinds"] = kinds
    json.dump(library, open(os.path.join(folder, "tl.project.json"), "w"), indent=1)

if __name__ == "__main__":
    open("Domain.cs", "w").write(domain())
    write_lib("lib-shapes", [
        timeline("walk", 40, [track("T0", 2, [(0, 40, {"Amount": 3})], "V0")], loop=True),
        timeline("walk_legacy", 40, [track("LegacyTrack", 2, [(0, 40, {"Amount": 3})], "LegacyClip")], loop=True),
        timeline("long", 65535, [track("T0", 2, [(0, 65535, {"Amount": 3})], "V0")], loop=True),
        timeline("hit", 8, [track("HitTrack", 1, [(2, 4, {"Amount": 5})], "HitClip")]),
    ], kinds={"Dmg": {"gamePulses": 8, "maxInlineBytes": 128}})
    write_lib("lib-many", [timeline(f"m{i:03d}", 40, [track("T0", 2, [(0, 40, {"Amount": 1 + i % 7})], "V0")], loop=True) for i in range(100)])
    write_lib("lib-pairs", [timeline(f"p{i:02d}", 40, [track(f"T{2*i}", 2, [(0, 40, {"Amount": 3})], f"V{2*i}"), track(f"T{2*i+1}", 2, [(0, 40, {"Amount": 4})], f"V{2*i+1}")], loop=True) for i in range(PAIRS)] + [timeline(f"q{i:02d}", 40, [track("T0", 2, [(0, 40, {"Amount": 3})], "V0"), track("T1", 2, [(0, 40, {"Amount": 4})], "V1")], loop=True) for i in range(PAIRS)] + [timeline("hit", 8, [track("HitTrack", 1, [(2, 4, {"Amount": 5})], "HitClip")])])
