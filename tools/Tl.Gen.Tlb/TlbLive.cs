using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using System.Text.Json;
using Tl;

namespace Tl.Gen.Tlb;

public static unsafe class TlbLive
{
    static readonly HashSet<string> InitializedAssemblies = [];
    static readonly Dictionary<string, Assembly> HostedAssemblies = [];

    public static int Run(string[] args)
    {
        string? assemblyPath = null;
        var assetPaths = new List<string>();
        var resolve = false;
        var addresses = false;
        var summary = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--assembly":
                    if (++i == args.Length) return Usage("--assembly requires a path");
                    assemblyPath = args[i];
                    break;
                case "--asset":
                    if (++i == args.Length) return Usage("--asset requires a path");
                    assetPaths.Add(args[i]);
                    break;
                case "--resolve":
                    resolve = true;
                    break;
                case "--addresses":
                    addresses = true;
                    break;
                case "--summary":
                    summary = true;
                    break;
                case "--json":
                    break;
                default:
                    return Usage($"Unknown option '{args[i]}'");
            }
        }
        if (assemblyPath is null) return Usage("--live requires --assembly <path>");
        var fullAssembly = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullAssembly))
        {
            Console.Error.WriteLine($"Host error: assembly file '{assemblyPath}' does not exist.");
            return 4;
        }

        Assembly assembly;
        if (!TryHost(fullAssembly, out assembly, out var failure))
        {
            Console.Error.WriteLine($"Host error: {failure}");
            return 4;
        }

        var engine = TlbEngine.Live;
        var assemblyName = assembly.GetName();
        byte[] assemblyBytes;
        try
        {
            assemblyBytes = File.ReadAllBytes(fullAssembly);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Host error: {ex.Message}");
            return 4;
        }

        var assets = new List<(string File, int Bytes, int Index, string Sha, HashSet<ulong> Pairs)>();
        foreach (var path in assetPaths)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Error: asset file '{path}' does not exist.");
                return 2;
            }
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Asset error: {ex.Message}");
                return 3;
            }
            int index;
            try
            {
                index = TimelineAsset.Load(bytes);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Asset error: {ex.Message}");
                return 3;
            }
            assets.Add((Path.GetFileName(path), bytes.Length, index, Sha(bytes), AssetPairKeys(bytes)));
        }
        assets.Sort((a, b) => a.Index != b.Index ? a.Index - b.Index : string.CompareOrdinal(a.File, b.File));

        var engineReferences = new List<(string Name, string Version)>();
        foreach (var reference in assembly.GetReferencedAssemblies())
            if (reference.Name == "Tl.Core")
                engineReferences.Add((reference.Name!, VersionOf(reference)));
        engineReferences.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        try
        {
            if (resolve && InitializedAssemblies.Add(fullAssembly))
                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Host error: module initialization failed: {ex.Message}");
            return 4;
        }

        if (resolve && engine is null)
            Console.Error.WriteLine("Host warning: resolve driver unavailable in this engine build; reporting without folding.");
        var engineVersion = VersionOf(typeof(TimelineAsset).Assembly.GetName());
        var banks = Banks(engine, assembly, resolve, addresses, [.. assets.Select(asset => (asset.Index, asset.Pairs))]);
        foreach (var bank in banks)
            if (bank.Snapshot.RetainedBytes != bank.Snapshot.HeaderBytes + bank.Snapshot.TableBytes + bank.Snapshot.DirectoryBytes)
            {
                Console.Error.WriteLine($"Snapshot inconsistency: bank {bank.Snapshot.Track},{bank.Snapshot.Clip} retained {bank.Snapshot.RetainedBytes} != {bank.Snapshot.HeaderBytes + bank.Snapshot.TableBytes + bank.Snapshot.DirectoryBytes}.");
                return 5;
            }

        if (summary)
        {
            Console.Write(Summary(assemblyName.Name ?? "", VersionOf(assemblyName), Sha(assemblyBytes), assets, banks, engineVersion));
            return 0;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            void W(string name) => writer.WriteStartObject(name);
            void A(string name) => writer.WriteStartArray(name);
            void E() => writer.WriteEndObject();
            void N(string name, long value) => writer.WriteNumber(name, value);
            void S(string name, string value) => writer.WriteString(name, value);

            writer.WriteStartObject();
            N("schemaVersion", 1);
            S("plane", "live");
            W("host");
            S("engine", "Tl.Core");
            S("engineVersion", engineVersion);
            E();
            W("assembly");
            S("name", assemblyName.Name ?? "");
            S("version", VersionOf(assemblyName));
            S("sha256", Sha(assemblyBytes));
            A("engineReferences");
            foreach (var reference in engineReferences)
            {
                writer.WriteStartObject();
                S("name", reference.Name);
                S("version", reference.Version);
                E();
            }
            writer.WriteEndArray();
            E();
            A("assets");
            foreach (var asset in assets)
            {
                writer.WriteStartObject();
                S("file", asset.File);
                N("bytes", asset.Bytes);
                N("index", asset.Index);
                S("sha256", asset.Sha);
                E();
            }
            writer.WriteEndArray();
            var tables = Inspection.Tables();
            W("tables");
            W("intern");
            N("capacity", tables.Intern.Capacity);
            N("live", tables.Intern.Live);
            N("distinct", tables.Intern.Distinct);
            N("graveyardBlocks", tables.Intern.GraveyardBlocks);
            N("graveyardBytes", tables.Intern.GraveyardBytes);
            N("allocBytes", tables.Intern.AllocBytes);
            N("peakLiveBytes", tables.Intern.PeakLiveBytes);
            E();
            W("pair");
            N("slots", tables.Pair.Slots);
            N("pairs", tables.Pair.Pairs);
            N("loadFactorPermille", tables.Pair.LoadFactorPermille);
            N("consumers", tables.Pair.Consumers);
            N("consumerCapacity", tables.Pair.ConsumerCapacity);
            N("slotRowBytes", tables.Pair.SlotRowBytes);
            E();
            W("bake");
            N("slots", tables.Bake.Slots);
            N("entries", tables.Bake.Entries);
            E();
            E();
            A("banks");
            foreach (var bank in banks)
            {
                writer.WriteStartObject();
                S("track", bank.Snapshot.Track);
                S("clip", bank.Snapshot.Clip);
                S("pairKey", Hex(bank.Snapshot.PairKey));
                N("count", bank.Snapshot.Count);
                N("holes", bank.Snapshot.Holes);
                N("blocks", bank.Snapshot.Blocks);
                N("dedupeHits", bank.Snapshot.DedupeHits);
                N("generation", bank.Snapshot.Generation);
                W("bytes");
                N("header", bank.Snapshot.HeaderBytes);
                N("table", bank.Snapshot.TableBytes);
                N("directory", bank.Snapshot.DirectoryBytes);
                N("retained", bank.Snapshot.RetainedBytes);
                E();
                A("views");
                foreach (var view in bank.Views)
                {
                    writer.WriteStartObject();
                    N("index", view.Index);
                    S("foldState", view.FoldState);
                    if (view is { FoldState: not "absent" and not "pending", Duration: not null })
                    {
                        N("duration", view.Duration.Value);
                        writer.WriteBoolean("looping", view.Looping);
                        N("ticks", view.Ticks!.Value);
                        N("lanes", view.Lanes!.Value);
                        N("abiVersion", view.AbiVersion!.Value);
                        N("generation", view.Generation!.Value);
                        if (view.Address is not null) S("address", Hex(unchecked((ulong)view.Address.Value)));
                        A("laneShapes");
                        foreach (var shape in view.LaneShapes!) WriteShape(writer, shape);
                        writer.WriteEndArray();
                    }
                    E();
                }
                writer.WriteEndArray();
                E();
            }
            writer.WriteEndArray();
            W("deep");
            writer.WriteBoolean("available", true);
            E();
            writer.WriteEndObject();
        }
        Console.Write(Encoding.UTF8.GetString(stream.ToArray()));
        return 0;
    }

    public static int Exec(string[] args)
    {
        string? assemblyPath = null;
        string? methodSpec = null;
        var assets = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--assembly":
                    if (++i == args.Length) return ExecUsage("--assembly requires a path");
                    assemblyPath = args[i];
                    break;
                case "--method":
                    if (++i == args.Length) return ExecUsage("--method requires Namespace.Type.Method");
                    methodSpec = args[i];
                    break;
                case "--asset":
                    if (++i == args.Length) return ExecUsage("--asset requires a path");
                    assets.Add(args[i]);
                    break;
                case "--arg":
                    if (++i == args.Length || !args[i].Contains('=')) return ExecUsage("--arg requires key=value");
                    break;
                default:
                    return ExecUsage($"Unknown option '{args[i]}'");
            }
        }
        if (assemblyPath is null) return ExecUsage("--exec requires --assembly <path>");
        if (methodSpec is null) return ExecUsage("--exec requires --method Namespace.Type.Method");
        if (assets.Count > 1) return ExecUsage("--exec accepts at most one --asset");
        foreach (var assetPath in assets)
            if (!File.Exists(assetPath))
            {
                Console.Error.WriteLine($"Error: asset file '{assetPath}' does not exist.");
                return 2;
            }
        var fullAssembly = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullAssembly))
        {
            Console.Error.WriteLine($"Host error: assembly file '{assemblyPath}' does not exist.");
            return 4;
        }
        if (!TryHost(fullAssembly, out var assembly, out var failure))
        {
            Console.Error.WriteLine($"Host error: {failure}");
            return 4;
        }
        var separator = methodSpec.LastIndexOf('.');
        if (separator <= 0 || separator == methodSpec.Length - 1)
        {
            Console.Error.WriteLine($"Host error: --method expects Namespace.Type.Method, got '{methodSpec}'.");
            return 4;
        }
        var typeName = methodSpec[..separator];
        var methodName = methodSpec[(separator + 1)..];
        var type = assembly.GetType(typeName, throwOnError: false);
        MethodInfo? bare = null;
        MethodInfo? withAsset = null;
        if (type != null)
            foreach (var candidate in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (candidate.Name != methodName || candidate.ReturnType != typeof(int)) continue;
                var parameters = candidate.GetParameters();
                if (parameters.Length == 0) bare = candidate;
                else if (parameters.Length == 1 && typeof(TimelineAsset).IsAssignableFrom(parameters[0].ParameterType)) withAsset = candidate;
            }
        var method = assets.Count > 0 ? withAsset ?? bare : bare ?? withAsset;
        if (method is null)
        {
            Console.Error.WriteLine($"Host error: no public static int {methodName}() or {methodName}(TimelineAsset) on '{typeName}'.");
            return 4;
        }
        object?[] arguments = [];
        if (method.GetParameters().Length == 1)
        {
            if (assets.Count != 1)
            {
                Console.Error.WriteLine($"Host error: {methodSpec} takes a TimelineAsset; pass one --asset <file.tlb>.");
                return 4;
            }
            try
            {
                arguments = [TimelineAsset.Of(TimelineAsset.Load(File.ReadAllBytes(assets[0])))];
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Asset error: {ex.Message}");
                return 3;
            }
        }
        try
        {
            return (int)method.Invoke(null, arguments)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            Console.Error.WriteLine($"Host error: {methodSpec} threw {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            Console.Error.WriteLine(ex.InnerException.StackTrace);
            return 4;
        }
    }

    static int ExecUsage(string message)
    {
        Console.Error.WriteLine($"Error: {message}\nUsage: tlb --exec --assembly <path.dll> --method Namespace.Type.Method [--asset <file.tlb>] [--arg key=value ...]");
        return 2;
    }

    internal static bool TryHost(string assemblyPath, out Assembly assembly, out string failure)
    {
        failure = "";
        if (HostedAssemblies.TryGetValue(assemblyPath, out assembly!)) return true;
        try
        {
            assembly = new TlbHostContext(assemblyPath).LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            assembly = null!;
            failure = ex.Message;
            return false;
        }
        HostedAssemblies[assemblyPath] = assembly;
        return true;
    }

    static int Usage(string message)
    {
        Console.Error.WriteLine($"Error: {message}\nUsage: tlb --live --assembly <path.dll> [--asset <file.tlb>]... [--resolve] [--json | --summary] [--addresses]");
        return 2;
    }

    static string Sha(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    static string VersionOf(AssemblyName name) => name.Version?.ToString() ?? "0.0";

    static string Hex(ulong value) => "0x" + value.ToString("x16");

    static void WriteShape(Utf8JsonWriter writer, Inspection.LaneShape shape)
    {
        writer.WriteStartObject();
        writer.WriteString("key", Hex(shape.Key));
        writer.WriteNumber("distinct", shape.Distinct);
        writer.WriteNumber("longestRun", shape.LongestRun);
        writer.WriteEndObject();
    }

    static string Summary(string assemblyName, string assemblyVersion, string assemblySha, List<(string File, int Bytes, int Index, string Sha, HashSet<ulong> Pairs)> assets, List<TlbBank> banks, string engineVersion)
    {
        var lines = new StringBuilder();
        lines.Append("engine Tl.Core ").Append(engineVersion).Append('\n');
        lines.Append("assembly ").Append(assemblyName).Append(' ').Append(assemblyVersion).Append(" sha256 ").Append(assemblySha[..16]).Append('\n');
        lines.Append("assets ").Append(assets.Count).Append('\n');
        var tables = Inspection.Tables();
        lines.Append("intern ").Append(tables.Intern.Live).Append('/').Append(tables.Intern.Distinct)
            .Append(" capacity ").Append(tables.Intern.Capacity)
            .Append(" graveyard ").Append(tables.Intern.GraveyardBlocks).Append('/').Append(tables.Intern.GraveyardBytes).Append('\n');
        lines.Append("pair ").Append(tables.Pair.Pairs).Append('/').Append(tables.Pair.Slots)
            .Append(" consumers ").Append(tables.Pair.Consumers).Append('/').Append(tables.Pair.ConsumerCapacity)
            .Append(" slotRow ").Append(tables.Pair.SlotRowBytes).Append('\n');
        lines.Append("bake ").Append(tables.Bake.Entries).Append('/').Append(tables.Bake.Slots).Append('\n');
        foreach (var bank in banks)
        {
            lines.Append("bank ").Append(bank.Snapshot.Track).Append(',').Append(bank.Snapshot.Clip)
                .Append(" views ").Append(bank.Views.Count)
                .Append(" blocks ").Append(bank.Snapshot.Blocks)
                .Append(" hits ").Append(bank.Snapshot.DedupeHits)
                .Append(" retained ").Append(bank.Snapshot.RetainedBytes).Append("B\n");
            foreach (var view in bank.Views)
            {
                lines.Append("  ").Append(view.Index).Append(' ').Append(view.FoldState);
                if (view is { FoldState: not "absent" and not "pending", Duration: not null })
                    lines.Append(" duration ").Append(view.Duration.Value)
                        .Append(" ticks ").Append(view.Ticks!.Value)
                        .Append(" lanes ").Append(view.Lanes!.Value);
                lines.Append('\n');
            }
        }
        return lines.ToString();
    }

    static HashSet<ulong> AssetPairKeys(byte[] bytes)
    {
        var keys = new HashSet<ulong>();
        var pairCount = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24));
        var pairOffset = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(28));
        for (var index = 0; index < pairCount; index++)
            keys.Add(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(pairOffset + 48 * index)));
        return keys;
    }

    static List<TlbBank> Banks(TlbEngine? engine, Assembly assembly, bool resolve, bool addresses, List<(int Index, HashSet<ulong> Pairs)> assets)
    {
        var banks = new List<TlbBank>();
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = Array.FindAll(ex.Types, static type => type is not null)!;
        }
        var blend = typeof(IBlend<>);
        var candidates = new SortedList<string, (Type Track, Type Clip)>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            if (!type.IsValueType) continue;
            foreach (var face in type.GetInterfaces())
            {
                if (!face.IsConstructedGenericType || face.GetGenericTypeDefinition() != blend) continue;
                var clip = face.GetGenericArguments()[0];
                if (!BakerAssemblyResolver.IsUnmanaged(type) || !BakerAssemblyResolver.IsUnmanaged(clip)) continue;
                candidates[$"{type.FullName}\0{clip.FullName}"] = (type, clip);
            }
        }

        foreach (var (_, pair) in candidates)
        {
            var (track, clip) = pair;
            var snapshot = ReadBank(track, clip);
            var preFolded = new HashSet<int>();
            if (snapshot is not null)
                foreach (var view in snapshot.Views)
                    if (view.State == Inspection.FoldState.Folded)
                        preFolded.Add(view.Index);
            else if (!resolve)
                continue;
            if (resolve && engine is not null)
            {
                var closed = engine.TimelineGeneric.MakeGenericType(track, clip);
                var bankField = closed.GetField("_bank", BindingFlags.Static | BindingFlags.NonPublic)!;
                var bank = bankField.GetValue(null);
                if (bank is null)
                {
                    var setClosed = engine.SetGeneric.MakeGenericType(track, clip);
                    bank = Activator.CreateInstance(setClosed)!;
                    setClosed.GetField("_lazyResolve", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(bank, true);
                    bankField.SetValue(null, bank);
                }
                var determine = closed.GetMethod("Determine", BindingFlags.Static | BindingFlags.NonPublic)!;
                var distinct = Inspection.Tables().Intern.Distinct;
                for (var index = 0; index < distinct; index++)
                    determine.Invoke(null, [bank, (object)(ushort)index]);
                snapshot = ReadBank(track, clip);
            }
            if (snapshot is null)
                continue;

            MethodInfo? viewOf = null;
            if (addresses && engine is not null)
                viewOf = engine.TimelineGeneric.MakeGenericType(track, clip)
                    .GetMethod("View", BindingFlags.Static | BindingFlags.Public, [typeof(ushort)]);
            var views = new List<TlbView>(snapshot.Count + assets.Count);
            var classified = new HashSet<int>();
            foreach (var asset in assets)
                if (asset.Index >= snapshot.Count && classified.Add(asset.Index))
                    views.Add(new TlbView(asset.Index, asset.Pairs.Contains(snapshot.PairKey) ? "pending" : "absent", null, false, null, null, null, null, null, null));
            foreach (var view in snapshot.Views)
            {
                if (view.State != Inspection.FoldState.Folded)
                {
                    views.Add(new TlbView(view.Index, view.State == Inspection.FoldState.Absent ? "absent" : "pending", null, false, null, null, null, null, null, null));
                    continue;
                }
                long? address = null;
                if (viewOf is not null)
                    address = (long)((SlotView)viewOf.Invoke(null, [(object)(ushort)view.Index])!).Forward - 72;
                views.Add(new TlbView(
                    view.Index,
                    resolve && !preFolded.Contains(view.Index) ? "folded-now" : "folded",
                    view.Duration,
                    view.Looping ?? false,
                    view.Ticks,
                    view.Lanes,
                    view.AbiVersion,
                    view.Generation,
                    address,
                    view.LaneShapes));
            }
            banks.Add(new TlbBank(snapshot, views));
        }
        return banks;
    }

    static Inspection.BankSnapshot? ReadBank(Type track, Type clip)
        => (Inspection.BankSnapshot?)typeof(Inspection)
            .GetMethod(nameof(Inspection.Bank), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(track, clip)
            .Invoke(null, null);

    sealed record TlbView(int Index, string FoldState, ushort? Duration, bool Looping, int? Ticks, int? Lanes, int? AbiVersion, long? Generation, long? Address, IReadOnlyList<Inspection.LaneShape>? LaneShapes);

    sealed record TlbBank(Inspection.BankSnapshot Snapshot, List<TlbView> Views);
}

internal sealed class TlbHostContext(string assemblyPath) : AssemblyLoadContext("TlLiveHost", isCollectible: false)
{
    readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

    protected override Assembly? Load(AssemblyName name)
    {
        if (name.Name == "Tl.Core") return null;
        var resolved = _resolver.ResolveAssemblyToPath(name);
        return resolved is null ? null : LoadFromAssemblyPath(resolved);
    }
}

internal sealed class TlbEngine(Type timelineGeneric, Type timelineSetGeneric)
{
    internal static readonly TlbEngine? Live = Create();

    internal readonly Type TimelineGeneric = timelineGeneric;
    internal readonly Type SetGeneric = timelineSetGeneric;

    static TlbEngine? Create()
    {
        try
        {
            var core = typeof(TimelineAsset).Assembly;
            var timelineGeneric = core.GetType("Tl.Timeline`2", throwOnError: false)!;
            var timelineSetGeneric = core.GetType("Tl.TimelineSet`2", throwOnError: false)!;
            if (timelineGeneric is null || timelineSetGeneric is null)
                return null;
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            foreach (var member in new MemberInfo?[]
                     {
                         timelineGeneric.GetField("_bank", flags),
                         timelineGeneric.GetMethod("Determine", flags),
                         timelineGeneric.GetMethod("View", BindingFlags.Static | BindingFlags.Public, [typeof(ushort)]),
                         timelineSetGeneric.GetField("_lazyResolve", BindingFlags.Instance | BindingFlags.NonPublic),
                     })
                if (member is null)
                    throw new InvalidOperationException("engine pin missed");
            return new TlbEngine(timelineGeneric, timelineSetGeneric);
        }
        catch
        {
            return null;
        }
    }
}
