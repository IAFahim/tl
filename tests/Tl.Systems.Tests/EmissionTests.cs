using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class EmissionTests
{
    internal static string ReadResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static IEnumerable<MetadataReference> References()
    {
        var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;
            try
            {
                if (assembly.Location.Length > 0) locations.Add(assembly.Location);
            }
            catch (NotSupportedException) { }
        }
        locations.Add(typeof(ITrack<,>).Assembly.Location);
        locations.Add(typeof(object).Assembly.Location);
        return locations.Select(static path => MetadataReference.CreateFromFile(path));
    }

    internal static int FirstDifference(string left, string right)
    {
        var bound = Math.Min(left.Length, right.Length);
        for (var i = 0; i < bound; i++) if (left[i] != right[i]) return i;
        return bound;
    }
}
