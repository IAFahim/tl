using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        var engineVersion = VersionOf(typeof(TimelineAsset).Assembly.GetName());
        var banks = engine is null ? [] : Banks(engine, assembly, resolve, [.. assets.Select(asset => (asset.Index, asset.Pairs))]);
        if (engine is not null)
            foreach (var bank in banks)
                if (bank.Retained != bank.Header + bank.Table + bank.Directory + bank.Arena)
                {
                    Console.Error.WriteLine($"Snapshot inconsistency: bank {bank.Track},{bank.Clip} retained {bank.Retained} != {bank.Header + bank.Table + bank.Directory + bank.Arena}.");
                    return 5;
                }

        if (summary)
        {
            Console.Write(Summary(engine, assemblyName.Name ?? "", VersionOf(assemblyName), Sha(assemblyBytes), assets, banks, engineVersion));
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
            if (engine is not null)
            {
                W("tables");
                W("intern");
                N("capacity", engine.InternCapacity());
                N("live", engine.Intern<long>("LiveEntries"));
                N("distinct", engine.Intern<long>("DistinctContents"));
                N("graveyardBlocks", engine.Intern<long>("GraveyardBlocks"));
                N("graveyardBytes", engine.Intern<long>("GraveyardBytes"));
                N("allocBytes", engine.Intern<long>("AllocBytes"));
                N("peakLiveBytes", engine.Intern<long>("PeakLiveBytes"));
                E();
                W("pair");
                var slots = engine.PairSlots();
                var pairs = engine.PairPairs();
                N("slots", slots);
                N("pairs", pairs);
                N("loadFactorPermille", slots > 0 ? pairs * 1000 / slots : 0);
                N("consumers", engine.Intern<long>("ConsumerCount", engine.PairTable));
                N("consumerCapacity", engine.PairConsumers());
                N("slotRowBytes", engine.PairSlotRow());
                E();
                W("bake");
                N("slots", engine.BakeSlotCount());
                N("entries", engine.Intern<long>("Count", engine.BakeTable));
                E();
                E();
                A("banks");
                foreach (var bank in banks)
                {
                    writer.WriteStartObject();
                    S("track", bank.Track);
                    S("clip", bank.Clip);
                    S("pairKey", Hex(bank.PairKey));
                    N("count", bank.Count);
                    N("holes", bank.Holes);
                    N("blocks", bank.Blocks);
                    N("dedupeHits", bank.DedupeHits);
                    N("generation", bank.Generation);
                    W("bytes");
                    N("header", bank.Header);
                    N("table", bank.Table);
                    N("directory", bank.Directory);
                    N("arena", bank.Arena);
                    N("retained", bank.Retained);
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
                            N("generation", unchecked((long)view.ViewGeneration!.Value));
                            if (addresses) S("address", Hex(unchecked((ulong)view.Address!.Value)));
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
            }
            W("deep");
            writer.WriteBoolean("available", engine is not null);
            if (engine is null) S("reason", "Tl.Core internals for the pinned snapshot plane were not found in this engine build.");
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

    static void WriteShape(Utf8JsonWriter writer, (ulong Key, int Distinct, int LongestRun) shape)
    {
        writer.WriteStartObject();
        writer.WriteString("key", Hex(shape.Key));
        writer.WriteNumber("distinct", shape.Distinct);
        writer.WriteNumber("longestRun", shape.LongestRun);
        writer.WriteEndObject();
    }

    static string Summary(TlbEngine? engine, string assemblyName, string assemblyVersion, string assemblySha, List<(string File, int Bytes, int Index, string Sha, HashSet<ulong> Pairs)> assets, List<TlbBank> banks, string engineVersion)
    {
        var lines = new StringBuilder();
        lines.Append("engine Tl.Core ").Append(engineVersion).Append('\n');
        lines.Append("assembly ").Append(assemblyName).Append(' ').Append(assemblyVersion).Append(" sha256 ").Append(assemblySha[..16]).Append('\n');
        lines.Append("assets ").Append(assets.Count).Append('\n');
        if (engine is null)
        {
            lines.Append("deep unavailable\n");
            return lines.ToString();
        }
        lines.Append("intern ").Append(engine.Intern<long>("LiveEntries")).Append('/').Append(engine.Intern<long>("DistinctContents"))
            .Append(" capacity ").Append(engine.InternCapacity())
            .Append(" graveyard ").Append(engine.Intern<long>("GraveyardBlocks")).Append('/').Append(engine.Intern<long>("GraveyardBytes")).Append('\n');
        lines.Append("pair ").Append(engine.PairPairs()).Append('/').Append(engine.PairSlots())
            .Append(" consumers ").Append(engine.Intern<long>("ConsumerCount", engine.PairTable)).Append('/').Append(engine.PairConsumers())
            .Append(" slotRow ").Append(engine.PairSlotRow()).Append('\n');
        lines.Append("bake ").Append(engine.Intern<long>("Count", engine.BakeTable)).Append('/').Append(engine.BakeSlotCount()).Append('\n');
        foreach (var bank in banks)
        {
            lines.Append("bank ").Append(bank.Track).Append(',').Append(bank.Clip)
                .Append(" views ").Append(bank.Views.Count)
                .Append(" blocks ").Append(bank.Blocks)
                .Append(" hits ").Append(bank.DedupeHits)
                .Append(" retained ").Append(bank.Retained).Append("B\n");
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

    static List<TlbBank> Banks(TlbEngine engine, Assembly assembly, bool resolve, List<(int Index, HashSet<ulong> Pairs)> assets)
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

        var liveIndices = Array.Empty<int>();
        if (resolve)
        {
            var distinct = (int)engine.Intern<long>("DistinctContents");
            liveIndices = new int[distinct];
            var count = 0;
            for (var index = 0; index < distinct; index++)
                if ((bool)engine.TimelineTable.GetMethod("IsLive", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [(object)(ushort)index])!)
                    liveIndices[count++] = index;
            Array.Resize(ref liveIndices, count);
        }

        foreach (var (_, pair) in candidates)
        {
            var (track, clip) = pair;
            var closed = engine.TimelineGeneric.MakeGenericType(track, clip);
            var bankField = closed.GetField("_bank", BindingFlags.Static | BindingFlags.NonPublic)!;
            var bank = bankField.GetValue(null);
            if (bank is null && !resolve) continue;
            if (bank is null)
            {
                var setClosed = engine.TimelineSetGeneric.MakeGenericType(track, clip);
                bank = Activator.CreateInstance(setClosed)!;
                setClosed.GetField("_lazyResolve", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(bank, true);
                bankField.SetValue(null, bank);
            }
            var setType = bank.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var isFolded = setType.GetMethod("IsFolded", flags)!;
            var isAbsent = setType.GetMethod("IsAbsent", flags)!;
            var isPending = setType.GetMethod("IsPending", flags)!;
            var view = closed.GetMethod("View", BindingFlags.Static | BindingFlags.Public, [typeof(ushort)])!;
            var pairKey = (ulong)engine.PairRuntimeGeneric.MakeGenericType(track, clip).GetField("Key")!.GetValue(null)!;

            var preFolded = new HashSet<int>();
            if (resolve)
            {
                foreach (var index in liveIndices)
                {
                    if ((bool)isFolded.Invoke(bank, [(object)(ushort)index])!) preFolded.Add(index);
                }
                var determine = closed.GetMethod("Determine", BindingFlags.Static | BindingFlags.NonPublic)!;
                foreach (var index in liveIndices)
                    determine.Invoke(null, [bank, (object)(ushort)index]);
            }

            var count = (int)setType.GetField("_count", flags)!.GetValue(bank)!;
            var views = new List<TlbView>(count);
            var classified = new HashSet<int>();
            foreach (var asset in assets)
                if (asset.Index >= count && classified.Add(asset.Index))
                    views.Add(new TlbView(asset.Index, asset.Pairs.Contains(pairKey) ? "pending" : "absent", null, false, null, null, null, null, null, null));
            for (var index = 0; index < count; index++)
            {
                var boxedIndex = (object)(ushort)index;
                if ((bool)isFolded.Invoke(bank, [boxedIndex])!)
                {
                    var box = view.Invoke(null, [boxedIndex])!;
                    var slot = typeof(SlotView);
                    ushort U(string name) => (ushort)slot.GetField(name)!.GetValue(box)!;
                    uint U32(string name) => (uint)slot.GetField(name)!.GetValue(box)!;
                    ulong U64(string name) => (ulong)slot.GetField(name)!.GetValue(box)!;
                    nint P(string name) => (nint)Pointer.Unbox(slot.GetField(name)!.GetValue(box)!);
                    var ticks = (int)U32("TableTicks");
                    var lanes = (int)U("ResultCount");
                    var shapes = LaneShapes((float*)P("Forward"), (byte*)P("BackwardRecords"), ticks, lanes, U("RecordBytes"));
                    views.Add(new TlbView(
                        index,
                        resolve && !preFolded.Contains(index) ? "folded-now" : "folded",
                        U("Duration"),
                        U("Looping") != 0,
                        ticks,
                        lanes,
                        (int)U("AbiVersion"),
                        U64("Generation"),
                        (long)P("Forward") - 72,
                        shapes));
                    continue;
                }
                var foldState = (bool)isAbsent.Invoke(bank, [boxedIndex])! ? "absent" : "pending";
                views.Add(new TlbView(index, foldState, null, false, null, null, null, null, null, null));
            }

            long Num(string name) => Convert.ToInt64(setType.GetProperty(name, flags)!.GetValue(bank));
            banks.Add(new TlbBank(
                track.FullName ?? track.Name,
                clip.FullName ?? clip.Name,
                pairKey,
                count,
                (int)setType.GetField("_holes", flags)!.GetValue(bank)!,
                (int)Num("BlockCount"),
                (int)Num("SharedHits"),
                Convert.ToInt64(setType.GetField("_generation", flags)!.GetValue(bank)),
                Num("HeaderBytes"),
                Num("TableBytes"),
                Num("DirectoryBytes"),
                Num("ArenaBytes"),
                Num("RetainedBytes"),
                views));
        }
        return banks;
    }

    static List<(ulong, int, int)> LaneShapes(float* forward, byte* backwardRecords, int ticks, int lanes, int recordBytes)
    {
        var shapes = new List<(ulong, int, int)>(lanes);
        var keys = (ulong*)(backwardRecords + (long)ticks * lanes * recordBytes);
        for (var lane = 0; lane < lanes; lane++)
        {
            var values = forward + (long)lane * ticks;
            var seen = new HashSet<uint>(ticks);
            var longest = 0;
            var run = 0;
            uint prior = 0;
            for (var tick = 0; tick < ticks; tick++)
            {
                var bits = *(uint*)(values + tick);
                seen.Add(bits);
                run = tick > 0 && bits == prior ? run + 1 : 1;
                if (run > longest) longest = run;
                prior = bits;
            }
            shapes.Add((keys[lane], seen.Count, longest));
        }
        return shapes;
    }

    sealed record TlbView(int Index, string FoldState, ushort? Duration, bool Looping, int? Ticks, int? Lanes, int? AbiVersion, ulong? ViewGeneration, long? Address, List<(ulong Key, int Distinct, int LongestRun)>? LaneShapes);

    sealed record TlbBank(string Track, string Clip, ulong PairKey, int Count, int Holes, int Blocks, int DedupeHits, long Generation, long Header, long Table, long Directory, long Arena, long Retained, List<TlbView> Views);
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

internal sealed class TlbEngine(
    Type timelineTable,
    Type pairTable,
    Type bakeTable,
    Type timelineGeneric,
    Type timelineSetGeneric,
    Type pairRuntimeGeneric,
    FieldInfo internCapacity,
    FieldInfo pairPairs,
    FieldInfo pairConsumers,
    FieldInfo pairSlotsBase,
    FieldInfo pairSlotRow,
    FieldInfo bakeSlotCount)
{
    internal static readonly TlbEngine? Live = Create();

    internal readonly Type TimelineTable = timelineTable;
    internal readonly Type PairTable = pairTable;
    internal readonly Type BakeTable = bakeTable;
    internal readonly Type TimelineGeneric = timelineGeneric;
    internal readonly Type TimelineSetGeneric = timelineSetGeneric;
    internal readonly Type PairRuntimeGeneric = pairRuntimeGeneric;
    readonly FieldInfo _internCapacity = internCapacity;
    readonly FieldInfo _pairPairs = pairPairs;
    readonly FieldInfo _pairConsumers = pairConsumers;
    readonly FieldInfo _pairSlotsBase = pairSlotsBase;
    readonly FieldInfo _pairSlotRow = pairSlotRow;
    readonly FieldInfo _bakeSlotCount = bakeSlotCount;

    static TlbEngine? Create()
    {
        try
        {
            var core = typeof(TimelineAsset).Assembly;
            var timelineTable = core.GetType("Tl.TimelineTable", throwOnError: false)!;
            var pairTable = core.GetType("Tl.PairTable", throwOnError: false)!;
            var bakeTable = core.GetType("Tl.BakeTable", throwOnError: false)!;
            var timelineGeneric = core.GetType("Tl.Timeline`2", throwOnError: false)!;
            var timelineSetGeneric = core.GetType("Tl.TimelineSet`2", throwOnError: false)!;
            var pairRuntimeGeneric = core.GetType("Tl.PairRuntime`2", throwOnError: false)!;
            if (timelineTable is null || pairTable is null || bakeTable is null || timelineGeneric is null || timelineSetGeneric is null || pairRuntimeGeneric is null)
                return null;
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var internCapacity = timelineTable.GetField("_capacity", flags) ?? throw new InvalidOperationException("_capacity");
            var pairPairs = pairTable.GetField("_pairs", flags) ?? throw new InvalidOperationException("_pairs");
            var pairConsumers = pairTable.GetField("_consumerCapacity", flags) ?? throw new InvalidOperationException("_consumerCapacity");
            var pairSlotsBase = pairTable.GetField("_slotsBase", flags) ?? throw new InvalidOperationException("_slotsBase");
            var pairSlotRow = pairTable.GetField("SlotRow", flags) ?? throw new InvalidOperationException("SlotRow");
            var bakeSlotCount = bakeTable.GetField("SlotCount", flags) ?? throw new InvalidOperationException("SlotCount");
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var member in new MemberInfo?[]
                     {
                         timelineTable.GetMethod("IsLive", flags),
                         pairTable.GetMethod("HeadOf", flags),
                         timelineGeneric.GetField("_bank", flags),
                         timelineGeneric.GetMethod("Determine", flags),
                         timelineGeneric.GetMethod("View", BindingFlags.Static | BindingFlags.Public, [typeof(ushort)]),
                         timelineSetGeneric.GetField("_lazyResolve", instance),
                         timelineSetGeneric.GetField("_count", instance),
                         timelineSetGeneric.GetField("_holes", instance),
                         timelineSetGeneric.GetField("_generation", instance),
                         timelineSetGeneric.GetMethod("IsFolded", instance),
                         timelineSetGeneric.GetMethod("IsAbsent", instance),
                         timelineSetGeneric.GetMethod("IsPending", instance),
                         pairRuntimeGeneric.GetField("Key"),
                     })
                if (member is null) throw new InvalidOperationException("engine pin missed");
            foreach (var name in new[] { "RetainedBytes", "HeaderBytes", "TableBytes", "DirectoryBytes", "ArenaBytes", "BlockCount", "SharedHits" })
                if (timelineSetGeneric.GetProperty(name, instance) is null) throw new InvalidOperationException(name);
            foreach (var name in new[] { "LiveEntries", "DistinctContents", "GraveyardBlocks", "GraveyardBytes", "AllocBytes", "PeakLiveBytes" })
                if (timelineTable.GetProperty(name, flags) is null) throw new InvalidOperationException(name);
            if (pairTable.GetProperty("ConsumerCount", flags) is null || bakeTable.GetProperty("Count", flags) is null)
                throw new InvalidOperationException("table count pin missed");
            return new TlbEngine(timelineTable, pairTable, bakeTable, timelineGeneric, timelineSetGeneric, pairRuntimeGeneric, internCapacity, pairPairs, pairConsumers, pairSlotsBase, pairSlotRow, bakeSlotCount);
        }
        catch
        {
            return null;
        }
    }

    internal long Intern<T>(string name, Type? type = null)
    {
        var source = type ?? TimelineTable;
        return Convert.ToInt64(source.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null));
    }

    internal long InternCapacity() => Convert.ToInt64(_internCapacity.GetValue(null));
    internal long PairPairs() => Convert.ToInt64(_pairPairs.GetValue(null));
    internal long PairConsumers() => Convert.ToInt64(_pairConsumers.GetValue(null));
    internal long PairSlotRow() => Convert.ToInt64(_pairSlotRow.GetRawConstantValue());
    internal long BakeSlotCount() => Convert.ToInt64(_bakeSlotCount.GetRawConstantValue());

    internal long PairSlots()
    {
        var block = Convert.ToInt64(_pairSlotsBase.GetValue(null));
        return block == 0 ? 0 : Marshal.ReadInt32((nint)block);
    }
}
