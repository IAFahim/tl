using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

internal static class SystemReader
{
    internal sealed record ContractSet(
        INamedTypeSymbol Track,
        INamedTypeSymbol Frame,
        INamedTypeSymbol Blend,
        INamedTypeSymbol Marker,
        INamedTypeSymbol MarkerFrame,
        INamedTypeSymbol Requests,
        INamedTypeSymbol Events,
        INamedTypeSymbol BatchItem,
        INamedTypeSymbol System,
        INamedTypeSymbol SystemMember);

    internal static (IReadOnlyList<SystemDecl> Systems, IReadOnlyList<SystemAssignment> Assignments, IReadOnlyList<DeclarationDiagnostic> Diagnostics) Read(CSharpCompilation compilation)
    {
        var systems = new List<SystemDecl>();
        var consumers = new List<SystemConsumer>();
        var sites = new Dictionary<string, SyntaxNode>(StringComparer.Ordinal);
        var diagnostics = new List<DeclarationDiagnostic>();
        var contracts = Contracts(compilation);
        if (contracts is null) return ([], [], []);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is not INamedTypeSymbol type) continue;
                if (IsSystem(type, contracts)) systems.Add(ReadSystem(type, node, diagnostics));
                var consumer = ReadConsumer(type, node, contracts, compilation, diagnostics);
                if (consumer is not null)
                {
                    consumers.Add(consumer);
                    sites[consumer.ConsumerTypeName] = node;
                }
            }
        }
        consumers.Sort(static (a, b) => string.CompareOrdinal(a.ConsumerTypeName, b.ConsumerTypeName));
        return (systems, Assign(systems, consumers, sites, diagnostics), diagnostics);
    }

    internal static ContractSet? Contracts(Compilation compilation)
    {
        INamedTypeSymbol?[] types =
        [
            compilation.GetTypeByMetadataName("Tl.ITrack`2"),
            compilation.GetTypeByMetadataName("Tl.Frame`2"),
            compilation.GetTypeByMetadataName("Tl.IBlend`1"),
            compilation.GetTypeByMetadataName("Tl.IMarker`1"),
            compilation.GetTypeByMetadataName("Tl.MarkerFrame`1"),
            compilation.GetTypeByMetadataName("Tl.Requests"),
            compilation.GetTypeByMetadataName("Tl.Events`1"),
            compilation.GetTypeByMetadataName("Tl.BatchItem`1"),
            compilation.GetTypeByMetadataName("Tl.TimelineSystemAttribute"),
            compilation.GetTypeByMetadataName("Tl.TimelineSystemMemberAttribute"),
        ];
        return types.Any(static type => type is null)
            ? null
            : new ContractSet(types[0]!, types[1]!, types[2]!, types[3]!, types[4]!, types[5]!, types[6]!, types[7]!, types[8]!, types[9]!);
    }

    static bool IsSystem(INamedTypeSymbol type, ContractSet contracts)
    {
        foreach (var attribute in type.GetAttributes())
            if (Symbols.Same(attribute.AttributeClass?.OriginalDefinition, contracts.System))
                return true;
        return false;
    }

    static SystemDecl ReadSystem(INamedTypeSymbol type, SyntaxNode site, List<DeclarationDiagnostic> diagnostics)
    {
        var name = Symbols.Name(type);
        var fields = new List<SystemField>();
        if (type.TypeKind != TypeKind.Struct)
            Symbols.Error(diagnostics, site, "TLGEN99", $"System '{name}' must be declared as a struct; systems own column spans by value.");
        if (!IsPartial(type))
            Symbols.Error(diagnostics, site, "TLGEN99", $"System '{name}' must be partial; the generator adds the Run wrapper to it.");
        foreach (var member in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsStatic || member.IsConst) continue;
            var fieldType = member.Type as INamedTypeSymbol;
            var element = fieldType is { IsGenericType: true } ? fieldType.TypeArguments[0] : null;
            if (fieldType is not null && IsSpan(fieldType))
            {
                fields.Add(new(member.Name, Symbols.Name(fieldType), SystemFieldKind.Column, Symbols.Name(element!)));
                continue;
            }
            if (fieldType is not null && IsReadOnlySpan(fieldType))
            {
                fields.Add(new(member.Name, Symbols.Name(fieldType), SystemFieldKind.ColumnReadOnly, Symbols.Name(element!)));
                continue;
            }
            if (fieldType is not null && IsTl(fieldType, "Requests") && !fieldType.IsGenericType)
            {
                fields.Add(new(member.Name, Symbols.Name(fieldType), SystemFieldKind.Requests, ""));
                continue;
            }
            if (fieldType is not null && IsTl(fieldType, "Events") && fieldType.IsGenericType && fieldType.TypeArguments.Length == 1)
            {
                fields.Add(new(member.Name, Symbols.Name(fieldType), SystemFieldKind.Events, Symbols.Name(fieldType.TypeArguments[0])));
                continue;
            }
            if (member.Type.IsUnmanagedType)
            {
                fields.Add(new(member.Name, Symbols.Name(member.Type), SystemFieldKind.Context, Symbols.Name(member.Type)));
                continue;
            }
            Symbols.Error(diagnostics, Symbols.Site(member, site), "TLGEN99", $"System '{name}' field '{member.Name}' has managed type '{Symbols.Name(member.Type)}'; context fields must be unmanaged.");
        }
        for (var i = 0; i < fields.Count; i++)
            for (var k = 0; k < i; k++)
                if ((fields[i].Kind is SystemFieldKind.Column or SystemFieldKind.ColumnReadOnly)
                    && fields[i].Kind == fields[k].Kind
                    && fields[i].ElementTypeName == fields[k].ElementTypeName)
                    Symbols.Error(diagnostics, site, "TLGEN99", $"System '{name}' has two '{fields[i].ElementTypeName}' columns; column element types must be distinct.");
        return new(Symbols.Name(type), type.IsRefLikeType, StableKey(type), fields);
    }

    static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
            if (reference.GetSyntax() is TypeDeclarationSyntax syntax
                && syntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
                return true;
        return false;
    }

    static bool IsSpan(INamedTypeSymbol type)
        => type is { IsGenericType: true, Name: "Span" }
        && type.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System"
        && type.TypeArguments.Length == 1;

    static bool IsReadOnlySpan(INamedTypeSymbol type)
        => type is { IsGenericType: true, Name: "ReadOnlySpan" }
        && type.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System"
        && type.TypeArguments.Length == 1;

    static bool IsTl(INamedTypeSymbol type, string name)
        => type.Name == name && type.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "Tl";

    static SystemConsumer? ReadConsumer(INamedTypeSymbol type, SyntaxNode site, ContractSet contracts, Compilation compilation, List<DeclarationDiagnostic> diagnostics)
    {
        if (!TypeAnalyzer.IsNewStyle(type)) return null;
        var name = Symbols.Name(type);
        var local = new List<DeclarationDiagnostic>();
        void Err(string code, string message) => Symbols.Error(local, site, code, message);
        ITypeSymbol? track = null, clip = null, marker = null;
        var blendable = false;
        foreach (var face in type.AllInterfaces)
        {
            if (Symbols.Same(face.OriginalDefinition, contracts.Track))
            {
                track = face.TypeArguments[0];
                clip = face.TypeArguments[1];
                if (!track.IsUnmanagedType || !clip.IsUnmanagedType)
                    Err("TLGEN93", $"Consumer '{name}' track and clip types must be unmanaged.");
            }
            if (Symbols.Same(face.OriginalDefinition, contracts.Marker))
            {
                marker = face.TypeArguments[0];
                if (!marker.IsUnmanagedType) Err("TLGEN93", $"Consumer '{name}' marker type must be unmanaged.");
            }
        }
        if (track is INamedTypeSymbol trackNamed && clip is not null)
            blendable = trackNamed.AllInterfaces.Any(blend => Symbols.Same(blend.OriginalDefinition, contracts.Blend) && blend.TypeArguments.Length == 1 && Symbols.Same(blend.TypeArguments[0], clip));
        if (track is null && marker is null) return null;
        if (track is not null && marker is not null)
        {
            Err("TLGEN99", $"Consumer '{name}' implements both ITrack and IMarker; a consumer is either a pair consumer or a marker consumer.");
            diagnostics.AddRange(local);
            return new(name, Symbols.Name(track), Symbols.Name(clip!), "", true, false, 0, MemberSystems(type, contracts), [], local);
        }
        var members = MemberSystems(type, contracts);
        var methods = new List<ConsumerMethod>();
        var foldTypes = new List<string>();
        var foldSizes = new List<int>();
        var fold = track is null ? null : ReadFold(type, site, contracts, track, clip!, local, foldTypes, foldSizes);
        if (fold is not null) methods.Add(fold);
        var active = track is null ? null : ReadActive(type, site, contracts, track, clip!, local, foldTypes);
        if (active is not null) methods.Add(active);
        var batch = track is null ? null : ReadBatch(type, site, contracts, track, clip!, local, foldTypes);
        if (batch is not null) methods.Add(batch);
        var start = track is null ? null : ReadEvent(type, "OnClipStart", ConsumerMethodKind.OnClipStart, site, contracts, track, clip!, local, foldTypes);
        if (start is not null) methods.Add(start);
        var end = track is null ? null : ReadEvent(type, "OnClipEnd", ConsumerMethodKind.OnClipEnd, site, contracts, track, clip!, local, foldTypes);
        if (end is not null) methods.Add(end);
        var onMarker = marker is null ? null : ReadMarker(type, site, contracts, marker, local);
        if (onMarker is not null) methods.Add(onMarker);
        if (marker is null && HasMethod(type, "OnMarker"))
            Err("TLGEN97", $"Consumer '{name}' declares OnMarker without IMarker<T>; marker consumers implement IMarker<T>.");
        if (active is not null && batch is not null)
            Err("TLGEN95", $"Consumer '{name}' declares both ExecuteActive and ExecuteBatch; batch replaces the active call.");
        if (batch is not null && (fold is null || !foldTypes.Contains(batch.Params[1].BatchElement, StringComparer.Ordinal)))
            Err("TLGEN96", $"Consumer '{name}' ExecuteBatch element must equal one Fold result type.");
        if (active is not null && batch is null && start is null && end is not null)
        {
            var direct = AnalyzeDirect(type, active, fold, compilation.GetSemanticModel(site.SyntaxTree));
            if (direct.Eligible)
            {
                var at = methods.IndexOf(active);
                if (at >= 0) methods[at] = active with { Direct = direct };
            }
        }
        if (methods.Count == 0 && local.Count == 0)
            Err("TLGEN99", $"Consumer '{name}' declares no readable consumer method; add Fold, ExecuteActive, ExecuteBatch, OnClipStart, OnClipEnd, or OnMarker.");
        diagnostics.AddRange(local);
        var key = track is null ? StableKey(marker!) : StablePairKey(track, clip!);
        return new(name, track is null ? "" : Symbols.Name(track), clip is null ? "" : Symbols.Name(clip), marker is null ? "" : Symbols.Name(marker), marker is not null, blendable, key, members, methods, local);
    }

    static bool HasMethod(INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name).OfType<IMethodSymbol>())
            if (member is { IsImplicitlyDeclared: false, IsStatic: true, ReturnsVoid: true, Arity: 0 }) return true;
        return false;
    }

    static List<string> MemberSystems(INamedTypeSymbol type, ContractSet contracts)
    {
        var members = new List<string>();
        foreach (var attribute in type.GetAttributes())
        {
            var ctorArgs = attribute.ConstructorArguments;
            if (ctorArgs.Length == 1 && ctorArgs[0].Value is INamedTypeSymbol system)
                members.Add(Symbols.Name(system));
        }
        return members;
    }

    // Direct-lane detection: ExecuteActive must be a single `col += rhs` /
    // `col -= rhs` statement where rhs mentions only fold feeds, read-only
    // columns, and lane-constant frame members (Direction/Track.*/Clip.*),
    // with an empty OnClipEnd and no start/marker/batch/requests/events.
    // Anything else fails closed to the generic dispatch path.
    static DirectLane AnalyzeDirect(INamedTypeSymbol type, ConsumerMethod active, ConsumerMethod? fold, SemanticModel semantic)
    {
        var site = type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (site is null) return DirectLane.None;
        var symbol = Pick(type, "ExecuteActive", type.ContainingAssembly, new List<DeclarationDiagnostic>(), site);
        var endSymbol = Pick(type, "OnClipEnd", type.ContainingAssembly, new List<DeclarationDiagnostic>(), site);
        if (symbol is null || endSymbol is null) return DirectLane.None;
        if (endSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax endSyntax
            || endSyntax.Body?.Statements.Count != 0) return DirectLane.None;
        if (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax syntax) return DirectLane.None;
        AssignmentExpressionSyntax? assignment = null;
        if (syntax.Body is { Statements.Count: 1 } body
            && body.Statements[0] is ExpressionStatementSyntax expression
            && expression.Expression is AssignmentExpressionSyntax a)
            assignment = a;
        else assignment = syntax.ExpressionBody?.Expression as AssignmentExpressionSyntax;
        if (assignment is null) return DirectLane.None;
        var op = assignment.Kind() switch
        {
            SyntaxKind.AddAssignmentExpression => "+=",
            SyntaxKind.SubtractAssignmentExpression => "-=",
            _ => "",
        };
        if (op.Length == 0) return DirectLane.None;
        string? frame = null, written = null, writtenType = null;
        var feeds = new Dictionary<string, int>(StringComparer.Ordinal);
        var reads = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in active.Params)
        {
            switch (parameter.Kind)
            {
                case ConsumerParamKind.Frame:
                    if (frame is not null) return DirectLane.None;
                    frame = parameter.Name;
                    break;
                case ConsumerParamKind.FoldFeed:
                    feeds[parameter.Name] = parameter.FoldIndex;
                    break;
                case ConsumerParamKind.ColumnReadOnly:
                    reads[parameter.Name] = parameter.TypeName;
                    break;
                case ConsumerParamKind.Column when parameter.Modifier == "ref ":
                    if (written is not null) return DirectLane.None;
                    written = parameter.Name;
                    writtenType = parameter.TypeName;
                    break;
                default:
                    return DirectLane.None;
            }
        }
        if (written is null || writtenType is null) return DirectLane.None;
        var member = "";
        if (assignment.Left is IdentifierNameSyntax target)
        {
            if (target.Identifier.Text != written) return DirectLane.None;
        }
        else if (assignment.Left is MemberAccessExpressionSyntax access
            && access.Expression is IdentifierNameSyntax root
            && root.Identifier.Text == written
            && access.Name is IdentifierNameSyntax fieldName)
        {
            if (semantic.GetSymbolInfo(access).Symbol is not IFieldSymbol field) return DirectLane.None;
            if (field.Type.SpecialType is not (SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Int32 or SpecialType.System_UInt32)) return DirectLane.None;
            member = fieldName.Identifier.Text;
        }
        else return DirectLane.None;
        var usedOffsets = new List<int>();
        var usedReads = new List<string>();
        var laneNeeded = false;
        if (!RhsOk(assignment.Right, fold, feeds, reads, frame, usedOffsets, usedReads, ref laneNeeded)) return DirectLane.None;
        if (!laneNeeded && usedReads.Count == 0) return DirectLane.None;
        if (laneNeeded && usedOffsets.Count == 0) usedOffsets.Add(0);
        return new DirectLane(true, op, writtenType, member, [.. usedOffsets], [.. usedReads]);
    }

    static bool RhsOk(ExpressionSyntax node, ConsumerMethod? fold, Dictionary<string, int> feeds, Dictionary<string, string> reads, string? frame, List<int> usedOffsets, List<string> usedReads, ref bool laneNeeded)
    {
        switch (node)
        {
            case IdentifierNameSyntax id:
                if (feeds.TryGetValue(id.Identifier.Text, out var feed))
                {
                    var offset = FoldOffsetOf(fold, feed);
                    if (offset < 0) return false;
                    if (!usedOffsets.Contains(offset)) usedOffsets.Add(offset);
                    laneNeeded = true;
                    return true;
                }
                if (reads.TryGetValue(id.Identifier.Text, out var read))
                {
                    if (!usedReads.Contains(read)) usedReads.Add(read);
                    return true;
                }
                return false;
            case MemberAccessExpressionSyntax member:
                if (!FrameRootedOk(member, frame)) return false;
                laneNeeded = true;
                return true;
            case LiteralExpressionSyntax literal:
                return literal.IsKind(SyntaxKind.NumericLiteralExpression);
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression)
                || binary.IsKind(SyntaxKind.SubtractExpression)
                || binary.IsKind(SyntaxKind.MultiplyExpression):
                return RhsOk(binary.Left, fold, feeds, reads, frame, usedOffsets, usedReads, ref laneNeeded)
                    && RhsOk(binary.Right, fold, feeds, reads, frame, usedOffsets, usedReads, ref laneNeeded);
            case ParenthesizedExpressionSyntax paren:
                return RhsOk(paren.Expression, fold, feeds, reads, frame, usedOffsets, usedReads, ref laneNeeded);
            case PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.UnaryMinusExpression):
                return RhsOk(unary.Operand, fold, feeds, reads, frame, usedOffsets, usedReads, ref laneNeeded);
            default:
                return false;
        }
    }

    static int FoldOffsetOf(ConsumerMethod? fold, int feed)
    {
        // Lane layout mirrors EmitFeeds: fold outs packed in declaration order.
        // Only float outs participate; anything else fails closed.
        if (fold is null) return -1;
        var offset = 0;
        for (var o = 0; o < fold.Params.Count; o++)
        {
            if (fold.Params[o].Kind is not ConsumerParamKind.FoldResult) continue;
            if (fold.Params[o].FoldIndex == feed)
                return fold.FoldTypes[o] is "float" or "System.Single" && fold.FoldSizes[o] == 4 ? offset : -1;
            offset += fold.FoldSizes[o];
        }
        return -1;
    }

    static bool FrameRootedOk(MemberAccessExpressionSyntax node, string? frame)
    {
        if (frame is null) return false;
        var names = new List<string>();
        ExpressionSyntax current = node;
        while (current is MemberAccessExpressionSyntax access)
        {
            names.Add(access.Name.Identifier.Text);
            current = access.Expression;
        }
        if (current is not IdentifierNameSyntax root || root.Identifier.Text != frame) return false;
        var first = names[names.Count - 1];
        if (first is not ("Direction" or "Track" or "Clip")) return false;
        return first is not "Direction" || names.Count == 1;
    }

    static IMethodSymbol? Pick(INamedTypeSymbol type, string name, IAssemblySymbol assembly, List<DeclarationDiagnostic> diagnostics, SyntaxNode site)
    {
        IMethodSymbol? found = null;
        var duplicates = 0;
        foreach (var member in type.GetMembers(name).OfType<IMethodSymbol>())
        {
            if (member is not { IsImplicitlyDeclared: false, IsStatic: true, ReturnsVoid: true, Arity: 0 }) continue;
            if (member.DeclaredAccessibility != Accessibility.Public
                && !SymbolEqualityComparer.Default.Equals(member.ContainingAssembly, assembly)) continue;
            if (member.Parameters.Any(static parameter => parameter.Type.TypeKind == TypeKind.Error)) continue;
            if (found is null) found = member;
            else duplicates++;
        }
        if (duplicates > 0)
            Symbols.Error(diagnostics, site, "TLGEN99", $"Consumer '{Symbols.Name(type)}' declares multiple {name} methods; keep exactly one.");
        return found;
    }

    static ConsumerMethod? ReadFold(INamedTypeSymbol type, SyntaxNode site, ContractSet contracts, ITypeSymbol track, ITypeSymbol clip, List<DeclarationDiagnostic> diagnostics, List<string> foldTypes, List<int> foldSizes)
    {
        var method = Pick(type, "Fold", type.ContainingAssembly, diagnostics, site);
        if (method is null) return null;
        var name = Symbols.Name(type);
        var frame = contracts.Frame.Construct(track, clip);
        var parameters = new List<ConsumerParam>();
        var index = 0;
        if (method.Parameters.Length > 0 && Symbols.Same(method.Parameters[0].Type, frame))
            index = 1;
        else if (method.Parameters.Length > 0)
        {
            Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' Fold must begin with in {Symbols.Name(frame)}.");
            return null;
        }
        for (; index < method.Parameters.Length; index++)
        {
            var parameter = method.Parameters[index];
            if (parameter.IsOptional || parameter.IsParams || !parameter.Type.IsUnmanagedType || parameter.RefKind is not (RefKind.Ref or RefKind.Out))
            {
                Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' Fold results must be required unmanaged out/ref ('{Symbols.Name(parameter.Type)} {parameter.Name}' is not).");
                return null;
            }
            var size = ResultSize(parameter.Type);
            if (size is 0 or > 8 || foldTypes.Count >= 10)
            {
                Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' Fold folds at most 10 unmanaged results of at most 8 bytes.");
                return null;
            }
            foldTypes.Add(Symbols.Name(parameter.Type));
            foldSizes.Add(size);
            parameters.Add(new(parameter.Name, Symbols.Name(parameter.Type), parameter.RefKind == RefKind.Out ? "out " : "ref ", ConsumerParamKind.FoldResult, foldTypes.Count - 1, ""));
        }
        if (parameters.Count == 0)
        {
            Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' Fold produces no result.");
            return null;
        }
        return new(ConsumerMethodKind.Fold, parameters, [.. foldTypes], [.. foldSizes]);
    }

    static ConsumerMethod? ReadActive(INamedTypeSymbol type, SyntaxNode site, ContractSet contracts, ITypeSymbol track, ITypeSymbol clip, List<DeclarationDiagnostic> diagnostics, List<string> foldTypes)
    {
        var method = Pick(type, "ExecuteActive", type.ContainingAssembly, diagnostics, site);
        if (method is null) return null;
        var name = Symbols.Name(type);
        var frame = contracts.Frame.Construct(track, clip);
        var parameters = new List<ConsumerParam>();
        var index = 0;
        if (method.Parameters.Length > 0 && method.Parameters[0].RefKind == RefKind.In && Symbols.Same(method.Parameters[0].Type, frame))
        {
            parameters.Add(new(method.Parameters[0].Name, Symbols.Name(frame), "in ", ConsumerParamKind.Frame, -1, ""));
            index = 1;
        }
        var feed = 0;
        while (index < method.Parameters.Length && feed < foldTypes.Count
            && method.Parameters[index].RefKind == RefKind.In
            && string.Equals(Symbols.Name(method.Parameters[index].Type), foldTypes[feed], StringComparison.Ordinal))
        {
            parameters.Add(new(method.Parameters[index].Name, foldTypes[feed], "in ", ConsumerParamKind.FoldFeed, feed, ""));
            index++;
            feed++;
        }
        for (; index < method.Parameters.Length; index++)
        {
            if (!BindValue(type, method.Name, method.Parameters[index], contracts, diagnostics, site, out var bound)) return null;
            parameters.Add(bound);
        }
        return CheckColumns(type, method.Name, parameters, diagnostics, site);
    }

    static ConsumerMethod? ReadBatch(INamedTypeSymbol type, SyntaxNode site, ContractSet contracts, ITypeSymbol track, ITypeSymbol clip, List<DeclarationDiagnostic> diagnostics, List<string> foldTypes)
    {
        var method = Pick(type, "ExecuteBatch", type.ContainingAssembly, diagnostics, site);
        if (method is null) return null;
        var name = Symbols.Name(type);
        var frame = contracts.Frame.Construct(track, clip);
        var parameters = new List<ConsumerParam>();
        if (method.Parameters.Length == 0 || method.Parameters[0].RefKind != RefKind.In || !Symbols.Same(method.Parameters[0].Type, frame))
        {
            Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' ExecuteBatch must begin with in {Symbols.Name(frame)}.");
            return null;
        }
        parameters.Add(new(method.Parameters[0].Name, Symbols.Name(frame), "in ", ConsumerParamKind.Frame, -1, ""));
        if (method.Parameters.Length < 2 || !IsBatchItems(method.Parameters[1].Type, contracts, out var element))
        {
            Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' ExecuteBatch must take ReadOnlySpan<Tl.BatchItem<R>> after the frame.");
            return null;
        }
        parameters.Add(new(method.Parameters[1].Name, Symbols.Name(method.Parameters[1].Type), "", ConsumerParamKind.BatchItems, -1, element));
        for (var index = 2; index < method.Parameters.Length; index++)
        {
            if (!BindValue(type, method.Name, method.Parameters[index], contracts, diagnostics, site, out var bound, batchColumns: true)) return null;
            if (bound.Kind is ConsumerParamKind.Requests or ConsumerParamKind.Events)
            {
                Symbols.Error(diagnostics, site, "TLGEN96", $"Consumer '{name}' ExecuteBatch takes {bound.Kind}; batch consumers take only columns and context, requests and events stay per playback.");
                return null;
            }
            parameters.Add(bound);
        }
        return CheckColumns(type, method.Name, parameters, diagnostics, site);
    }

    static ConsumerMethod? ReadEvent(INamedTypeSymbol type, string methodName, ConsumerMethodKind kind, SyntaxNode site, ContractSet contracts, ITypeSymbol track, ITypeSymbol clip, List<DeclarationDiagnostic> diagnostics, List<string> foldTypes)
    {
        var method = Pick(type, methodName, type.ContainingAssembly, diagnostics, site);
        if (method is null) return null;
        var name = Symbols.Name(type);
        var frame = contracts.Frame.Construct(track, clip);
        if (method.Parameters.Length == 0 || method.Parameters[0].RefKind != RefKind.In || !Symbols.Same(method.Parameters[0].Type, frame))
        {
            Symbols.Error(diagnostics, site, "TLGEN92", $"Consumer '{name}' {methodName} must begin with in {Symbols.Name(frame)}.");
            return null;
        }
        var parameters = new List<ConsumerParam> { new(method.Parameters[0].Name, Symbols.Name(frame), "in ", ConsumerParamKind.Frame, -1, "") };
        for (var index = 1; index < method.Parameters.Length; index++)
        {
            var parameter = method.Parameters[index];
            if (foldTypes.Contains(Symbols.Name(parameter.Type), StringComparer.Ordinal))
            {
                Symbols.Error(diagnostics, site, "TLGEN92", $"Consumer '{name}' {methodName} takes a fold result; folds exist only at load.");
                return null;
            }
            if (!BindValue(type, methodName, parameter, contracts, diagnostics, site, out var bound, eventMethod: true)) return null;
            parameters.Add(bound);
        }
        return new(kind, parameters, [.. foldTypes], []);
    }

    static ConsumerMethod? ReadMarker(INamedTypeSymbol type, SyntaxNode site, ContractSet contracts, ITypeSymbol marker, List<DeclarationDiagnostic> diagnostics)
    {
        var method = Pick(type, "OnMarker", type.ContainingAssembly, diagnostics, site);
        if (method is null) return null;
        var name = Symbols.Name(type);
        var frame = contracts.MarkerFrame.Construct(marker);
        if (method.Parameters.Length == 0 || method.Parameters[0].RefKind != RefKind.In || !Symbols.Same(method.Parameters[0].Type, frame))
        {
            Symbols.Error(diagnostics, site, "TLGEN97", $"Consumer '{name}' OnMarker must begin with in {Symbols.Name(frame)}.");
            return null;
        }
        var parameters = new List<ConsumerParam> { new(method.Parameters[0].Name, Symbols.Name(frame), "in ", ConsumerParamKind.MarkerFrame, -1, "") };
        for (var index = 1; index < method.Parameters.Length; index++)
        {
            if (!BindValue(type, method.Name, method.Parameters[index], contracts, diagnostics, site, out var bound, eventMethod: true)) return null;
            parameters.Add(bound);
        }
        return new(ConsumerMethodKind.OnMarker, parameters, [], []);
    }

    static bool BindValue(INamedTypeSymbol type, string methodName, IParameterSymbol parameter, ContractSet contracts, List<DeclarationDiagnostic> diagnostics, SyntaxNode site, out ConsumerParam bound, bool eventMethod = false, bool batchColumns = false)
    {
        bound = new("", "", "", ConsumerParamKind.Context, -1, "");
        var name = Symbols.Name(type);
        var paramType = parameter.Type as INamedTypeSymbol;
        if (paramType is not null && Symbols.Same(paramType.OriginalDefinition, contracts.Requests))
        {
            if (parameter.RefKind != RefKind.Ref)
            {
                Symbols.Error(diagnostics, Symbols.Site(parameter, site), "TLGEN94", $"Consumer '{name}' {methodName} takes Requests by in; Requests is bound per entity and must be ref.");
                return false;
            }
            bound = new(parameter.Name, Symbols.Name(parameter.Type), "ref ", ConsumerParamKind.Requests, -1, "");
            return true;
        }
        if (paramType is { IsGenericType: true } && Symbols.Same(paramType.ConstructedFrom, contracts.Events))
        {
            if (parameter.RefKind != RefKind.Ref)
            {
                Symbols.Error(diagnostics, Symbols.Site(parameter, site), "TLGEN94", $"Consumer '{name}' {methodName} takes Events by in; the writer is bound per entity and must be ref.");
                return false;
            }
            bound = new(parameter.Name, Symbols.Name(parameter.Type), "ref ", ConsumerParamKind.Events, -1, "");
            return true;
        }
        if (parameter.Type is INamedTypeSymbol span && (IsSpan(span) || IsReadOnlySpan(span)))
        {
            if (!batchColumns || span.TypeArguments.Length != 1 || !span.TypeArguments[0].IsUnmanagedType)
            {
                Symbols.Error(diagnostics, Symbols.Site(parameter, site), "TLGEN93", $"Consumer '{name}' {methodName} parameter '{parameter.Name}' is a span; systems own spans, consumers take elements.");
                return false;
            }
            var spanKind = IsSpan(span) ? ConsumerParamKind.Column : ConsumerParamKind.ColumnReadOnly;
            bound = new(parameter.Name, Symbols.Name(span.TypeArguments[0]), "", spanKind, -1, "");
            return true;
        }
        if (!parameter.Type.IsUnmanagedType || parameter.RefKind == RefKind.Out || parameter.IsOptional || parameter.IsParams)
        {
            Symbols.Error(diagnostics, Symbols.Site(parameter, site), "TLGEN93", $"Consumer '{name}' {methodName} parameter '{Symbols.Name(parameter.Type)} {parameter.Name}' must be a required unmanaged in/ref column, context, Requests, or Events type.");
            return false;
        }
        var kind = parameter.RefKind == RefKind.Ref ? ConsumerParamKind.Column : ConsumerParamKind.ColumnReadOnly;
        bound = new(parameter.Name, Symbols.Name(parameter.Type), parameter.RefKind == RefKind.Ref ? "ref " : "in ", kind, -1, "");
        return true;
    }

    static ConsumerMethod? CheckColumns(INamedTypeSymbol type, string methodName, List<ConsumerParam> parameters, List<DeclarationDiagnostic> diagnostics, SyntaxNode site)
    {
        var live = parameters.Count(static parameter => parameter.Kind is ConsumerParamKind.Column or ConsumerParamKind.ColumnReadOnly);
        if (live > 30)
        {
            Symbols.Error(diagnostics, site, "TLGEN90", $"Consumer '{Symbols.Name(type)}' {methodName} declares {live} live columns; the consumer ABI holds at most 30 gameplay columns per consumer.");
            return null;
        }
        for (var i = 0; i < parameters.Count; i++)
            for (var k = 0; k < i; k++)
                if (parameters[i].Kind == parameters[k].Kind
                    && parameters[i].Kind is ConsumerParamKind.Column or ConsumerParamKind.ColumnReadOnly
                    && string.Equals(parameters[i].TypeName, parameters[k].TypeName, StringComparison.Ordinal))
                {
                    Symbols.Error(diagnostics, site, "TLGEN90", $"Consumer '{Symbols.Name(type)}' {methodName} declares two live '{parameters[i].TypeName}' columns; TypeKey binding cannot distinguish same-type columns.");
                    return null;
                }
        var kind = methodName == "ExecuteBatch" ? ConsumerMethodKind.ExecuteBatch : ConsumerMethodKind.ExecuteActive;
        return new(kind, parameters, [], []);
    }

    static bool IsBatchItems(ITypeSymbol type, ContractSet contracts, out string element)
    {
        element = "";
        if (type is not INamedTypeSymbol span || !span.IsGenericType || !IsReadOnlySpan(span)) return false;
        if (span.TypeArguments.Length != 1 || span.TypeArguments[0] is not INamedTypeSymbol inner) return false;
        if (!inner.IsGenericType || !Symbols.Same(inner.OriginalDefinition, contracts.BatchItem)) return false;
        if (inner.TypeArguments.Length != 1 || !inner.TypeArguments[0].IsUnmanagedType) return false;
        element = Symbols.Name(inner.TypeArguments[0]);
        return true;
    }

    static byte ResultSize(ITypeSymbol type)
        => type switch
        {
            INamedTypeSymbol { EnumUnderlyingType: { } u } => ResultSize(u),
            { SpecialType: SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte } => 1,
            { SpecialType: SpecialType.System_Char or SpecialType.System_UInt16 or SpecialType.System_Int16 } => 2,
            { SpecialType: SpecialType.System_UInt32 or SpecialType.System_Int32 or SpecialType.System_Single } => 4,
            { SpecialType: SpecialType.System_UInt64 or SpecialType.System_Int64 or SpecialType.System_Double } => 8,
            _ => 0,
        };

    internal static ulong StablePairKey(ITypeSymbol track, ITypeSymbol clip)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in StableName(track)) hash = (hash ^ c) * 1099511628211UL;
        hash = (hash ^ '\0') * 1099511628211UL;
        foreach (var c in StableName(clip)) hash = (hash ^ c) * 1099511628211UL;
        return hash | 1UL;
    }

    internal static ulong StableKey(ITypeSymbol type)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in StableName(type)) hash = (hash ^ c) * 1099511628211UL;
        return hash | 1UL;
    }

    internal static string StableName(ITypeSymbol type)
    {
        var nested = new Stack<string>();
        var current = type;
        while (current is not null)
        {
            nested.Push(current.Name + TypeArguments(current));
            current = current.ContainingType;
        }
        var prefix = type.ContainingNamespace is { IsGlobalNamespace: false } space ? space.ToDisplayString() + "." : "";
        return prefix + string.Join("+", nested) + ", " + type.ContainingAssembly.Name;
    }

    static string TypeArguments(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named || !named.IsGenericType) return "";
        var text = "<";
        foreach (var argument in named.TypeArguments) text += StableName(argument) + ",";
        return text;
    }

    static IReadOnlyList<SystemAssignment> Assign(
        IReadOnlyList<SystemDecl> systems,
        IReadOnlyList<SystemConsumer> consumers,
        IReadOnlyDictionary<string, SyntaxNode> sites,
        List<DeclarationDiagnostic> diagnostics)
    {
        var result = new List<SystemAssignment>();
        var systemNames = new HashSet<string>(systems.Select(static system => system.TypeName), StringComparer.Ordinal);
        var owned = new HashSet<SystemConsumer>();
        foreach (var system in systems)
        {
            var mine = new List<SystemConsumer>();
            foreach (var consumer in consumers)
            {
                if (!Fits(system, consumer)) continue;
                if (consumer.MemberSystems.Count == 0)
                {
                    var rivals = 0;
                    foreach (var other in systems)
                        if (other != system && Fits(other, consumer)) rivals++;
                    if (rivals > 0)
                    {
                        Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN91", $"Consumer '{consumer.ConsumerTypeName}' fits {rivals + 1} systems; add [TimelineSystemMember(typeof({system.TypeName}))] to pick one.");
                        continue;
                    }
                    mine.Add(consumer);
                    owned.Add(consumer);
                    continue;
                }
                if (consumer.MemberSystems.Contains(system.TypeName, StringComparer.Ordinal))
                {
                    mine.Add(consumer);
                    owned.Add(consumer);
                }
            }
            var markers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ownedConsumer in mine)
                if (ownedConsumer.IsMarker) markers.Add(ownedConsumer.MarkerTypeName);
            if (markers.Count > 1)
                Symbols.Error(diagnostics, sites[mine[0].ConsumerTypeName], "TLGEN99", $"System '{system.TypeName}' has {markers.Count} marker types; a system routes at most one marker type until marker keys ship in the library view.");
            result.Add(new(system, mine, []));
        }
        foreach (var consumer in consumers)
        {
            if (owned.Contains(consumer)) continue;
            var memberUnknown = false;
            foreach (var member in consumer.MemberSystems)
                if (!systemNames.Contains(member))
                {
                    Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN99", $"Consumer '{consumer.ConsumerTypeName}' names unknown system '{member}' in TimelineSystemMember.");
                    memberUnknown = true;
                }
            if (memberUnknown) continue;
            var fitting = 0;
            var membered = 0;
            foreach (var system in systems)
            {
                if (!Fits(system, consumer)) continue;
                fitting++;
                if (consumer.MemberSystems.Contains(system.TypeName, StringComparer.Ordinal)) membered++;
            }
            if (membered > 0) continue;
            if (consumer.MemberSystems.Count > 0)
            {
                if (fitting > 0)
                    Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN91", $"Consumer '{consumer.ConsumerTypeName}' names systems that do not fit it; a fitting system exists outside its TimelineSystemMember list.");
                else if (FirstMissingParam(consumer, systems) is { } missing)
                    Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN90", $"Consumer '{consumer.ConsumerTypeName}' needs {missing.Kind} '{missing.Type}' but no system provides it.");
                else
                    Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN98", $"Consumer '{consumer.ConsumerTypeName}' fits no system; it never runs.");
                continue;
            }
            if (fitting > 0) continue;
            if (FirstMissingParam(consumer, systems) is { } uncovered)
                Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN90", $"Consumer '{consumer.ConsumerTypeName}' needs {uncovered.Kind} '{uncovered.Type}' but no system provides it.");
            else
                Symbols.Error(diagnostics, sites[consumer.ConsumerTypeName], "TLGEN98", $"Consumer '{consumer.ConsumerTypeName}' fits no system; it never runs.");
        }
        return result;
    }
    static (string Kind, string Type)? FirstMissingParam(SystemConsumer consumer, IReadOnlyList<SystemDecl> systems)
    {
        foreach (var method in consumer.Methods)
            foreach (var parameter in method.Params)
            {
                var covered = false;
                foreach (var system in systems)
                {
                    switch (parameter.Kind)
                    {
                        case ConsumerParamKind.Frame:
                        case ConsumerParamKind.MarkerFrame:
                        case ConsumerParamKind.FoldFeed:
                        case ConsumerParamKind.FoldResult:
                        case ConsumerParamKind.BatchItems:
                            covered = true;
                            break;
                        case ConsumerParamKind.Requests:
                            covered = system.Fields.Any(static field => field.Kind == SystemFieldKind.Requests);
                            break;
                        case ConsumerParamKind.Events:
                            covered = system.Fields.Any(field => field.Kind == SystemFieldKind.Events && string.Equals(field.ElementTypeName, EventElement(parameter.TypeName), StringComparison.Ordinal));
                            break;
                        case ConsumerParamKind.Column:
                            covered = system.Fields.Any(field => field.Kind == SystemFieldKind.Column && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal))
                                || system.Fields.Any(field => field.Kind == SystemFieldKind.Context && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal));
                            break;
                        default:
                            covered = system.Fields.Any(field => (field.Kind == SystemFieldKind.Column || field.Kind == SystemFieldKind.ColumnReadOnly) && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal))
                                || system.Fields.Any(field => field.Kind == SystemFieldKind.Context && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal));
                            break;
                    }
                    if (covered) break;
                }
                if (!covered)
                    return (parameter.Kind switch
                    {
                        ConsumerParamKind.Column or ConsumerParamKind.ColumnReadOnly => "column",
                        ConsumerParamKind.Context => "context",
                        ConsumerParamKind.Requests => "Requests",
                        _ => "Events",
                    }, parameter.Kind == ConsumerParamKind.Events ? EventElement(parameter.TypeName) : parameter.TypeName);
            }
        return null;
    }

    static bool Fits(SystemDecl system, SystemConsumer consumer)
    {
        foreach (var method in consumer.Methods)
            foreach (var parameter in method.Params)
            {
                switch (parameter.Kind)
                {
                    case ConsumerParamKind.Frame:
                    case ConsumerParamKind.MarkerFrame:
                    case ConsumerParamKind.FoldFeed:
                    case ConsumerParamKind.FoldResult:
                    case ConsumerParamKind.BatchItems:
                        continue;
                    case ConsumerParamKind.Requests:
                        if (!system.Fields.Any(static field => field.Kind == SystemFieldKind.Requests)) return false;
                        continue;
                    case ConsumerParamKind.Events:
                        var element = EventElement(parameter.TypeName);
                        if (!system.Fields.Any(field => field.Kind == SystemFieldKind.Events && string.Equals(field.ElementTypeName, element, StringComparison.Ordinal))) return false;
                        continue;
                    case ConsumerParamKind.Column:
                        if (!system.Fields.Any(field => field.Kind == SystemFieldKind.Column && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal))
                            && !system.Fields.Any(field => field.Kind == SystemFieldKind.Context && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal))) return false;
                        continue;
                    default:
                        if (!system.Fields.Any(field => (field.Kind == SystemFieldKind.Column || field.Kind == SystemFieldKind.ColumnReadOnly) && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal))
                            && !system.Fields.Any(field => field.Kind == SystemFieldKind.Context && string.Equals(field.ElementTypeName, parameter.TypeName, StringComparison.Ordinal))) return false;
                        continue;
                }
            }
        return true;
    }

    static string EventElement(string typeName)
    {
        var start = typeName.IndexOf('<');
        var end = typeName.LastIndexOf('>');
        return start >= 0 && end > start ? typeName.Substring(start + 1, end - start - 1) : typeName;
    }
}
