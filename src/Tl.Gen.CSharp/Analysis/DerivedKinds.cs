using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

internal static class DerivedKinds
{
    internal static DerivedKind TemplateOf(INamedTypeSymbol kindType, out INamedTypeSymbol? value)
    {
        value = null;
        if (!kindType.IsGenericType || kindType.TypeArguments.Length != 1 || kindType.ContainingNamespace?.ToDisplayString() != "Tl") return DerivedKind.None;
        var template = kindType.OriginalDefinition.MetadataName switch { "Sum`1" => DerivedKind.Sum, "Set`1" => DerivedKind.Set, "While`1" => DerivedKind.While, _ => DerivedKind.None };
        if (template != DerivedKind.None) value = kindType.TypeArguments[0] as INamedTypeSymbol;
        return template;
    }

    internal static bool IsPlainValue(INamedTypeSymbol? value) => value is { TypeKind: TypeKind.Struct, IsUnmanagedType: true, IsGenericType: false };

    internal static KindDecl Build(INamedTypeSymbol kindType, DerivedKind template, INamedTypeSymbol? value, SyntaxNode site, List<DeclarationDiagnostic> diagnostics)
    {
        var display = value is null ? Symbols.Name(kindType) : Symbols.Name(value);
        var label = template + "<" + display + ">";
        var fields = new List<DerivedField>();
        if (!IsPlainValue(value))
            Symbols.Error(diagnostics, site, "TLGEN110", $"Derived kind '{label}' needs a non-generic unmanaged struct as its value type; '{display}' is not one.");
        else
            foreach (var field in value!.GetMembers().OfType<IFieldSymbol>())
            {
                if (field.IsStatic || field.IsConst) continue;
                var kind = field.Type.SpecialType == SpecialType.System_Boolean ? DerivedFieldKind.Boolean
                    : field.Type.SpecialType is SpecialType.System_Single or SpecialType.System_Double ? DerivedFieldKind.Float
                    : IsInteger(field.Type) ? DerivedFieldKind.Integer : DerivedFieldKind.Other;
                fields.Add(new(field.Name, kind));
                if (template == DerivedKind.Sum && kind is DerivedFieldKind.Boolean or DerivedFieldKind.Other)
                    Symbols.Error(diagnostics, Symbols.Site(field, site), "TLGEN110", $"Derived kind '{label}' field '{field.Name}' of type '{Symbols.Name(field.Type)}' is not numeric; every field of a Sum value must be integral or floating-point.");
                else if (template == DerivedKind.While && kind == DerivedFieldKind.Other)
                    Symbols.Error(diagnostics, Symbols.Site(field, site), "TLGEN111", $"Derived kind '{label}' field '{field.Name}' of type '{Symbols.Name(field.Type)}' cannot be combined; While fields are bool, integral or floating-point only.");
            }
        var name = template + "_" + SystemReader.StableName(value ?? kindType).Replace('.', '_').Replace('+', '_');
        var column = new ConsumerParam("column", display, "ref ", ConsumerParamKind.Column, -1, "");
        return new(name, "global::Tl.Derived." + name, display, false, template == DerivedKind.While ? 0 : 2, 0, "", [column], [])
        {
            Id = "Tl.Derived." + name, Derived = template, DerivedFields = fields, IdleParams = template == DerivedKind.While ? [column] : null,
        };
    }

    static bool IsInteger(ITypeSymbol type)
        => type.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64;

    internal static void CheckTwoWriters(IReadOnlyList<KindDecl> kinds, IReadOnlyDictionary<string, SyntaxNode> kindSites, IReadOnlyList<SystemConsumer> consumers, IReadOnlyDictionary<string, SyntaxNode> consumerSites, List<DeclarationDiagnostic> diagnostics)
    {
        var writers = new Dictionary<string, List<(string Name, DerivedKind Derived, SyntaxNode Site)>>(StringComparer.Ordinal);
        void Add(string column, string name, DerivedKind derived, SyntaxNode node)
        {
            if (!writers.TryGetValue(column, out var list)) writers[column] = list = [];
            if (!list.Any(w => w.Name == name)) list.Add((name, derived, node));
        }
        foreach (var kind in kinds)
            foreach (var parameter in kind.ApplyParams.Concat(kind.IdleParams ?? []))
                if (parameter.Kind == ConsumerParamKind.Column) Add(parameter.TypeName, kind.Derived == DerivedKind.None ? kind.Name : kind.Derived + "<" + kind.ValueTypeName + ">", kind.Derived, kindSites[kind.TypeName]);
        foreach (var consumer in consumers)
            foreach (var method in consumer.Methods)
                if (method.Kind is ConsumerMethodKind.ExecuteActive or ConsumerMethodKind.ExecuteBatch)
                    foreach (var parameter in method.Params)
                        if (parameter.Kind == ConsumerParamKind.Column) Add(parameter.TypeName, consumer.ConsumerTypeName, DerivedKind.None, consumerSites[consumer.ConsumerTypeName]);
        foreach (var pair in writers)
            if (pair.Value.Count > 1 && pair.Value.Any(static w => w.Derived is DerivedKind.Set or DerivedKind.While))
                Symbols.Error(diagnostics, pair.Value[1].Site, "TLGEN112", $"Column '{pair.Key}' is written by both '{pair.Value[0].Name}' and '{pair.Value[1].Name}'; a column written by Set<T> or While<T> must have only that one writer.");
    }
}
