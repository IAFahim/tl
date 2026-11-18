using System.Text;
using Microsoft.CodeAnalysis;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

internal enum SystemModelKind : byte { System, Kind, State, Feeder, Consumer, Module }

internal sealed record SystemTypeModel(string TypeName, SystemModelKind Kind, string AssemblyName, string Fingerprint, SyntaxNode? Site)
{
    public SystemDecl? System { get; init; }
    public KindDecl? KindDecl { get; init; }
    public SystemConsumer? Consumer { get; init; }
    public IReadOnlyList<FeederDecl>? Feeders { get; init; }
    public IReadOnlyList<KindDecl>? Foreign { get; init; }
    public IReadOnlyList<DeclarationDiagnostic>? Diagnostics { get; init; }
    public IReadOnlyList<bool>? ApplyPlain { get; init; }
    public IReadOnlyList<bool>? IdlePlain { get; init; }
    public IReadOnlyList<string>? Events { get; init; }
    public IReadOnlyList<string>? Facts { get; init; }
    public bool Equals(SystemTypeModel? other) => other is not null && Kind == other.Kind && TypeName == other.TypeName && Fingerprint == other.Fingerprint;
    public override int GetHashCode() => (Kind, TypeName, Fingerprint).GetHashCode();
}

internal static class SystemDigest
{
    internal static string Of(SystemTypeModel m)
    {
        var b = new StringBuilder();
        Add(b, m.TypeName); Add(b, m.AssemblyName);
        if (m.System is not null) Add(b, OfSystem(m.System));
        if (m.KindDecl is not null) Add(b, OfKind(m.KindDecl));
        if (m.Consumer is not null) Add(b, OfConsumer(m.Consumer));
        foreach (var f in m.Feeders ?? []) Add(b, OfFeeder(f));
        foreach (var f in m.Foreign ?? []) Add(b, OfKind(f));
        foreach (var d in m.Diagnostics ?? []) Add(b, OfDiag(d));
        foreach (var p in m.ApplyPlain ?? []) Add(b, p ? "1" : "0");
        foreach (var p in m.IdlePlain ?? []) Add(b, p ? "1" : "0");
        foreach (var e in m.Events ?? []) Add(b, e);
        foreach (var f in m.Facts ?? []) Add(b, f);
        return b.ToString();
        static void Add(StringBuilder b, string v) => b.Append('\0').Append(v);
        static string OfDiag(DeclarationDiagnostic d) => $"{d.File}\0{d.Line}\0{d.Column}\0{d.Code}\0{d.Message}\0{d.SpanStart}\0{d.SpanLength}\0{d.EndLine}\0{d.EndColumn}";
        static string OfField(SystemField f) => $"{f.Name}\0{f.TypeName}\0{(byte)f.Kind}\0{f.ElementTypeName}";
        static string OfParam(ConsumerParam p) => $"{p.Name}\0{p.TypeName}\0{p.Modifier}\0{(byte)p.Kind}\0{p.FoldIndex}\0{p.BatchElement}";
        static string OfLane(DirectLane d) => $"{d.Eligible}\0{d.Op}\0{d.WrittenType}\0{d.WrittenMember}" + string.Join("\0", d.FoldOffsets.Select(x => x.ToString())) + "|" + string.Join("\0", d.ReadTypes);
        static string OfMethod(ConsumerMethod m) => $"{(byte)m.Kind}" + (m.Direct == DirectLane.None ? "" : OfLane(m.Direct)) + string.Join("\0", m.Params.Select(OfParam)) + "|" + string.Join("\0", m.FoldTypes) + "|" + string.Join("\0", m.FoldSizes.Select(x => x.ToString()));
        static string OfFeeder(FeederDecl f) => $"{f.KindName}\0{(byte)f.Trigger}\0{f.MethodName}\0{f.ConsumerMetadata}\0{f.ValueTypeName}\0{OfMethod(f.Method)}";
        static string OfKind(KindDecl k) => $"{k.Name}\0{k.TypeName}\0{k.ValueTypeName}\0{k.Each}\0{k.Accepts}\0{k.Order}\0{k.Group}\0{k.Foreign}\0{k.Id}" + string.Join("\0", k.ApplyParams.Select(OfParam)) + "|" + string.Join("\0", k.MemberSystems) + "|" + (k.IdleParams is null ? "" : string.Join("\0", k.IdleParams.Select(OfParam)));
        static string OfSystem(SystemDecl s) => $"{s.TypeName}\0{s.IsRef}\0{s.SystemKey}" + string.Join("\0", s.Fields.Select(OfField));
        static string OfConsumer(SystemConsumer c) => $"{c.ConsumerTypeName}\0{c.TrackTypeName}\0{c.ClipTypeName}\0{c.MarkerTypeName}\0{c.IsMarker}\0{c.TrackBlendable}\0{c.PairKey}" + string.Join("\0", c.MemberSystems) + "|" + string.Join("\0", c.Methods.Select(OfMethod)) + "|" + string.Join("\0", c.Diagnostics.Select(OfDiag)) + "|" + string.Join("\0", c.Feeders.Select(OfFeeder));
    }
}
