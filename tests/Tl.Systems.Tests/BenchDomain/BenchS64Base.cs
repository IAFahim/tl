using Tl;

namespace Bench;

public struct Shared
{
    public int Ticks;
    public Shared(int ticks) { Ticks = ticks; }
}

[Tl.TimelineSystem]
public ref partial struct S64
{
    public Shared Ctx;
}

public struct T0 : IBlend<C0> { public float S; public T0(float s) { S = s; } public void Blend(in C0 first, in C0 second, float factor, out C0 result) { result = new C0(first.V + (second.V - first.V) * factor); } }
public struct C0 { public float V; public C0(float v) { V = v; } }
public struct T1 : IBlend<C1> { public float S; public T1(float s) { S = s; } public void Blend(in C1 first, in C1 second, float factor, out C1 result) { result = new C1(first.V + (second.V - first.V) * factor); } }
public struct C1 { public float V; public C1(float v) { V = v; } }
public struct T2 : IBlend<C2> { public float S; public T2(float s) { S = s; } public void Blend(in C2 first, in C2 second, float factor, out C2 result) { result = new C2(first.V + (second.V - first.V) * factor); } }
public struct C2 { public float V; public C2(float v) { V = v; } }
public struct T3 : IBlend<C3> { public float S; public T3(float s) { S = s; } public void Blend(in C3 first, in C3 second, float factor, out C3 result) { result = new C3(first.V + (second.V - first.V) * factor); } }
public struct C3 { public float V; public C3(float v) { V = v; } }
public struct T4 : IBlend<C4> { public float S; public T4(float s) { S = s; } public void Blend(in C4 first, in C4 second, float factor, out C4 result) { result = new C4(first.V + (second.V - first.V) * factor); } }
public struct C4 { public float V; public C4(float v) { V = v; } }
public struct T5 : IBlend<C5> { public float S; public T5(float s) { S = s; } public void Blend(in C5 first, in C5 second, float factor, out C5 result) { result = new C5(first.V + (second.V - first.V) * factor); } }
public struct C5 { public float V; public C5(float v) { V = v; } }
public struct T6 : IBlend<C6> { public float S; public T6(float s) { S = s; } public void Blend(in C6 first, in C6 second, float factor, out C6 result) { result = new C6(first.V + (second.V - first.V) * factor); } }
public struct C6 { public float V; public C6(float v) { V = v; } }
public struct T7 : IBlend<C7> { public float S; public T7(float s) { S = s; } public void Blend(in C7 first, in C7 second, float factor, out C7 result) { result = new C7(first.V + (second.V - first.V) * factor); } }
public struct C7 { public float V; public C7(float v) { V = v; } }
