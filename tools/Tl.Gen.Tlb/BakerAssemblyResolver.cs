using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Tl.Gen.Tlb;

public sealed class BakerAssemblyResolver
{
    private readonly List<Assembly> _assemblies = [];

    public BakerAssemblyResolver(IEnumerable<string>? assemblyPaths = null)
    {
        _assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

        if (assemblyPaths != null)
        {
            foreach (var path in assemblyPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    var asm = Assembly.LoadFrom(fullPath);
                    if (!_assemblies.Contains(asm))
                        _assemblies.Add(asm);
                }
                else
                {
                    throw new FileNotFoundException($"Referenced assembly file not found: '{path}'", path);
                }
            }
        }
    }

    public void AddAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
    }

    public Type ResolveType(string @namespace, string typeName, string? assemblyName, string context)
    {
        ValidateBareName(@namespace, allowGlobal: true, "namespace", context);
        ValidateBareName(typeName, allowGlobal: false, "type", context);

        var candidates = new List<Type>();
        foreach (var asm in EnumerateCandidateAssemblies())
            foreach (var type in GetTypesSafe(asm))
                if (string.Equals(type.Name, typeName, StringComparison.Ordinal) && MatchesNamespace(type, @namespace))
                    candidates.Add(type);

        IReadOnlyList<Type> scope = candidates;
        if (assemblyName != null)
            scope = candidates.Where(t => string.Equals(t.Assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();

        if (scope.Count == 1)
            return scope[0];

        if (scope.Count == 0)
        {
            if (assemblyName != null && candidates.Count > 0)
            {
                var assemblies = candidates.Select(t => t.Assembly.GetName().Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.Ordinal);
                throw new BakeDiagnosticException($"type resolution failed: assembly '{assemblyName}' does not contain ({FormatNamespace(@namespace)}, {typeName}) for {context}; matching types exist in assembly(es): {string.Join(", ", assemblies)}.");
            }
            throw new BakeDiagnosticException($"unknown/unresolvable type: no loaded type named ({FormatNamespace(@namespace)}, {typeName}) for {context}; reference the defining assembly and check the bare namespace/type spelling.");
        }

        var names = string.Join(", ", scope.Select(t => $"{t.FullName} in {t.Assembly.GetName().Name}").OrderBy(n => n, StringComparer.Ordinal));
        throw new BakeDiagnosticException($"ambiguous type: bare name ({FormatNamespace(@namespace)}, {typeName}) matches multiple loaded types for {context}; declare 'assembly' with one of: {names}.");
    }

    private static string FormatNamespace(string @namespace) => @namespace.Length == 0 ? "<global>" : @namespace;

    private static void ValidateBareName(string value, bool allowGlobal, string field, string context)
    {
        if (value.Length == 0)
        {
            if (allowGlobal) return;
            throw new BakeDiagnosticException($"empty bare name: '{field}' in {context} must be a bare name.");
        }
        if (value.Contains('.') || value.Contains(',') || value.Contains('+') || value.Contains('='))
            throw new BakeDiagnosticException($"dotted name rejected: '{field}' in {context} must be a bare name without '.', ',', '+' or '='; got '{value}'.");
    }

    private static bool MatchesNamespace(Type type, string @namespace) =>
        @namespace.Length == 0
            ? string.IsNullOrEmpty(type.Namespace)
            : string.Equals(type.Namespace, @namespace, StringComparison.Ordinal);

    private IEnumerable<Assembly> EnumerateCandidateAssemblies()
    {
        foreach (var asm in _assemblies)
        {
            if (asm.IsDynamic) continue;
            yield return asm;
        }
    }

    private static IEnumerable<Type> GetTypesSafe(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).Select(t => t!);
        }
    }

    public static bool IsUnmanaged(Type type)
    {
        if (!type.IsValueType || type.IsGenericTypeDefinition)
            return false;
        if (type.IsPrimitive || type.IsPointer || type.IsEnum)
            return true;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!IsUnmanaged(field.FieldType))
                return false;
        }
        return true;
    }

    public static void ValidateUnmanaged(Type trackType, Type clipType)
    {
        if (!IsUnmanaged(trackType))
            throw new BakeDiagnosticException($"track or clip not unmanaged: track type '{trackType.FullName}' is not an unmanaged type.");
        if (!IsUnmanaged(clipType))
            throw new BakeDiagnosticException($"track or clip not unmanaged: clip type '{clipType.FullName}' is not an unmanaged type.");
    }

    public static object PopulateStruct(Type structType, JsonElement element, string contextName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new BakeDiagnosticException($"wrong-typed value for {contextName}: expected object, got {element.ValueKind}.");

        var allFields = structType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var fieldMap = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        foreach (var f in allFields)
        {
            var name = f.Name;
            if (name.StartsWith("<") && name.Contains(">k__BackingField"))
            {
                name = name.Substring(1, name.IndexOf('>') - 1);
            }
            fieldMap[name] = f;
        }

        object boxed = Activator.CreateInstance(structType)!;

        foreach (var prop in element.EnumerateObject())
        {
            if (!fieldMap.TryGetValue(prop.Name, out var field))
            {
                throw new BakeDiagnosticException($"unknown field name in track/payload: field '{prop.Name}' not found on type '{structType.Name}' in {contextName}.");
            }

            var val = ReadPrimitive(prop.Value, field.FieldType, prop.Name, contextName);
            field.SetValue(boxed, val);
        }

        return boxed;
    }

    private static object ReadPrimitive(JsonElement elem, Type targetType, string fieldName, string contextName)
    {
        if (targetType == typeof(bool))
        {
            if (elem.ValueKind == JsonValueKind.True) return true;
            if (elem.ValueKind == JsonValueKind.False) return false;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected bool, got {elem.ValueKind}.");
        }
        if (targetType == typeof(byte))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetByte(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected byte, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(sbyte))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetSByte(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected sbyte, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(short))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt16(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected short, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(ushort))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetUInt16(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected ushort, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(int))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected int, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(uint))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetUInt32(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected uint, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(long))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt64(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected long, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(ulong))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetUInt64(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected ulong, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(float))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetSingle(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected float, got {elem.GetRawText()}.");
        }
        if (targetType == typeof(double))
        {
            if (elem.ValueKind == JsonValueKind.Number && elem.TryGetDouble(out var v)) return v;
            throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} expected double, got {elem.GetRawText()}.");
        }

        throw new BakeDiagnosticException($"wrong-typed value: field '{fieldName}' in {contextName} has unsupported primitive type '{targetType.Name}'.");
    }
}
