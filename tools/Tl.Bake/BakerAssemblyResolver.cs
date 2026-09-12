using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Tl.Bake;

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

    public Type ResolveTrackType(string typeName)
    {
        var type = Type.GetType(typeName, false);
        if (type != null) return type;

        var parts = typeName.Split(',', 2);
        var rawTypeName = parts[0].Trim();
        var rawAsmName = parts.Length > 1 ? parts[1].Trim() : null;

        if (rawAsmName != null)
        {
            foreach (var asm in _assemblies)
            {
                if (string.Equals(asm.GetName().Name, rawAsmName, StringComparison.OrdinalIgnoreCase))
                {
                    type = asm.GetType(rawTypeName);
                    if (type != null) return type;
                }
            }
        }

        foreach (var asm in _assemblies)
        {
            type = asm.GetType(rawTypeName);
            if (type != null) return type;

            try
            {
                type = asm.GetTypes().FirstOrDefault(t =>
                    string.Equals(t.FullName, rawTypeName, StringComparison.Ordinal) ||
                    string.Equals(t.Name, rawTypeName, StringComparison.Ordinal));
                if (type != null) return type;
            }
            catch (ReflectionTypeLoadException ex)
            {
                type = ex.Types.Where(t => t != null).FirstOrDefault(t =>
                    string.Equals(t!.FullName, rawTypeName, StringComparison.Ordinal) ||
                    string.Equals(t!.Name, rawTypeName, StringComparison.Ordinal));
                if (type != null) return type;
            }
        }

        throw new BakeDiagnosticException($"unknown/unresolvable track type: '{typeName}'");
    }

    public static Type ExtractClipType(Type trackType)
    {
        Type? blendInterface = null;
        foreach (var iface in trackType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IBlend<>))
            {
                if (blendInterface != null)
                {
                    throw new BakeDiagnosticException($"track type '{trackType.FullName}' must implement exactly one Tl.IBlend<TClip> pairing.");
                }
                blendInterface = iface;
            }
        }

        if (blendInterface == null)
        {
            throw new BakeDiagnosticException($"missing IBlend<>: track type '{trackType.FullName}' must implement Tl.IBlend<TClip>.");
        }

        return blendInterface.GetGenericArguments()[0];
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
        var fieldMap = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
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
