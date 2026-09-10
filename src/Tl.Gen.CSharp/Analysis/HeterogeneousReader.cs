using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public sealed record DeclarationDiagnostic(
    string File,
    int Line,
    int Column,
    string Code,
    string Message)
{
    internal int SpanStart { get; init; } = -1;
    internal int SpanLength { get; init; }
    internal int EndLine { get; init; }
    internal int EndColumn { get; init; }

    public override string ToString() => $"{File}({Line},{Column}): error {Code}: {Message}";
}

public sealed record HeterogeneousCompilationSettings
{
    public IReadOnlyList<string> ReferencePaths { get; init; } = [];
    public string LanguageVersion { get; init; } = "preview";
    public string Nullable { get; init; } = "enable";
    public bool AllowUnsafe { get; init; }
    public bool CheckOverflow { get; init; }
}

public static class HeterogeneousReader
{
    private sealed record Contracts(
        INamedTypeSymbol Timeline,
        INamedTypeSymbol Track,
        INamedTypeSymbol Hook,
        INamedTypeSymbol Frame,
        INamedTypeSymbol Builder,
        INamedTypeSymbol Playback,
        INamedTypeSymbol TypedPlayback);

    private sealed record TimelineEntry(
        INamedTypeSymbol Symbol,
        TypeDeclarationSyntax Syntax);

#if NET10_0
    public static (IReadOnlyList<HeterogeneousTimeline> Timelines, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        Read(IReadOnlyList<(string Path, string Source)> sources, IReadOnlyList<string>? symbols = null)
        => Read(sources, symbols, new HeterogeneousCompilationSettings { ReferencePaths = DefaultReferences() });

    public static (IReadOnlyList<HeterogeneousTimeline> Timelines, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        Read(
            IReadOnlyList<(string Path, string Source)> sources,
            IReadOnlyList<string>? symbols,
            IReadOnlyList<string> referencePaths)
        => Read(sources, symbols, new HeterogeneousCompilationSettings { ReferencePaths = referencePaths });

    public static (IReadOnlyList<HeterogeneousTimeline> Timelines, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        Read(
            IReadOnlyList<(string Path, string Source)> sources,
            IReadOnlyList<string>? symbols,
            HeterogeneousCompilationSettings settings)
    {
        var diagnostics = new List<DeclarationDiagnostic>();
        var languageVersion = ParseLanguageVersion(settings.LanguageVersion);
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(languageVersion)
            .WithPreprocessorSymbols(symbols ?? []);
        var trees = sources
            .Select(source => CSharpSyntaxTree.ParseText(source.Source, parseOptions, source.Path))
            .ToArray();
        var references = References(settings.ReferencePaths, diagnostics);
        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: settings.AllowUnsafe,
            checkOverflow: settings.CheckOverflow,
            nullableContextOptions: ParseNullable(settings.Nullable));
        var compilation = CSharpCompilation.Create("Tl.Generated.Analysis", trees, references, options);
        return Read(compilation, trees, null, diagnostics);
    }
#endif

    internal static (IReadOnlyList<HeterogeneousTimeline> Timelines, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        ReadCandidate(CSharpCompilation compilation, TypeDeclarationSyntax candidate)
    {
        var symbol = compilation.GetSemanticModel(candidate.SyntaxTree).GetDeclaredSymbol(candidate);
        return symbol is null
            ? ([], [])
            : Read(compilation, compilation.SyntaxTrees, symbol, []);
    }

    private static (IReadOnlyList<HeterogeneousTimeline> Timelines, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        Read(
            CSharpCompilation compilation,
            IEnumerable<SyntaxTree> trees,
            INamedTypeSymbol? requested,
            List<DeclarationDiagnostic> diagnostics)
    {
        var treeArray = trees.ToArray();
        var contracts = ReadContracts(compilation);
        if (contracts is null)
        {
            if (treeArray.Any(static tree => tree.GetRoot().DescendantNodes().OfType<BaseTypeSyntax>().Any(static type => type.Type.ToString().IndexOf("ITimeline", StringComparison.Ordinal) >= 0)))
                diagnostics.Add(new DeclarationDiagnostic(treeArray[0].FilePath, 1, 1, "TLGEN20", "The exact Tl compilation contracts could not be resolved from metadata references."));
            return ([], diagnostics);
        }

        var entries = SourceTypes(compilation, treeArray)
            .Where(pair => Implements(pair.Symbol, contracts.Timeline))
            .OrderBy(pair => Display(pair.Symbol), StringComparer.Ordinal)
            .ToArray();
        var bySymbol = new Dictionary<INamedTypeSymbol, TimelineEntry>(SymbolEqualityComparer.Default);
        foreach (var pair in entries)
        {
            var declarations = pair.Symbol.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .OrderBy(static syntax => syntax.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(static syntax => syntax.SpanStart)
                .ToArray();
            var declaration = declarations.FirstOrDefault(syntax => syntax.BaseList?.Types.Any(type =>
                compilation.GetSemanticModel(syntax.SyntaxTree).GetTypeInfo(type.Type).Type is INamedTypeSymbol candidate
                && Same(candidate, contracts.Timeline)) == true) ?? declarations[0];
            bySymbol[pair.Symbol] = new TimelineEntry(
                pair.Symbol,
                declaration);
        }

        var resolver = new Resolver(compilation, contracts, bySymbol, diagnostics);
        var timelines = entries
            .Where(pair => requested is null || Same(pair.Symbol, requested))
            .Select(pair => resolver.Resolve(pair.Symbol, pair.Syntax))
            .Where(static timeline => timeline is not null)
            .Cast<HeterogeneousTimeline>()
            .ToArray();
        return (timelines, diagnostics);
    }

    private sealed class Resolver(
        CSharpCompilation compilation,
        Contracts contracts,
        IReadOnlyDictionary<INamedTypeSymbol, TimelineEntry> declarations,
        List<DeclarationDiagnostic> diagnostics)
    {
        private readonly Dictionary<INamedTypeSymbol, HeterogeneousTimeline> _resolved = new(SymbolEqualityComparer.Default);
        private readonly HashSet<INamedTypeSymbol> _failed = new(SymbolEqualityComparer.Default);
        private readonly List<INamedTypeSymbol> _stack = [];
        private readonly Dictionary<TimelineSlot, SyntaxNode> _slotSites = new(ReferenceComparer<TimelineSlot>.Instance);

        public HeterogeneousTimeline? Resolve(INamedTypeSymbol symbol, SyntaxNode site)
        {
            if (_resolved.TryGetValue(symbol, out var existing))
                return existing;
            if (_failed.Contains(symbol))
                return null;
            var cycleStart = _stack.FindIndex(candidate => Same(candidate, symbol));
            if (cycleStart >= 0)
            {
                var cycle = string.Join(" -> ", _stack.Skip(cycleStart).Append(symbol).Select(Display));
                Add(diagnostics, site, "TLGEN42", $"Timeline include cycle: {cycle}.");
                _failed.Add(symbol);
                return null;
            }
            if (!declarations.TryGetValue(symbol, out var declaration))
            {
                Add(diagnostics, site, "TLGEN41", $"Included timeline '{Display(symbol)}' was not found in this compilation.");
                return null;
            }

            _stack.Add(symbol);
            var timeline = ReadTimeline(declaration);
            _stack.RemoveAt(_stack.Count - 1);
            if (timeline is null)
                _failed.Add(symbol);
            else
                _resolved.Add(symbol, timeline);
            return timeline;
        }

        private HeterogeneousTimeline? ReadTimeline(TimelineEntry declaration)
        {
            var syntaxDeclarations = declaration.Symbol.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .ToArray();
            if (declaration.Symbol.DeclaredAccessibility != Accessibility.Public
                || !declaration.Symbol.IsReadOnly
                || !declaration.Symbol.IsUnmanagedType
                || declaration.Symbol.Arity != 0
                || declaration.Symbol.ContainingType is not null
                || syntaxDeclarations.Any(static syntax => syntax is not StructDeclarationSyntax)
                || syntaxDeclarations.Any(static syntax => !Has(syntax.Modifiers, SyntaxKind.PartialKeyword)))
            {
                Add(diagnostics, declaration.Syntax, "TLGEN22", $"Timeline '{Display(declaration.Symbol)}' must be a public readonly partial top-level unmanaged non-generic struct.");
                return null;
            }

            var definitions = declaration.Symbol.GetMembers("Define")
                .OfType<IMethodSymbol>()
                .Where(static method => !method.IsImplicitlyDeclared)
                .ToArray();
            if (definitions.Length != 1
                || !ValidDefine(definitions[0])
                || definitions[0].Parameters[0].Type is not INamedTypeSymbol builderType
                || !Same(builderType, contracts.Builder)
                || MethodSyntax(definitions[0]) is not { Body: not null } define)
            {
                Add(diagnostics, declaration.Syntax, "TLGEN23", $"Timeline '{Display(declaration.Symbol)}' must define exactly one public static void Define(scoped Tl.Builder builder) method with a block body.");
                return null;
            }

            var model = compilation.GetSemanticModel(define.SyntaxTree);
            var builderParameter = definitions[0].Parameters[0];
            var tracks = new List<HeterogeneousTrack>();
            var clips = new List<HeterogeneousClip>();
            var beforeHooks = new List<TimelineHook>();
            var afterHooks = new List<TimelineHook>();
            var locals = new Dictionary<ILocalSymbol, (HeterogeneousTrack Track, INamedTypeSymbol ClipType)>(SymbolEqualityComparer.Default);
            var invalidLocals = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
            var included = new List<HeterogeneousTimeline>();
            var loopsDeclared = false;
            var includeDeclared = false;
            var valid = true;

            foreach (var member in declaration.Symbol.GetMembers()
                .Where(static member => !member.IsImplicitlyDeclared && member.Name is "Data" or "DynamicData" or "Start" or "TrySeek"))
            {
                Add(diagnostics, DeclarationSite(member, declaration.Syntax), "TLGEN52", $"Timeline '{Display(declaration.Symbol)}' declares '{member.Name}', which conflicts with generated member '{member.Name}'.");
                valid = false;
            }

            foreach (var statement in define.Body!.Statements)
            {
                if (statement is LocalDeclarationStatementSyntax local
                    && local.Declaration.Variables.Count == 1
                    && local.Declaration.Variables[0] is var variable
                    && variable.Initializer?.Value is InvocationExpressionSyntax trackCall
                    && BuilderCall(trackCall, model, builderParameter, "Track")
                    && trackCall.ArgumentList.Arguments.Count == 1)
                {
                    var expression = trackCall.ArgumentList.Arguments[0].Expression;
                    var typeInfo = model.GetTypeInfo(expression);
                    var trackType = (typeInfo.ConvertedType ?? typeInfo.Type) as INamedTypeSymbol;
                    if (trackType is null || expression is not BaseObjectCreationExpressionSyntax || !Constant(expression, model))
                    {
                        Add(diagnostics, expression, "TLGEN27", "Track values must be explicit unmanaged object constructions from compile-time constants.");
                        valid = false;
                        if (model.GetDeclaredSymbol(variable) is ILocalSymbol invalidLocal)
                            invalidLocals.Add(invalidLocal);
                        continue;
                    }
                    var contract = ReadTrack(trackType, declaration.Symbol, expression);
                    if (contract is null)
                    {
                        valid = false;
                        if (model.GetDeclaredSymbol(variable) is ILocalSymbol invalidLocal)
                            invalidLocals.Add(invalidLocal);
                        continue;
                    }
                    var track = new HeterogeneousTrack(
                        tracks.Count,
                        Display(trackType),
                        Display(contract.Value.ClipType),
                        Canonical(expression, model),
                        contract.Value.Seek);
                    tracks.Add(track);
                    if (model.GetDeclaredSymbol(variable) is not ILocalSymbol localSymbol || locals.ContainsKey(localSymbol))
                    {
                        Add(diagnostics, variable, "TLGEN25", $"Track local '{variable.Identifier.ValueText}' is invalid or declared more than once.");
                        valid = false;
                    }
                    else
                        locals.Add(localSymbol, (track, contract.Value.ClipType));
                    continue;
                }

                if (statement is not ExpressionStatementSyntax expressionStatement
                    || expressionStatement.Expression is not InvocationExpressionSyntax call)
                {
                    Add(diagnostics, statement, "TLGEN24", "Define contains an unsupported statement.");
                    valid = false;
                    continue;
                }

                if (BuilderCall(call, model, builderParameter, "Clip"))
                {
                    if (TrackLocal(call, model) is { } trackLocal && invalidLocals.Contains(trackLocal))
                        continue;
                    if (!ReadClip(call, model, locals, clips))
                        valid = false;
                    continue;
                }
                if (BuilderCall(call, model, builderParameter, "Looping") && call.ArgumentList.Arguments.Count == 0)
                {
                    if (loopsDeclared)
                    {
                        Add(diagnostics, call, "TLGEN33", "Looping may be declared only once.");
                        valid = false;
                    }
                    loopsDeclared = true;
                    continue;
                }
                if (GenericBuilderCall(call, model, builderParameter, "Before", out var beforeType))
                {
                    var hook = ReadHook(beforeType, declaration.Symbol, call);
                    if (hook is null)
                        valid = false;
                    else
                        beforeHooks.Add(hook);
                    continue;
                }
                if (GenericBuilderCall(call, model, builderParameter, "After", out var afterType))
                {
                    var hook = ReadHook(afterType, declaration.Symbol, call);
                    if (hook is null)
                        valid = false;
                    else
                        afterHooks.Add(hook);
                    continue;
                }
                if (GenericBuilderCall(call, model, builderParameter, "Include", out var includedType))
                {
                    if (includeDeclared)
                    {
                        Add(diagnostics, call, "TLGEN48", "A timeline may include at most one timeline.");
                        valid = false;
                        continue;
                    }
                    includeDeclared = true;
                    if (!Implements(includedType, contracts.Timeline))
                    {
                        Add(diagnostics, call, "TLGEN41", $"Included type '{Display(includedType)}' must implement the exact Tl.ITimeline contract.");
                        valid = false;
                        continue;
                    }
                    var nested = Resolve(includedType, call);
                    if (nested is null)
                    {
                        valid = false;
                        continue;
                    }
                    var offset = tracks.Count;
                    tracks.AddRange(nested.Tracks.Select(track => track with { Index = track.Index + offset }));
                    clips.AddRange(nested.Clips.Select(clip => clip with { TrackIndex = clip.TrackIndex + offset }));
                    included.Add(nested);
                    continue;
                }

                Add(diagnostics, call, "TLGEN24", "Define contains an unsupported Tl.Builder call.");
                valid = false;
            }

            if (included.Count == 1)
            {
                var nestedTimeline = included[0];
                beforeHooks.AddRange(nestedTimeline.BeforeHooks);
                afterHooks.InsertRange(0, nestedTimeline.AfterHooks);
            }

            if (!ValidateOverlaps(tracks.Count, clips, declaration.Syntax))
                valid = false;
            if (tracks.Count > 65_536)
            {
                Add(diagnostics, define, "TLGEN39", "A timeline may contain at most 65,536 track instances.");
                valid = false;
            }
            var kinds = tracks.Select(static track => (track.TypeName, track.ClipTypeName)).Distinct().Count();
            if (kinds > 256)
            {
                Add(diagnostics, define, "TLGEN40", "A timeline may contain at most 256 distinct track and clip type pairs.");
                valid = false;
            }

            var loopModes = included.Select(static timeline => timeline.Loops).Distinct().ToArray();
            if (loopModes.Length > 1 || loopsDeclared && included.Any(static timeline => !timeline.Loops))
            {
                Add(diagnostics, define, "TLGEN43", "Included timelines have incompatible looping modes.");
                valid = false;
            }
            var loops = loopsDeclared || loopModes.Length == 1 && loopModes[0];
            var duration = clips.Count == 0 ? 0u : clips.Max(static clip => clip.End);
            if (loops && included.Any(timeline => timeline.Loops && timeline.Duration != duration))
            {
                Add(diagnostics, define, "TLGEN47", "A looping include must have the same duration as the flattened timeline.");
                valid = false;
            }

            var allSlots = tracks.SelectMany(static track => track.SeekSlots)
                .Concat(beforeHooks.SelectMany(static hook => hook.ForwardSlots.Concat(hook.BackwardSlots)))
                .Concat(afterHooks.SelectMany(static hook => hook.ForwardSlots.Concat(hook.BackwardSlots)))
                .ToArray();
            var protectedPlayback = Display(contracts.Playback);
            var protectedTypedPlayback = Display(contracts.TypedPlayback.Construct(declaration.Symbol));
            foreach (var slot in allSlots.Where(slot => slot.Mode != SlotMode.Input && (slot.TypeName == protectedPlayback || slot.TypeName == protectedTypedPlayback)))
            {
                Add(diagnostics, _slotSites[slot], "TLGEN51", $"Writable component slot '{slot.Name}' cannot use '{slot.TypeName}' because it can alias generated playback state.");
                valid = false;
            }
            var (readOnlySlots, writableSlots) = MergeSlots(allSlots, ref valid);
            if (!valid)
                return null;

            return new HeterogeneousTimeline(
                declaration.Symbol.Name,
                declaration.Symbol.ContainingNamespace.IsGlobalNamespace ? "" : declaration.Symbol.ContainingNamespace.ToDisplayString(),
                loops,
                [],
                tracks,
                clips,
                beforeHooks,
                afterHooks,
                readOnlySlots,
                writableSlots);
        }

        private (INamedTypeSymbol ClipType, IReadOnlyList<TimelineSlot> Seek)?
            ReadTrack(INamedTypeSymbol trackType, INamedTypeSymbol timelineType, SyntaxNode site)
        {
            var contractsForTrack = trackType.AllInterfaces
                .Where(candidate => Same(candidate.OriginalDefinition, contracts.Track))
                .ToArray();
            if (!trackType.IsUnmanagedType || contractsForTrack.Length != 1 || contractsForTrack[0].TypeArguments[0] is not INamedTypeSymbol clipType || !clipType.IsUnmanagedType)
            {
                Add(diagnostics, site, "TLGEN29", $"Track '{Display(trackType)}' must be unmanaged and implement exactly one exact Tl.ITrack<TClip> with an unmanaged clip type.");
                return null;
            }
            var seek = ReadSeekOperation(trackType, clipType, timelineType, site);
            return seek is null ? null : (clipType, seek);
        }

        private IReadOnlyList<TimelineSlot>? ReadSeekOperation(
            INamedTypeSymbol trackType,
            INamedTypeSymbol clipType,
            INamedTypeSymbol timelineType,
            SyntaxNode site)
        {
            var members = trackType.GetMembers("Seek")
                .Where(static member => !member.IsImplicitlyDeclared)
                .ToArray();
            var method = members.Length == 1 ? members[0] as IMethodSymbol : null;
            if (method is null || !ContractMethod(method))
            {
                var diagnosticSite = members.Length == 1 ? DeclarationSite(members[0], site) : TypeSite(trackType, site);
                Add(diagnostics, diagnosticSite, "TLGEN30", $"Track '{Display(trackType)}' must declare exactly one public static non-generic void Seek method.");
                return null;
            }
            if (method.Parameters.Length == 0
                || method.Parameters[0].RefKind != RefKind.In
                || method.Parameters[0].Type is not INamedTypeSymbol frame
                || !Same(frame.OriginalDefinition, contracts.Frame)
                || frame.TypeArguments.Length != 2
                || !Same(frame.TypeArguments[0], trackType)
                || !Same(frame.TypeArguments[1], clipType))
            {
                var diagnosticSite = method.Parameters.Length == 0 ? DeclarationSite(method, site) : ParameterSite(method.Parameters[0], site);
                Add(diagnostics, diagnosticSite, "TLGEN31", $"Track '{Display(trackType)}.Seek' must begin with in Tl.Frame<{Display(trackType)}, {Display(clipType)}>.");
                return null;
            }
            return ReadSlots(method.Parameters.Skip(1), method, timelineType, "TLGEN32", site);
        }

        private TimelineHook? ReadHook(INamedTypeSymbol hookType, INamedTypeSymbol timelineType, SyntaxNode site)
        {
            if (hookType.TypeKind != TypeKind.Struct || !hookType.IsUnmanagedType || !Implements(hookType, contracts.Hook))
            {
                Add(diagnostics, site, "TLGEN44", $"Hook type '{Display(hookType)}' must be an unmanaged struct implementing the exact Tl.IHook contract.");
                return null;
            }
            var forward = ReadHookOperation(hookType, timelineType, "Forward", site);
            var backward = ReadHookOperation(hookType, timelineType, "Backward", site);
            return forward is null || backward is null
                ? null
                : new TimelineHook(Display(hookType), forward, backward);
        }

        private IReadOnlyList<TimelineSlot>? ReadHookOperation(INamedTypeSymbol hookType, INamedTypeSymbol timelineType, string name, SyntaxNode site)
        {
            var methods = hookType.GetMembers(name)
                .OfType<IMethodSymbol>()
                .Where(ContractMethod)
                .ToArray();
            if (methods.Length != 1)
            {
                Add(diagnostics, site, "TLGEN45", $"Hook '{Display(hookType)}' must declare exactly one public static non-generic void {name} method.");
                return null;
            }
            return ReadSlots(methods[0].Parameters, methods[0], timelineType, "TLGEN46", site);
        }

        private IReadOnlyList<TimelineSlot>? ReadSlots(
            IEnumerable<IParameterSymbol> parameters,
            IMethodSymbol method,
            INamedTypeSymbol timelineType,
            string code,
            SyntaxNode site)
        {
            var slots = new List<TimelineSlot>();
            foreach (var parameter in parameters)
            {
                var mode = parameter.RefKind switch
                {
                    RefKind.In => SlotMode.Input,
                    RefKind.Ref => SlotMode.Reference,
                    RefKind.Out => SlotMode.Output,
                    _ => (SlotMode?)null,
                };
                if (mode is null || parameter.IsOptional || parameter.IsParams || !parameter.Type.IsUnmanagedType)
                {
                    Add(diagnostics, ParameterSite(parameter, site), code, $"Every component parameter of '{Display(method.ContainingType)}.{method.Name}' must be a required unmanaged in, ref, or out parameter.");
                    return null;
                }
                if (parameter.Name == "playback")
                {
                    Add(diagnostics, ParameterSite(parameter, site), "TLGEN50", "Component slot 'playback' is reserved for generated playback state.");
                    return null;
                }
                if (mode != SlotMode.Input && IsProtectedPlayback(parameter.Type, timelineType))
                {
                    Add(diagnostics, ParameterSite(parameter, site), "TLGEN51", $"Writable component slot '{parameter.Name}' cannot use '{Display(parameter.Type)}' because it can alias generated playback state.");
                    return null;
                }
                var slot = new TimelineSlot(parameter.Name, Display(parameter.Type), mode.Value);
                _slotSites.Add(slot, ParameterSite(parameter, site));
                slots.Add(slot);
            }
            return slots;
        }

        private bool ReadClip(
            InvocationExpressionSyntax call,
            SemanticModel model,
            IReadOnlyDictionary<ILocalSymbol, (HeterogeneousTrack Track, INamedTypeSymbol ClipType)> locals,
            List<HeterogeneousClip> clips)
        {
            var operation = model.GetOperation(call) as IInvocationOperation;
            var arguments = new ExpressionSyntax?[4];
            if (operation is not null)
                foreach (var argument in operation.Arguments)
                    if (argument.Parameter is { Ordinal: >= 0 and < 4 } parameter
                        && argument.Syntax is ArgumentSyntax syntax)
                        arguments[parameter.Ordinal] = syntax.Expression;
            if (operation?.TargetMethod.Parameters.Length != 4
                || arguments.Any(static argument => argument is null)
                || model.GetSymbolInfo(arguments[0]!).Symbol is not ILocalSymbol local
                || !locals.TryGetValue(local, out var binding))
            {
                Add(diagnostics, call, "TLGEN34", "Clip requires a track local declared earlier in Define.");
                return false;
            }
            var payload = arguments[1]!;
            var payloadType = model.GetTypeInfo(payload).ConvertedType;
            if (!Constant(payload, model)
                || payloadType is null
                || !Same(payloadType, binding.ClipType))
            {
                Add(diagnostics, payload, "TLGEN35", $"Clip payload must be a compile-time construction or constant convertible to '{binding.Track.ClipTypeName}'.");
                return false;
            }
            if (!UInt(arguments[2]!, model, out var start)
                || !UInt(arguments[3]!, model, out var end)
                || start >= end)
            {
                Add(diagnostics, call, "TLGEN36", "Clip bounds must be compile-time uint constants with start less than end.");
                return false;
            }
            clips.Add(new HeterogeneousClip(binding.Track.Index, binding.Track.ClipTypeName, Canonical(payload, model), start, end));
            return true;
        }

        private static ILocalSymbol? TrackLocal(InvocationExpressionSyntax call, SemanticModel model)
        {
            if (model.GetOperation(call) is not IInvocationOperation operation)
                return null;
            var argument = operation.Arguments.FirstOrDefault(static argument => argument.Parameter?.Ordinal == 0);
            return argument?.Syntax is ArgumentSyntax syntax
                ? model.GetSymbolInfo(syntax.Expression).Symbol as ILocalSymbol
                : null;
        }

        private bool ValidateOverlaps(int trackCount, IReadOnlyList<HeterogeneousClip> clips, SyntaxNode site)
        {
            for (var track = 0; track < trackCount; track++)
            {
                var own = clips.Where(clip => clip.TrackIndex == track).ToArray();
                var cuts = own.SelectMany(static clip => new[] { clip.Start, clip.End }).Distinct();
                if (cuts.Any(cut => own.Count(clip => clip.Start <= cut && cut < clip.End) > 2))
                {
                    Add(diagnostics, site, "TLGEN37", $"Track {track} has more than two clips active at once.");
                    return false;
                }
            }
            return true;
        }

        private (TimelineSlot[] ReadOnly, TimelineSlot[] Writable) MergeSlots(
            IEnumerable<TimelineSlot> source,
            ref bool valid)
        {
            var readOnlySlots = new List<TimelineSlot>();
            var writableSlots = new List<TimelineSlot>();
            foreach (var group in source.GroupBy(static slot => slot.Name, StringComparer.Ordinal))
            {
                var variants = group.ToArray();
                var types = variants.Select(static slot => slot.TypeName).Distinct(StringComparer.Ordinal).ToArray();
                if (types.Length != 1)
                {
                    var conflict = variants.First(slot => slot.TypeName != variants[0].TypeName);
                    Add(diagnostics, _slotSites[conflict], "TLGEN38", $"Slot '{group.Key}' has incompatible type declarations.");
                    valid = false;
                    continue;
                }
                var capabilities = variants.Select(static slot => slot.Mode == SlotMode.Input).Distinct().ToArray();
                if (capabilities.Length != 1)
                {
                    var conflict = variants.First(slot => (slot.Mode == SlotMode.Input) != (variants[0].Mode == SlotMode.Input));
                    Add(diagnostics, _slotSites[conflict], "TLGEN38", $"Slot '{group.Key}' cannot be both read-only and writable.");
                    valid = false;
                    continue;
                }
                var slot = new TimelineSlot(group.Key, types[0], capabilities[0] ? SlotMode.Input : SlotMode.Reference);
                (capabilities[0] ? readOnlySlots : writableSlots).Add(slot);
            }
            return (
                readOnlySlots.OrderBy(static slot => slot.Name, StringComparer.Ordinal).ToArray(),
                writableSlots.OrderBy(static slot => slot.Name, StringComparer.Ordinal).ToArray());
        }

        private bool IsProtectedPlayback(ITypeSymbol type, INamedTypeSymbol timelineType)
            => Same(type, contracts.Playback)
                || type is INamedTypeSymbol named
                    && Same(named.OriginalDefinition, contracts.TypedPlayback)
                    && named.TypeArguments.Length == 1
                    && Same(named.TypeArguments[0], timelineType);

        private bool BuilderCall(
            InvocationExpressionSyntax call,
            SemanticModel model,
            IParameterSymbol builderParameter,
            string name)
        {
            if (call.Expression is not MemberAccessExpressionSyntax member
                || member.Name.Identifier.ValueText != name
                || !SameSymbol(model.GetSymbolInfo(member.Expression).Symbol, builderParameter))
                return false;
            var symbol = model.GetSymbolInfo(call);
            return symbol.Symbol is IMethodSymbol method && Same(method.ContainingType, contracts.Builder) && method.Name == name
                || symbol.CandidateSymbols.OfType<IMethodSymbol>().Any(candidate => Same(candidate.ContainingType, contracts.Builder) && candidate.Name == name);
        }

        private bool GenericBuilderCall(
            InvocationExpressionSyntax call,
            SemanticModel model,
            IParameterSymbol builderParameter,
            string name,
            out INamedTypeSymbol type)
        {
            type = null!;
            if (call.ArgumentList.Arguments.Count != 0
                || call.Expression is not MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } member
                || generic.TypeArgumentList.Arguments.Count != 1
                || generic.Identifier.ValueText != name
                || !SameSymbol(model.GetSymbolInfo(member.Expression).Symbol, builderParameter)
                || model.GetTypeInfo(generic.TypeArgumentList.Arguments[0]).Type is not INamedTypeSymbol resolved)
                return false;
            var method = model.GetSymbolInfo(call).Symbol as IMethodSymbol;
            if (method is not null && (!Same(method.ContainingType, contracts.Builder) || method.Name != name))
                return false;
            type = resolved;
            return true;
        }
    }

    private sealed class ExpressionCanonicalizer(SemanticModel model) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var visited = base.VisitObjectCreationExpression(node) as ObjectCreationExpressionSyntax ?? node;
            return model.GetTypeInfo(node).Type is { } type
                ? visited.WithType(SyntaxFactory.ParseTypeName(Display(type)))
                : visited;
        }

        public override SyntaxNode VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            var arguments = Visit(node.ArgumentList) as ArgumentListSyntax ?? node.ArgumentList;
            return model.GetTypeInfo(node).Type is { } type
                ? SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(Display(type)), arguments, null)
                : base.VisitImplicitObjectCreationExpression(node) ?? node;
        }

        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            var expression = Visit(node.Expression) as ExpressionSyntax ?? node.Expression;
            return model.GetTypeInfo(node.Type).Type is { } type
                ? node.WithType(SyntaxFactory.ParseTypeName(Display(type))).WithExpression(expression)
                : base.VisitCastExpression(node) ?? node;
        }

        public override SyntaxNode VisitDefaultExpression(DefaultExpressionSyntax node)
            => model.GetTypeInfo(node.Type).Type is { } type
                ? node.WithType(SyntaxFactory.ParseTypeName(Display(type)))
                : base.VisitDefaultExpression(node) ?? node;

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (model.GetSymbolInfo(node).Symbol is IFieldSymbol { IsStatic: true } field
                && (field.HasConstantValue || field.ContainingType.TypeKind == TypeKind.Enum))
                return SyntaxFactory.ParseExpression($"{Display(field.ContainingType)}.{Escape(field.Name)}");
            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (model.GetSymbolInfo(node).Symbol is IFieldSymbol { IsStatic: true } field
                && (field.HasConstantValue || field.ContainingType.TypeKind == TypeKind.Enum))
                return SyntaxFactory.ParseExpression($"{Display(field.ContainingType)}.{Escape(field.Name)}");
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
                && model.GetConstantValue(node) is { HasValue: true, Value: string value })
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
            return base.VisitInvocationExpression(node);
        }
    }

    private static IReadOnlyList<(INamedTypeSymbol Symbol, TypeDeclarationSyntax Syntax)> SourceTypes(
        CSharpCompilation compilation,
        IEnumerable<SyntaxTree> trees)
    {
        var result = new List<(INamedTypeSymbol Symbol, TypeDeclarationSyntax Syntax)>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var syntax in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                if (model.GetDeclaredSymbol(syntax) is { } symbol && seen.Add(symbol))
                    result.Add((symbol, syntax));
        }
        return result;
    }

    private static Contracts? ReadContracts(Compilation compilation)
    {
        var timeline = compilation.GetTypeByMetadataName("Tl.ITimeline");
        var track = compilation.GetTypeByMetadataName("Tl.ITrack`1");
        var hook = compilation.GetTypeByMetadataName("Tl.IHook");
        var frame = compilation.GetTypeByMetadataName("Tl.Frame`2");
        var builder = compilation.GetTypeByMetadataName("Tl.Builder");
        var playback = compilation.GetTypeByMetadataName("Tl.Playback");
        var typedPlayback = compilation.GetTypeByMetadataName("Tl.Playback`1");
        return timeline is null || track is null || hook is null || frame is null || builder is null || playback is null || typedPlayback is null
            ? null
            : new Contracts(timeline, track, hook, frame, builder, playback, typedPlayback);
    }

#if NET10_0
    private static IReadOnlyList<MetadataReference> References(
        IEnumerable<string> paths,
        ICollection<DeclarationDiagnostic> diagnostics)
    {
        var references = new List<MetadataReference>();
        foreach (var path in paths.Where(static path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.Ordinal))
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(Path.GetFullPath(path)));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException or ArgumentException)
            {
                diagnostics.Add(new DeclarationDiagnostic(path, 1, 1, "TLGEN18", $"Metadata reference could not be loaded: {exception.Message}"));
            }
        }
        return references;
    }

    private static string[] DefaultReferences()
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
            foreach (var path in trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                paths.Add(path);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (!assembly.IsDynamic && assembly.Location.Length != 0)
                paths.Add(assembly.Location);
        var core = Path.Combine(AppContext.BaseDirectory, "Tl.Core.dll");
        if (File.Exists(core))
            paths.Add(core);
        return paths.OrderBy(static path => path, StringComparer.Ordinal).ToArray();
    }

    private static LanguageVersion ParseLanguageVersion(string value)
        => LanguageVersionFacts.TryParse(value, out var version) ? version : LanguageVersion.Preview;

    private static NullableContextOptions ParseNullable(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "enable" => NullableContextOptions.Enable,
            "annotations" => NullableContextOptions.Annotations,
            "warnings" => NullableContextOptions.Warnings,
            _ => NullableContextOptions.Disable,
        };
#endif

    private static bool ValidDefine(IMethodSymbol method)
        => ContractMethod(method)
            && method.Parameters.Length == 1
            && method.Parameters[0] is { } parameter
            && parameter.RefKind == RefKind.None
            && parameter.ScopedKind == ScopedKind.ScopedValue;

    private static bool ContractMethod(IMethodSymbol method)
        => method.DeclaredAccessibility == Accessibility.Public
            && method.IsStatic
            && method.Arity == 0
            && method.MethodKind == MethodKind.Ordinary
            && method.ReturnsVoid;

    private static MethodDeclarationSyntax? MethodSyntax(IMethodSymbol method)
        => method.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .SingleOrDefault();

    private static SyntaxNode DeclarationSite(ISymbol symbol, SyntaxNode fallback)
        => symbol.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OrderBy(static syntax => syntax.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(static syntax => syntax.SpanStart)
            .FirstOrDefault() ?? fallback;

    private static SyntaxNode TypeSite(INamedTypeSymbol type, SyntaxNode fallback)
        => DeclarationSite(type, fallback);

    private static SyntaxNode ParameterSite(IParameterSymbol parameter, SyntaxNode fallback)
        => DeclarationSite(parameter, fallback);

    private static bool Constant(ExpressionSyntax expression, SemanticModel model)
    {
        if (model.GetConstantValue(expression).HasValue)
            return true;
        return expression switch
        {
            ObjectCreationExpressionSyntax creation => creation.Initializer is null
                && creation.ArgumentList?.Arguments.All(argument => Constant(argument.Expression, model)) != false,
            ImplicitObjectCreationExpressionSyntax creation => creation.Initializer is null
                && creation.ArgumentList.Arguments.All(argument => Constant(argument.Expression, model)),
            ParenthesizedExpressionSyntax parenthesized => Constant(parenthesized.Expression, model),
            CastExpressionSyntax cast => Constant(cast.Expression, model),
            PrefixUnaryExpressionSyntax unary => Constant(unary.Operand, model),
            CheckedExpressionSyntax checkedExpression => Constant(checkedExpression.Expression, model),
            _ => false,
        };
    }

    private static bool UInt(ExpressionSyntax expression, SemanticModel model, out uint value)
    {
        var constant = model.GetConstantValue(expression);
        switch (constant.Value)
        {
            case byte item: value = item; return true;
            case ushort item: value = item; return true;
            case int item when item >= 0: value = (uint)item; return true;
            case uint item: value = item; return true;
            case long item when item is >= 0 and <= uint.MaxValue: value = (uint)item; return true;
            case ulong item when item <= uint.MaxValue: value = (uint)item; return true;
            default: value = 0; return false;
        }
    }

    private static string Canonical(ExpressionSyntax expression, SemanticModel model)
        => new ExpressionCanonicalizer(model).Visit(expression).WithoutTrivia().ToFullString();

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol contract)
        => type.AllInterfaces.Any(candidate => Same(candidate.OriginalDefinition, contract));

    private static bool Same(ISymbol? left, ISymbol? right)
        => SymbolEqualityComparer.Default.Equals(left, right);

    private static bool SameSymbol(ISymbol? left, ISymbol? right)
        => SymbolEqualityComparer.Default.Equals(left, right);

    private static string Display(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string Escape(string value)
        => SyntaxFacts.GetKeywordKind(value) == SyntaxKind.None ? value : "@" + value;

    private static void Add(
        ICollection<DeclarationDiagnostic> diagnostics,
        SyntaxNode node,
        string code,
        string message)
    {
        var span = node.GetLocation().GetLineSpan();
        diagnostics.Add(new DeclarationDiagnostic(
            span.Path,
            span.StartLinePosition.Line + 1,
            span.StartLinePosition.Character + 1,
            code,
            message)
        {
            SpanStart = node.SpanStart,
            SpanLength = node.Span.Length,
            EndLine = span.EndLinePosition.Line + 1,
            EndColumn = span.EndLinePosition.Character + 1,
        });
    }

    private static bool Has(SyntaxTokenList modifiers, SyntaxKind kind)
        => modifiers.Any(token => token.IsKind(kind));

    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly ReferenceComparer<T> Instance = new();
        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
