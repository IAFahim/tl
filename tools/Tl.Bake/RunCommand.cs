using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal static unsafe class RunCommand
{
    const string Usage = "Usage: tl run <scene.json> --steps <n> --assembly <path>... [--library game.tlib] [--checkpoints <every>] [--show]";

    delegate void SystemRun(TimelineWorld world, PlaybackTable players, bool forward, nint* columns, int count);

    internal static int Run(string[] args)
    {
        string? scenePath = null, libraryPath = null;
        int steps = -1, every = 0;
        var show = false;
        var assemblies = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--assembly" or "-a" or "--library" or "--steps" or "--checkpoints")
            {
                if (i + 1 >= args.Length) return Fail($"Missing argument for {args[i]}");
                var value = args[++i];
                if (args[i - 1] == "--library") libraryPath = value;
                else if (args[i - 1] == "--steps") { if (!int.TryParse(value, out steps) || steps < 0) return Fail("--steps needs a non-negative integer."); }
                else if (args[i - 1] == "--checkpoints") { if (!int.TryParse(value, out every) || every <= 0) return Fail("--checkpoints needs a positive integer."); }
                else assemblies.Add(value);
            }
            else if (args[i] == "--show") show = true;
            else if (args[i].StartsWith('-') || scenePath != null) return Fail($"Unexpected argument '{args[i]}'");
            else scenePath = args[i];
        }
        if (scenePath == null || steps < 0 || assemblies.Count == 0) return Fail(Usage);
        var hash = Scene(scenePath, libraryPath, assemblies, steps, every, show, Console.Out, out var entities);
        Console.WriteLine($"hash={hash} steps={steps} entities={entities}");
        return 0;
    }

    internal static ulong Scene(string scenePath, string? libraryPath, List<string> assemblies, int steps, int every, bool show, TextWriter output, out int entities)
    {
        var json = File.ReadAllBytes(scenePath);
        var resolver = new BakerAssemblyResolver(assemblies);
        var library = libraryPath != null ? File.ReadAllBytes(libraryPath) : BakeProject(scenePath, json, assemblies);
        var scene = TlScene.Read(json, resolver, TlScene.ManifestOf(library, out _));
        entities = scene.Entities.Count;
        return Execute(scene, library, resolver.ReferencedAssemblies, steps, every, show, output);
    }

    internal static ulong Execute(TlScene scene, byte[] library, IEnumerable<Assembly> assemblies, int steps, int every, bool show, TextWriter output)
    {
        var count = scene.Entities.Count;
        var systems = Systems(assemblies);
        var columns = new Dictionary<Type, nint>();
        foreach (var type in scene.Columns.Concat(systems.SelectMany(s => s.Columns)))
            if (!columns.ContainsKey(type)) columns[type] = (nint)NativeMemory.AllocZeroed((nuint)(Math.Max(1, count) * Size(type)));
        try
        {
            for (var e = 0; e < count; e++)
                foreach (var (type, bytes) in scene.Entities[e].States)
                    bytes.CopyTo(new Span<byte>((byte*)columns[type] + e * bytes.Length, bytes.Length));
            using var world = new TimelineWorld(library);
            using var players = new PlaybackTable(Math.Max(1, count));
            _ = new KindStore(world, players);
            var pointers = stackalloc nint[16];
            var hash = TlStateHash.Begin();
            var forward = true;
            for (var step = 0; step < steps; step++)
            {
                forward = scene.ForwardAt(step, forward);
                if (step == 0)
                    for (var e = 0; e < count; e++)
                        foreach (var (timeline, tick) in scene.Entities[e].Play) players.Start(e, timeline, tick);
                foreach (var request in scene.Requests)
                    if (request.Step == step) Apply(players, request);
                world.Tick(players, forward);
                foreach (var system in systems)
                {
                    for (var c = 0; c < system.Columns.Length; c++) pointers[c] = columns[system.Columns[c]];
                    system.Run(world, players, forward, pointers, count);
                }
                var view = players.View;
                for (var e = 0; e < count; e++)
                {
                    hash.Slots(view.SlotOf(e, 0), view.CountOf(e));
                    foreach (var type in scene.Columns) hash.Bytes((byte*)columns[type] + e * Size(type), Size(type));
                }
                if (every > 0 && (step + 1) % every == 0)
                {
                    output.WriteLine($"step {step + 1} hash={hash.Value}");
                    if (show) Show(scene, columns, output);
                }
            }
            if (show && (every == 0 || steps % every != 0)) Show(scene, columns, output);
            return hash.Value;
        }
        finally
        {
            foreach (var pointer in columns.Values) NativeMemory.Free((void*)pointer);
        }
    }

    static void Apply(PlaybackTable players, in TlScene.Request r)
    {
        switch (r.Op)
        {
            case RequestOp.Start: players.Start(r.Entity, r.Target, r.Value); break;
            case RequestOp.Stop: players.Stop(r.Entity, r.Target); break;
            case RequestOp.Pause: players.Pause(r.Entity, r.Target); break;
            case RequestOp.Resume: players.Resume(r.Entity, r.Target); break;
            case RequestOp.Seek: players.Seek(r.Entity, r.Target, r.Value); break;
            case RequestOp.Reverse: players.Reverse(r.Entity, r.Target, r.Value != 0); break;
            case RequestOp.StopAll: players.StopAll(r.Entity); break;
            case RequestOp.StopChannel: players.StopChannel(r.Entity, (byte)r.Target); break;
            case RequestOp.PauseChannel: players.PauseChannel(r.Entity, (byte)r.Target); break;
            case RequestOp.ResumeChannel: players.ResumeChannel(r.Entity, (byte)r.Target); break;
            case RequestOp.SeekChannel: players.SeekChannel(r.Entity, (byte)r.Target, r.Value); break;
            case RequestOp.ReverseChannel: players.ReverseChannel(r.Entity, (byte)r.Target, r.Value != 0); break;
        }
    }

    static void Show(TlScene scene, Dictionary<Type, nint> columns, TextWriter output)
    {
        for (var e = 0; e < scene.Entities.Count; e++)
            foreach (var type in scene.Columns)
            {
                var value = Marshal.PtrToStructure((nint)((byte*)columns[type] + e * Size(type)), type)!;
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => $"{f.Name} {f.GetValue(value)}");
                output.WriteLine($"  {scene.Entities[e].Name ?? e.ToString()} {type.Name}: {string.Join(", ", fields)}");
            }
    }

    static int Size(Type type) => TimelineBakerFast.HelperFor(type).Size;

    static List<(Type[] Columns, SystemRun Run)> Systems(IEnumerable<Assembly> assemblies)
    {
        var found = new List<(string Name, Type[] Columns, SystemRun Run)>();
        foreach (var assembly in assemblies)
            foreach (var type in BakerAssemblyResolver.GetTypesSafe(assembly))
            {
                if (!type.IsValueType || !type.IsByRefLike || type.GetCustomAttribute<TimelineSystemAttribute>() == null) continue;
                var run = type.GetMethod("Run", [typeof(TimelineWorld), typeof(PlaybackTable), typeof(bool)]);
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (run == null || fields.Length > 16 || fields.Any(f => !f.FieldType.IsGenericType || f.FieldType.GetGenericTypeDefinition() != typeof(Span<>))) continue;
                found.Add((type.FullName!, fields.Select(f => f.FieldType.GetGenericArguments()[0]).ToArray(), Emit(type, run, fields)));
            }
        return found.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => (s.Columns, s.Run)).ToList();
    }

    static SystemRun Emit(Type system, MethodInfo run, FieldInfo[] fields)
    {
        var method = new DynamicMethod("Run" + system.Name, null, [typeof(TimelineWorld), typeof(PlaybackTable), typeof(bool), typeof(nint*), typeof(int)], typeof(RunCommand).Module, true);
        var il = method.GetILGenerator();
        var local = il.DeclareLocal(system);
        il.Emit(OpCodes.Ldloca, local);
        il.Emit(OpCodes.Initobj, system);
        for (var i = 0; i < fields.Length; i++)
        {
            var span = fields[i].FieldType;
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldind_I);
            il.Emit(OpCodes.Ldarg, 4);
            il.Emit(OpCodes.Newobj, span.GetConstructor([typeof(void*), typeof(int)])!);
            il.Emit(OpCodes.Stfld, fields[i]);
        }
        il.Emit(OpCodes.Ldloca, local);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, run);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<SystemRun>();
    }

    internal static byte[] BakeProject(string scenePath, byte[] json, List<string> assemblies)
    {
        using var document = TlJson.ReadScene(json);
        if (!document.RootElement.TryGetProperty("project", out var project)) throw new BakeDiagnosticException("scene names no project; pass --library game.tlib or add \"project\".");
        var projectPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(scenePath))!, project.GetString()!);
        var input = LibraryPack.LoadKindInputs(projectPath, assemblies, false);
        return LibraryAssembler.Assemble(input.Parts, input.TickRate, input.Settings, out _);
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
