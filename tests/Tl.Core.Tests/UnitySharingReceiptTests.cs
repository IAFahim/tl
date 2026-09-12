using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Tl.Core.Tests;

public partial class UnitySharingReceiptTests
{
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null &&
               !(Directory.Exists(Path.Combine(directory.FullName, "src", "Tl.Core")) &&
                 Directory.Exists(Path.Combine(directory.FullName, "unity", "com.iafahim.tl"))))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("repository root not found");
    }

    [Theory]
    [InlineData("Hooks.cs")]
    [InlineData("Playback.cs")]
    [InlineData("Data.cs")]
    public void UnityRuntimeIncludesStayByteIdenticalToTlCore(string name)
    {
        var root = RepoRoot();
        var source = File.ReadAllBytes(Path.Combine(root, "src", "Tl.Core", name));
        var include = Path.Combine(root, "unity", "com.iafahim.tl", "Runtime", name);
        Assert.True(File.Exists(include), $"Unity runtime include missing: {include}");
        Assert.Equal(source, File.ReadAllBytes(include));
    }

    [Fact]
    public void UnityFrameAdapterMirrorsTheSourceFramePublicSurface()
    {
        var root = RepoRoot();
        var source = PublicSurface(Path.Combine(root, "src", "Tl.Core", "Compiled.cs"));
        var adapter = PublicSurface(Path.Combine(root, "unity", "com.iafahim.tl", "Runtime", "FrameAdapter.cs"));
        Assert.Equal(source, adapter);
    }

    private static List<string> PublicSurface(string path)
    {
        var lines = File.ReadAllLines(path);
        var start = -1;
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Contains("struct Frame<TTrack, TClip>"))
            {
                start = i;
                break;
            }
        Assert.True(start >= 0, $"Frame declaration not found in {path}");

        var depth = 0;
        var opened = false;
        var surface = new List<string>();
        for (var i = start; i < lines.Length; i++)
        {
            depth += lines[i].Count(c => c == '{');
            depth -= lines[i].Count(c => c == '}');
            if (lines[i].Contains('{')) opened = true;
            var trimmed = Whitespace().Replace(lines[i].Trim(), " ");
            if (trimmed.StartsWith("public", StringComparison.Ordinal) && !trimmed.Contains("struct Frame"))
            {
                var arrow = trimmed.IndexOf("=>", StringComparison.Ordinal);
                surface.Add(arrow < 0 ? trimmed : trimmed[..arrow].TrimEnd());
            }
            if (opened && depth == 0) break;
        }
        Assert.NotEmpty(surface);
        return surface;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
