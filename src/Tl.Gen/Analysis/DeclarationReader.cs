using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp;
using Tl.Gen.Model;

namespace Tl.Gen.Analysis;

public sealed record DeclarationDiagnostic(string File, int Line, int Column, string Code, string Message)
{
    public override string ToString() => $"{File}({Line},{Column}): error {Code}: {Message}";
}

public enum CompiledDeclarationKind : byte
{
    LegacyCompile,
    PartialTimeline,
}

public sealed record CompiledDeclaration
{
    public required string KernelName { get; init; }
    public required TimelineDefinition Definition { get; init; }
    public required string File { get; init; }
    public required int Line { get; init; }
    public CompiledDeclarationKind Kind { get; init; }
    public int ClipCount => Definition.Clips.Count;
    public int TrackCount => Definition.Tracks.Count;
}

/// <summary>
/// Reads human-authored timeline declarations out of the compiling project's
/// C# sources. A site is Compile-eligible when the Build argument is a static
/// lambda (or a static method group) whose Track/Clip/Looping calls use only
/// compile-time-constant arguments: literal payload expressions and literal
/// tick windows. Anything else is rejected with a diagnostic naming the
/// constraint and pointing at the in-memory interpreter path (drop
/// <c>.Compile()</c>), which has no such constraint.
/// </summary>
public static class DeclarationReader
{
    public static (IReadOnlyList<CompiledDeclaration> Declarations, IReadOnlyList<DeclarationDiagnostic> Diagnostics)
        Read(IReadOnlyList<(string Path, string Source)> sources, IReadOnlyList<string>? preprocessorSymbols = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols ?? []);
        var trees = sources.Select(s => CSharpSyntaxTree.ParseText(s.Source, parseOptions, s.Path)).ToList();
        var diagnostics = new List<DeclarationDiagnostic>();
        var declarations = new List<CompiledDeclaration>();
        var usedKernelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Method groups resolve across every scanned source; collect the
        // candidates once up front.
        var authorMethods = new List<MethodDeclarationSyntax>();
        foreach (var tree in trees)
            authorMethods.AddRange(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>());

        foreach (var tree in trees)
        {
            var path = tree.FilePath;
            var root = tree.GetCompilationUnitRoot();

            foreach (var declaration in root.DescendantNodes().OfType<StructDeclarationSyntax>())
            {
                var read = ReadPartialTimeline(declaration, path, trees, authorMethods, diagnostics);
                if (read != null)
                    Add(read, declaration, path, declarations, diagnostics, usedKernelNames);
            }

            foreach (var compile in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var read = ReadSite(compile, path, trees, authorMethods, diagnostics);
                if (read != null)
                    Add(read, compile, path, declarations, diagnostics, usedKernelNames);
            }
        }

        return (declarations, diagnostics);
    }

    private static void Add(
        CompiledDeclaration declaration,
        SyntaxNode site,
        string path,
        List<CompiledDeclaration> declarations,
        List<DeclarationDiagnostic> diagnostics,
        HashSet<string> names)
    {
        if (string.Equals($"{declaration.KernelName}.g.cs", KernelEmitter.SharedFileName, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(At(site, path, "TLGEN07", $"the generated type name '{declaration.KernelName}' is reserved."));
            return;
        }

        if (!names.Add(declaration.KernelName))
        {
            diagnostics.Add(At(site, path, "TLGEN07", $"two compiled timelines resolve to '{declaration.KernelName}'."));
            return;
        }

        declarations.Add(declaration);
    }

    private static CompiledDeclaration? ReadPartialTimeline(
        StructDeclarationSyntax declaration,
        string path,
        IReadOnlyList<SyntaxTree> trees,
        IReadOnlyList<MethodDeclarationSyntax> methods,
        List<DeclarationDiagnostic> diagnostics)
    {
        if (!TryReadTimelineDeclarationTypes(declaration, trees, out var trackType, out var clipType))
            return null;

        if (declaration.Parent is not CompilationUnitSyntax and not BaseNamespaceDeclarationSyntax
            || declaration.TypeParameterList != null
            || !declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.PublicKeyword))
            || !declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.ReadOnlyKeyword))
            || !declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(At(declaration, path, "TLGEN11",
                "a compiled timeline must be a top-level public readonly partial struct without type parameters."));
            return null;
        }

        var identity = TypeIdentity(declaration);
        var candidates = methods
            .Where(method => method.Identifier.ValueText == "Define" && TypeIdentity(method) == identity)
            .ToList();
        if (candidates.Count != 1)
        {
            diagnostics.Add(At(declaration, path, "TLGEN12",
                "a compiled timeline must declare exactly one public static void Define(scoped TimelineBuilder<TTrack,TClip> timeline) method."));
            return null;
        }

        var define = candidates[0];
        var parameter = define.ParameterList.Parameters.Count == 1
            ? define.ParameterList.Parameters[0]
            : null;
        var builder = TimelineBuilderType(parameter?.Type);
        if (define.Body == null
            || define.ReturnType is not PredefinedTypeSyntax { Keyword.ValueText: "void" }
            || !define.Modifiers.Any(static token => token.IsKind(SyntaxKind.PublicKeyword))
            || !define.Modifiers.Any(static token => token.IsKind(SyntaxKind.StaticKeyword))
            || define.TypeParameterList != null
            || parameter == null
            || !parameter.Modifiers.Any(static token => token.IsKind(SyntaxKind.ScopedKeyword))
            || builder?.TypeArgumentList.Arguments.Count != 2
            || NormalizeName(builder.TypeArgumentList.Arguments[0].ToString()) != NormalizeName(trackType)
            || NormalizeName(builder.TypeArgumentList.Arguments[1].ToString()) != NormalizeName(clipType))
        {
            diagnostics.Add(At(define, define.SyntaxTree.FilePath, "TLGEN12",
                $"Define must be 'public static void Define(scoped TimelineBuilder<{trackType}, {clipType}> timeline)' with a block body."));
            return null;
        }

        var sourceUsings = SourceUsings(declaration, define, diagnostics);
        if (sourceUsings == null)
            return null;

        var name = declaration.Identifier.Text;
        var definition = InterpretBody(
            define.Body,
            parameter.Identifier.ValueText,
            name,
            trackType,
            clipType,
            NamespaceIdentity(declaration),
            define,
            define.SyntaxTree.FilePath,
            sourceUsings,
            diagnostics);
        if (definition == null)
            return null;

        return new CompiledDeclaration
        {
            KernelName = name,
            Definition = definition,
            File = path,
            Line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            Kind = CompiledDeclarationKind.PartialTimeline,
        };
    }

    private static CompiledDeclaration? ReadSite(
        InvocationExpressionSyntax compile,
        string path,
        List<SyntaxTree> trees,
        List<MethodDeclarationSyntax> authorMethods,
        List<DeclarationDiagnostic> diagnostics)
    {
        // .Compile() with no arguments, chained on a Build invocation.
        if (compile.ArgumentList.Arguments.Count != 0
            || compile.Expression is not MemberAccessExpressionSyntax compileMember
            || compileMember.Name is not IdentifierNameSyntax { Identifier.ValueText: "Compile" })
            return null;

        if (compileMember.Expression is not InvocationExpressionSyntax build)
            return null;

        // Timeline<A,B>.Build: the member's receiver must be the closed
        // generic timeline type (bare, Tl.-qualified, or global::-qualified).
        // Note the method name itself is a plain identifier — the type
        // arguments sit on Timeline.
        if (build.Expression is not MemberAccessExpressionSyntax buildMember
            || buildMember.Name is not IdentifierNameSyntax { Identifier.ValueText: "Build" }
            || !TryReadTimelineTypes(buildMember.Expression, trees, out var trackType, out var clipType))
            return null;

        if (build.ArgumentList.Arguments.Count != 1)
        {
            diagnostics.Add(At(build, path, "TLGEN09",
                "only the single-author Build(author) overload is Compile-eligible; the source-carrying and options overloads execute against runtime data."));
            return null;
        }

        var authorArg = build.ArgumentList.Arguments[0].Expression;

        BlockSyntax body;
        string builderName;
        if (authorArg is SimpleLambdaExpressionSyntax lambda)
        {
            if (!lambda.Modifiers.Any(static m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                diagnostics.Add(At(lambda, path, "TLGEN02",
                    "the authoring lambda must be static (no captures): compile-time specialization reads the declaration, it does not close over runtime state. The in-memory interpreter path (Timeline<TTrack,TClip>.Build(...).InMemory()) accepts any lambda."));
                return null;
            }

            builderName = lambda.Parameter.Identifier.ValueText;
            if (lambda.Body is not BlockSyntax block)
            {
                diagnostics.Add(At(lambda, path, "TLGEN03",
                    "the authoring lambda must have a block body of builder calls."));
                return null;
            }

            body = block;
        }
        else if (TryResolveMethodGroup(authorArg, trackType, clipType, trees, authorMethods, out var method, out builderName))
        {
            // The shared-authoring form: the same static method feeds the
            // in-memory interpreter leg (Timeline<T,C>.Build(Author)) and the
            // compiled kernel — one authored instance, two players.
            if (method.Body == null)
            {
                diagnostics.Add(At(authorArg, path, "TLGEN06",
                    $"the authoring method '{method.Identifier.ValueText}' must have a block body."));
                return null;
            }

            body = method.Body;
        }
        else
        {
            diagnostics.Add(At(authorArg, path, "TLGEN06",
                "the Build argument must be a static lambda or a static method group 'void Author(scoped TimelineBuilder<TTrack,TClip>)' declared in the scanned sources."));
            return null;
        }

        var sourceUsings = SourceUsings(compile, body, diagnostics);
        if (sourceUsings == null)
            return null;

        var name = KernelNameFor(compile);
        var declarationNamespace = EnclosingNamespace(compile);
        var definition = InterpretBody(
            body,
            builderName,
            name,
            trackType,
            clipType,
            declarationNamespace,
            compile,
            body.SyntaxTree.FilePath,
            sourceUsings,
            diagnostics);
        if (definition == null)
            return null;

        var line = compile.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return new CompiledDeclaration
        {
            KernelName = name,
            Definition = definition,
            File = path,
            Line = line,
            Kind = CompiledDeclarationKind.LegacyCompile,
        };
    }

    private static bool TryReadTimelineDeclarationTypes(
        StructDeclarationSyntax declaration,
        IReadOnlyList<SyntaxTree> trees,
        out string trackType,
        out string clipType)
    {
        trackType = clipType = "";
        var marker = declaration.BaseList?.Types
            .Select(static item => item.Type)
            .Select(type => type switch
            {
                GenericNameSyntax { Identifier.ValueText: "ITimeline" } generic
                    when IsTlTypeInScope(type, trees, "ITimeline", 2) => generic,
                QualifiedNameSyntax
                {
                    Left: IdentifierNameSyntax { Identifier.ValueText: "Tl" },
                    Right: GenericNameSyntax { Identifier.ValueText: "ITimeline" } generic,
                } => generic,
                QualifiedNameSyntax
                {
                    Left: AliasQualifiedNameSyntax
                    {
                        Alias.Identifier.ValueText: "global",
                        Name.Identifier.ValueText: "Tl",
                    },
                    Right: GenericNameSyntax { Identifier.ValueText: "ITimeline" } generic,
                } => generic,
                _ => null,
            })
            .FirstOrDefault(static type => type != null);

        if (marker?.TypeArgumentList.Arguments.Count != 2)
            return false;

        trackType = marker.TypeArgumentList.Arguments[0].ToString();
        clipType = marker.TypeArgumentList.Arguments[1].ToString();
        return true;
    }

    private static bool TryReadTimelineTypes(
        ExpressionSyntax receiver,
        IReadOnlyList<SyntaxTree> trees,
        out string trackType,
        out string clipType)
    {
        trackType = clipType = "";
        var generic = receiver switch
        {
            GenericNameSyntax { Identifier.ValueText: "Timeline" } g when IsTlTimelineInScope(receiver, trees) => g,
            MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "Tl" },
                Name: GenericNameSyntax { Identifier.ValueText: "Timeline" } g,
            } => g,
            MemberAccessExpressionSyntax
            {
                Expression: AliasQualifiedNameSyntax
                {
                    Alias.Identifier.ValueText: "global",
                    Name.Identifier.ValueText: "Tl",
                },
                Name: GenericNameSyntax { Identifier.ValueText: "Timeline" } g,
            } => g,
            _ => null,
        };

        if (generic?.TypeArgumentList.Arguments.Count != 2)
            return false;

        trackType = generic.TypeArgumentList.Arguments[0].ToString();
        clipType = generic.TypeArgumentList.Arguments[1].ToString();
        return true;
    }

    private static bool IsTlTimelineInScope(ExpressionSyntax receiver, IReadOnlyList<SyntaxTree> trees)
        => IsTlTypeInScope(receiver, trees, "Timeline", 2);

    private static bool IsTlTypeInScope(SyntaxNode receiver, IReadOnlyList<SyntaxTree> trees, string name, int arity)
    {
        if (DeclaresTypeInScope(receiver, trees, name, arity))
            return false;

        var root = receiver.SyntaxTree.GetCompilationUnitRoot();
        var visibleUsings = root.Usings
            .Concat(receiver.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().SelectMany(static scope => scope.Usings))
            .ToList();
        if (visibleUsings.Any(directive => IsTypeAlias(directive, name)))
            return false;
        var containingNamespace = NamespaceIdentity(receiver);
        if (containingNamespace == "Tl" || containingNamespace.StartsWith("Tl.", StringComparison.Ordinal))
            return true;
        if (visibleUsings.Any(IsTlImport))
            return true;

        var globalUsings = trees
            .Select(static tree => tree.GetCompilationUnitRoot())
            .SelectMany(static unit => unit.Usings)
            .Where(static directive => directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
            .ToList();
        if (globalUsings.Any(directive => IsTypeAlias(directive, name)))
            return false;

        return globalUsings.Any(IsTlImport);
    }

    private static bool IsTlImport(UsingDirectiveSyntax directive) =>
        directive.Alias == null
        && !directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
        && directive.Name is IdentifierNameSyntax { Identifier.ValueText: "Tl" }
            or AliasQualifiedNameSyntax
            {
                Alias.Identifier.ValueText: "global",
                Name.Identifier.ValueText: "Tl",
            };

    private static bool IsTypeAlias(UsingDirectiveSyntax directive, string name) =>
        directive.Alias?.Name.Identifier.ValueText == name;

    private static bool DeclaresTypeInScope(SyntaxNode receiver, IReadOnlyList<SyntaxTree> trees, string name, int arity)
    {
        if (receiver.Ancestors().OfType<TypeDeclarationSyntax>().Any(container =>
                container.Members.OfType<TypeDeclarationSyntax>().Any(type => IsNamedType(type, name, arity))))
            return true;

        var targetNamespace = NamespaceIdentity(receiver);
        return trees
            .SelectMany(static tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            .Any(type => type.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax
                && NamespaceIdentity(type) == targetNamespace
                && IsNamedType(type, name, arity));
    }

    private static bool IsNamedType(TypeDeclarationSyntax type, string name, int arity) =>
        type.Identifier.ValueText == name && type.TypeParameterList?.Parameters.Count == arity;

    private static string NamespaceIdentity(SyntaxNode node) => string.Join(
        ".",
        node.AncestorsAndSelf()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(static declaration => declaration.Name.ToString()));

    private static bool TryResolveMethodGroup(
        ExpressionSyntax expression,
        string trackType,
        string clipType,
        IReadOnlyList<SyntaxTree> trees,
        List<MethodDeclarationSyntax> methods,
        out MethodDeclarationSyntax method,
        out string builderName)
    {
        method = null!;
        builderName = "";

        var (identifier, qualifier, global) = expression switch
        {
            IdentifierNameSyntax id => (id.Identifier.ValueText, (string?)null, false),
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } member =>
                (name.Identifier.ValueText, NormalizeName(member.Expression.ToString()), member.Expression.ToString().StartsWith("global::", StringComparison.Ordinal)),
            _ => ((string?)null, null, false),
        };

        if (identifier == null)
            return false;

        var namedMethods = methods.Where(candidate => candidate.Identifier.ValueText == identifier).ToList();
        string? targetType;
        if (qualifier == null)
        {
            var enclosingType = expression.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            targetType = enclosingType == null ? null : TypeIdentity(enclosingType);
        }
        else
        {
            var allowedTypes = AllowedTypeIdentities(expression, qualifier, global, trees);
            var matchedTypes = namedMethods
                .Select(TypeIdentity)
                .Where(allowedTypes.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            targetType = matchedTypes.Count == 1 ? matchedTypes[0] : null;
        }

        if (targetType == null)
            return false;

        var matches = new List<MethodDeclarationSyntax>();
        foreach (var candidate in namedMethods)
        {
            if (TypeIdentity(candidate) != targetType
                || !candidate.Modifiers.Any(static m => m.IsKind(SyntaxKind.StaticKeyword))
                || candidate.ParameterList.Parameters.Count != 1
                || candidate.ReturnType is not PredefinedTypeSyntax { Keyword.ValueText: "void" })
                continue;

            var parameter = candidate.ParameterList.Parameters[0];
            var builderType = TimelineBuilderType(parameter.Type);
            if (builderType == null
                || builderType.TypeArgumentList.Arguments.Count != 2
                || NormalizeName(builderType.TypeArgumentList.Arguments[0].ToString()) != NormalizeName(trackType)
                || NormalizeName(builderType.TypeArgumentList.Arguments[1].ToString()) != NormalizeName(clipType))
                continue;

            matches.Add(candidate);
        }

        if (matches.Count != 1)
            return false;

        method = matches[0];
        builderName = matches[0].ParameterList.Parameters[0].Identifier.ValueText;
        return true;
    }

    private static GenericNameSyntax? TimelineBuilderType(TypeSyntax? type) => type switch
    {
        GenericNameSyntax { Identifier.ValueText: "TimelineBuilder" } generic => generic,
        QualifiedNameSyntax
        {
            Left: IdentifierNameSyntax { Identifier.ValueText: "Tl" },
            Right: GenericNameSyntax { Identifier.ValueText: "TimelineBuilder" } generic,
        } => generic,
        QualifiedNameSyntax
        {
            Left: AliasQualifiedNameSyntax
            {
                Alias.Identifier.ValueText: "global",
                Name.Identifier.ValueText: "Tl",
            },
            Right: GenericNameSyntax { Identifier.ValueText: "TimelineBuilder" } generic,
        } => generic,
        _ => null,
    };

    private static HashSet<string> AllowedTypeIdentities(
        ExpressionSyntax expression,
        string qualifier,
        bool global,
        IReadOnlyList<SyntaxTree> trees)
    {
        var identities = new HashSet<string>(StringComparer.Ordinal) { qualifier };
        if (global)
            return identities;

        var currentNamespace = NamespaceIdentity(expression);
        while (currentNamespace.Length > 0)
        {
            identities.Add(currentNamespace + "." + qualifier);
            var separator = currentNamespace.LastIndexOf('.');
            currentNamespace = separator < 0 ? "" : currentNamespace[..separator];
        }

        var visibleUsings = expression.SyntaxTree.GetCompilationUnitRoot().Usings
            .Concat(expression.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().SelectMany(static scope => scope.Usings))
            .Concat(trees
                .Select(static tree => tree.GetCompilationUnitRoot())
                .SelectMany(static unit => unit.Usings)
                .Where(static directive => directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)));
        foreach (var directive in visibleUsings)
        {
            if (directive.Alias == null
                && !directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                && directive.Name != null)
                identities.Add(NormalizeName(directive.Name.ToString()) + "." + qualifier);
        }

        return identities;
    }

    private static string TypeIdentity(SyntaxNode node)
    {
        var type = string.Join(
            ".",
            node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Reverse().Select(static declaration => declaration.Identifier.ValueText));
        return string.Join('.', new[] { NamespaceIdentity(node), type }.Where(static part => part.Length > 0));
    }

    private static string NormalizeName(string name) => name.Replace("global::", "", StringComparison.Ordinal);

    private static TimelineDefinition? InterpretBody(
        BlockSyntax body,
        string builderName,
        string kernelName,
        string trackType,
        string clipType,
        string declarationNamespace,
        SyntaxNode site,
        string path,
        IReadOnlyList<string> sourceUsings,
        List<DeclarationDiagnostic> diagnostics)
    {
        var tracks = new List<TrackDefinition>();
        var clips = new List<ClipDefinition>();
        var trackVars = new Dictionary<string, int>(StringComparer.Ordinal);
        var loops = false;

        foreach (var statement in body.Statements)
        {
            if (statement is LocalDeclarationStatementSyntax declaration)
            {
                if (declaration.Declaration.Variables.Count != 1
                    || declaration.Declaration.Variables[0].Initializer?.Value is not InvocationExpressionSyntax trackCall
                    || !IsBuilderCall(trackCall, builderName, "Track")
                    || trackCall.ArgumentList.Arguments.Count != 1)
                {
                    diagnostics.Add(At(statement, path, "TLGEN03",
                        $"unsupported statement in the authoring body: only 'var id = {builderName}.Track(payload)', {builderName}.Clip(...), {builderName}.Looping() and {builderName}.DedupStorage(bool) are Compile-eligible."));
                    return null;
                }

                var trackPayload = trackCall.ArgumentList.Arguments[0].Expression;
                if (!IsConstantPayload(trackPayload))
                {
                    diagnostics.Add(At(trackPayload, path, "TLGEN04",
                        $"track payloads must be compile-time constants (literals, default, or 'new T(literal-args...)'); '{trackPayload}' is not. The in-memory interpreter path (Timeline<TTrack,TClip>.Build(...).InMemory()) accepts any track value."));
                    return null;
                }

                var variable = declaration.Declaration.Variables[0].Identifier.ValueText;
                trackVars[variable] = tracks.Count;
                tracks.Add(new TrackDefinition { Index = (ushort)tracks.Count, TrackExpression = trackPayload.ToString() });
                continue;
            }

            if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax call }
                || !TryGetBuilderName(call, builderName, out var member))
            {
                diagnostics.Add(At(statement, path, "TLGEN03",
                    $"unsupported statement in the authoring body: only 'var id = {builderName}.Track(payload)', {builderName}.Clip(...), {builderName}.Looping() and {builderName}.DedupStorage(bool) are Compile-eligible."));
                return null;
            }

            var callee = member.Identifier.ValueText;
            if (callee == "Track")
            {
                // Bare Track statement: a track with no clips (allowed by the
                // runtime builder; the declaration keeps its authored slot).
                if (call.ArgumentList.Arguments.Count != 1 || !IsConstantPayload(call.ArgumentList.Arguments[0].Expression))
                {
                    diagnostics.Add(At(call, path, "TLGEN04",
                        "track payloads must be compile-time constants (literals, default, or 'new T(literal-args...)')."));
                    return null;
                }

                tracks.Add(new TrackDefinition { Index = (ushort)tracks.Count, TrackExpression = call.ArgumentList.Arguments[0].Expression.ToString() });
                continue;
            }

            if (callee == "Looping")
            {
                if (call.ArgumentList.Arguments.Count != 0)
                {
                    diagnostics.Add(At(call, path, "TLGEN03", "Looping() takes no arguments."));
                    return null;
                }

                loops = true;
                continue;
            }

            if (callee == "DedupStorage")
            {
                // Storage dedup is a runtime-table concern; the compiled
                // kernel bakes per-region work slots and never dedups, and
                // dedup is behaviorally invisible, so the flag is accepted
                // and ignored by emission.
                if (call.ArgumentList.Arguments.Count > 1
                    || call.ArgumentList.Arguments.Count == 1
                    && !call.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.TrueLiteralExpression)
                    && !call.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.FalseLiteralExpression))
                {
                    diagnostics.Add(At(call, path, "TLGEN03", "DedupStorage(bool) is the only accepted form."));
                    return null;
                }

                continue;
            }

            if (callee != "Clip")
            {
                diagnostics.Add(At(call, path, "TLGEN03",
                    $"unsupported builder call '{member}' in the authoring body."));
                return null;
            }

            var read = ReadClipCall(call, trackVars, path, diagnostics);
            if (read == null)
                return null;

            clips.Add(read);
        }

        var definition = new TimelineDefinition
        {
            Name = kernelName,
            Namespace = declarationNamespace,
            TrackTypeName = trackType,
            ClipTypeName = clipType,
            Loops = loops,
            Tracks = tracks,
            Clips = clips,
            SourceUsings = sourceUsings,
        };

        try
        {
            TimelineValidator.Validate(definition);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or NotSupportedException or InvalidOperationException)
        {
            diagnostics.Add(At(site, path, "TLGEN08", $"the declaration violates a timeline constraint: {exception.Message}"));
            return null;
        }

        return definition;
    }

    private static IReadOnlyList<string>? SourceUsings(
        SyntaxNode first,
        SyntaxNode second,
        List<DeclarationDiagnostic> diagnostics)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in new[] { first, second })
        {
            var directives = node.SyntaxTree.GetCompilationUnitRoot().Usings
                .Concat(node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().SelectMany(static scope => scope.Usings));
            foreach (var directive in directives)
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
                    continue;

                var value = directive.WithoutTrivia().NormalizeWhitespace().ToFullString();
                var alias = directive.Alias?.Name.Identifier.ValueText;
                if (alias != null
                    && aliases.TryGetValue(alias, out var existing)
                    && existing != value)
                {
                    diagnostics.Add(At(
                        directive,
                        directive.SyntaxTree.FilePath,
                        "TLGEN13",
                        $"using alias '{alias}' has conflicting declarations across the compiled timeline parts; use distinct aliases or fully qualify the referenced type."));
                    return null;
                }

                if (alias != null)
                    aliases.TryAdd(alias, value);
                values.Add(value);
            }
        }

        return values.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
    }

    private static ClipDefinition? ReadClipCall(
        InvocationExpressionSyntax call,
        Dictionary<string, int> trackVars,
        string path,
        List<DeclarationDiagnostic> diagnostics)
    {
        var arguments = call.ArgumentList.Arguments;
        if (arguments.Count is < 4 or > 4)
        {
            diagnostics.Add(At(call, path, "TLGEN03",
                "Clip takes (track, payload, start, end) with literal windows; the start:/end: named forms are accepted."));
            return null;
        }

        // (in track, payload, [start:,] [end:] ...)
        var trackArgument = arguments[0];
        var trackName = (trackArgument.Expression as IdentifierNameSyntax)?.Identifier.ValueText;
        if (trackName == null || !trackVars.TryGetValue(trackName, out var trackIndex))
        {
            diagnostics.Add(At(trackArgument, path, "TLGEN05",
                $"Clip's first argument must name a local bound by 'var id = builder.Track(...)' in the same body; '{trackArgument.Expression}' does not."));
            return null;
        }

        var payload = arguments[1].Expression;
        if (!IsConstantPayload(payload))
        {
            diagnostics.Add(At(payload, path, "TLGEN04",
                $"clip payloads must be compile-time constants (literals, default, or 'new T(literal-args...)'); '{payload}' is not. The in-memory interpreter path (Timeline<TTrack,TClip>.Build(...).InMemory()) accepts any clip value."));
            return null;
        }

        uint? start = null, end = null;
        foreach (var argument in arguments.Skip(2))
        {
            var name = argument.NameColon?.Name.Identifier.ValueText;
            if (name == "start" || name == "end")
            {
                var value = ReadUintLiteral(argument.Expression, path, diagnostics);
                if (value == null)
                    return null;
                if (name == "start") start = value; else end = value;
            }
            else
            {
                var value = ReadUintLiteral(argument.Expression, path, diagnostics);
                if (value == null)
                    return null;
                if (start == null) start = value;
                else if (end == null) end = value;
                else
                {
                    diagnostics.Add(At(argument, path, "TLGEN03", "too many positional window arguments."));
                    return null;
                }
            }
        }

        if (start == null || end == null)
        {
            diagnostics.Add(At(call, path, "TLGEN03", "Clip needs both a start and an end window literal."));
            return null;
        }

        return new ClipDefinition
        {
            TrackIndex = (ushort)trackIndex,
            Start = start.Value,
            End = end.Value,
            PayloadExpression = payload.ToString(),
        };
    }

    private static uint? ReadUintLiteral(
        ExpressionSyntax expression,
        string path,
        List<DeclarationDiagnostic> diagnostics)
    {
        if (expression is not LiteralExpressionSyntax literal || !literal.Token.IsKind(SyntaxKind.NumericLiteralToken))
        {
            diagnostics.Add(At(expression, path, "TLGEN10",
                $"tick windows must be uint literals; '{expression}' is not. The in-memory interpreter path (Timeline<TTrack,TClip>.Build(...).InMemory()) accepts any window values."));
            return null;
        }

        var value = literal.Token.Value switch
        {
            uint number => number,
            int number when number >= 0 => (uint)number,
            _ => (uint?)null,
        };
        if (value == null)
        {
            diagnostics.Add(At(expression, path, "TLGEN10", $"'{literal.Token.Text}' is not a uint literal."));
            return null;
        }

        return value;
    }

    /// <summary>
    /// The constant grammar a payload expression must satisfy: literals,
    /// unary +/- over literals, default, or new T(constant-args...) with no
    /// initializer. Emitted verbatim into the kernel's frozen payload tables.
    /// </summary>
    private static bool IsConstantPayload(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax => true,
        PrefixUnaryExpressionSyntax unary when unary.OperatorToken.IsKind(SyntaxKind.PlusToken) || unary.OperatorToken.IsKind(SyntaxKind.MinusToken)
            => IsConstantPayload(unary.Operand),
        DefaultExpressionSyntax => true,
        ObjectCreationExpressionSyntax creation when creation.Initializer == null
            => creation.ArgumentList?.Arguments.All(static a => IsConstantPayload(a.Expression)) ?? true,
        _ => false,
    };

    private static bool IsBuilderCall(InvocationExpressionSyntax call, string builderName, string method) =>
        TryGetBuilderName(call, builderName, out var name) && name.Identifier.ValueText == method;

    private static bool TryGetBuilderName(InvocationExpressionSyntax call, string builderName, out IdentifierNameSyntax name)
    {
        if (call.Expression is MemberAccessExpressionSyntax member
            && member.Expression is IdentifierNameSyntax identifier
            && identifier.Identifier.ValueText == builderName
            && member.Name is IdentifierNameSyntax builder)
        {
            name = builder;
            return true;
        }

        name = null!;
        return false;
    }

    private static string EnclosingNamespace(SyntaxNode node) => NamespaceIdentity(node);

    private static string KernelNameFor(InvocationExpressionSyntax compile)
    {
        // The declaration's variable (or field) name becomes the kernel name:
        // 'CompiledPulse = Timeline<...>.Build(Author).Compile()' emits
        // CompiledPulse. Unnamed sites fall back to a sequence number.
        var name = "Timeline" ;
        for (var node = compile.Parent; node != null; node = node.Parent)
        {
            if (node is VariableDeclaratorSyntax declarator)
            {
                name = declarator.Identifier.ValueText;
                break;
            }

            if (node is AssignmentExpressionSyntax { Left: IdentifierNameSyntax assigned })
            {
                name = assigned.Identifier.ValueText;
                break;
            }
        }

        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        return "Compiled" + pascal;
    }

    private static DeclarationDiagnostic At(SyntaxNode node, string path, string code, string message)
    {
        var position = node.GetLocation().GetLineSpan().StartLinePosition;
        return new DeclarationDiagnostic(path, position.Line + 1, position.Character + 1, code, message);
    }
}
