using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tl.Gen.CSharp.Analysis;

internal static class Connections
{
    sealed class Use
    {
        public SyntaxNode Site = null!;
        public readonly HashSet<string> Names = new(StringComparer.Ordinal);
        public bool Escaped;
        public bool Seen;
    }

    internal static void Warn(CSharpCompilation compilation, List<DeclarationDiagnostic> diagnostics)
    {
        var clipAttribute = compilation.GetTypeByMetadataName("Tl.TlClipAttribute");
        var stateAttribute = compilation.GetTypeByMetadataName("Tl.TlStateAttribute");
        var frame = compilation.GetTypeByMetadataName("Tl.TlFrame`1");
        if (clipAttribute is null || stateAttribute is null || frame is null) return;
        WarnColumns(compilation, stateAttribute, diagnostics);
        var clips = new Dictionary<INamedTypeSymbol, Use>(SymbolEqualityComparer.Default);
        var states = new Dictionary<INamedTypeSymbol, Use>(SymbolEqualityComparer.Default);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is not INamedTypeSymbol type || type.TypeKind != TypeKind.Struct) continue;
                foreach (var attribute in type.GetAttributes())
                    if (Symbols.Same(attribute.AttributeClass, clipAttribute)) clips[type] = new Use { Site = node };
                    else if (Symbols.Same(attribute.AttributeClass, stateAttribute)) states[type] = new Use { Site = node };
            }
        }
        if (clips.Count == 0 && states.Count == 0) return;
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                SyntaxNode? body = node.Body ?? (SyntaxNode?)node.ExpressionBody;
                if (body is null || model.GetDeclaredSymbol(node) is not IMethodSymbol method) continue;
                foreach (var parameter in method.Parameters)
                    if (parameter.Type is INamedTypeSymbol { IsGenericType: true } framed && Symbols.Same(framed.OriginalDefinition, frame)
                        && framed.TypeArguments[0] is INamedTypeSymbol clip && clips.TryGetValue(clip, out var read))
                        ScanClip(body, model, parameter, read);
                    else if (parameter.Type is INamedTypeSymbol whole && clips.TryGetValue(whole, out var held))
                        ScanWhole(body, model, parameter, held);
            }
        }
        foreach (var pair in clips)
            if (pair.Value.Seen && !pair.Value.Escaped)
                foreach (var field in Fields(pair.Key))
                    if (!pair.Value.Names.Contains(field.Name) && !Reference(field.Type))
                        Symbols.Error(diagnostics, Symbols.Site(field, pair.Value.Site), "TLGEN113", $"Clip '{Symbols.Name(pair.Key)}' field '{field.Name}' is never read by a track of this clip, so it is never connected and stays 0. Read it as frame.Clip.{field.Name}, remove it, or silence TLGEN113.");
        if (states.Count == 0) return;
        var mentions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tree in compilation.SyntaxTrees)
            foreach (var token in tree.GetRoot().DescendantTokens())
                if (token.IsKind(SyntaxKind.IdentifierToken) && token.Parent is not VariableDeclaratorSyntax)
                    mentions.Add(token.ValueText);
        foreach (var pair in states)
            foreach (var field in Fields(pair.Key))
                if (!mentions.Contains(field.Name))
                    Symbols.Error(diagnostics, Symbols.Site(field, pair.Value.Site), "TLGEN114", $"State '{Symbols.Name(pair.Key)}' field '{field.Name}' is declared but its name appears nowhere else in this assembly, so it is never connected and stays default. Write it from a kind, track or consumer, remove it, or silence TLGEN114.");
    }

    static void WarnColumns(CSharpCompilation compilation, INamedTypeSymbol stateAttribute, List<DeclarationDiagnostic> diagnostics)
    {
        var kindAttribute = compilation.GetTypeByMetadataName("Tl.TlKindAttribute");
        var systemAttribute = compilation.GetTypeByMetadataName("Tl.TimelineSystemAttribute");
        var component = compilation.GetTypeByMetadataName("Unity.Entities.IComponentData");
        if (kindAttribute is null) return;
        var kinds = new List<(INamedTypeSymbol Type, SyntaxNode Site)>();
        var contexts = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is not INamedTypeSymbol type || type.TypeKind != TypeKind.Struct) continue;
                foreach (var attribute in type.GetAttributes())
                    if (Symbols.Same(attribute.AttributeClass, kindAttribute)) kinds.Add((type, node));
                    else if (systemAttribute is not null && Symbols.Same(attribute.AttributeClass, systemAttribute))
                        foreach (var field in Fields(type))
                            if (field.Type is not INamedTypeSymbol { IsGenericType: true }) contexts.Add(field.Type);
            }
        }
        bool Shared(ITypeSymbol type)
            => contexts.Contains(type) || type.GetAttributes().Any(a => Symbols.Same(a.AttributeClass, stateAttribute))
            || component is not null && type.AllInterfaces.Any(face => Symbols.Same(face, component));
        foreach (var (type, site) in kinds)
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
            {
                if (!method.IsStatic || method.Name is not ("Apply" or "Idle")) continue;
                foreach (var parameter in method.Parameters.Skip(method.Name == "Apply" ? 1 : 0))
                    if (NewStyle.IsPlainState(parameter.Type) && !Shared(parameter.Type))
                        Symbols.Error(diagnostics, Symbols.Site(parameter, site), "TLGEN117", $"Kind '{Symbols.Name(type)}' {method.Name} column '{Symbols.Name(parameter.Type)}' is not a [TlState] struct, so Unity runs no system for this kind. Mark '{Symbols.Name(parameter.Type)}' [TlState] (partial and top-level); a later version requires it on both hosts.");
            }
    }

    static IEnumerable<IFieldSymbol> Fields(INamedTypeSymbol type)
        => type.GetMembers().OfType<IFieldSymbol>().Where(static f => !f.IsStatic && !f.IsConst && !f.IsImplicitlyDeclared);

    static bool Reference(ITypeSymbol type)
        => type.Name is "TimelineId" or "TlTimeline" && type.ContainingNamespace is { Name: "Tl", ContainingNamespace.IsGlobalNamespace: true };

    static IEnumerable<IdentifierNameSyntax> Uses(SyntaxNode body, SemanticModel model, IParameterSymbol parameter)
        => body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == parameter.Name && Symbols.Same(model.GetSymbolInfo(id).Symbol, parameter));

    static MemberAccessExpressionSyntax? Target(IdentifierNameSyntax id)
        => id.Parent is MemberAccessExpressionSyntax access && access.Expression == id ? access : null;

    static void ScanClip(SyntaxNode body, SemanticModel model, IParameterSymbol parameter, Use use)
    {
        use.Seen = true;
        foreach (var id in Uses(body, model, parameter))
        {
            var access = Target(id);
            if (access is null) use.Escaped = true;
            else if (access.Name.Identifier.ValueText == "Clip")
            {
                if (access.Parent is MemberAccessExpressionSyntax field && field.Expression == access) use.Names.Add(field.Name.Identifier.ValueText);
                else use.Escaped = true;
            }
        }
    }

    static void ScanWhole(SyntaxNode body, SemanticModel model, IParameterSymbol parameter, Use use)
    {
        foreach (var id in Uses(body, model, parameter))
            if (Target(id) is { } access) use.Names.Add(access.Name.Identifier.ValueText);
            else use.Escaped = true;
    }
}
