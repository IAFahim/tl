using System.Reflection;
using System.Text;
using Xunit;

namespace Tl.Bake.Tests;

public class PublicApiTests
{
    [Fact]
    public void PublicApiMatchesApproval()
    {
        var assembly = typeof(Tl.Gen.Tlb.TimelineBaker).Assembly;
        var actual = GetPublicApi(assembly);

        var approvedPath = Path.Combine(AppContext.BaseDirectory, "Tl.Gen.Tlb.PublicApi.approved.txt");
        if (!File.Exists(approvedPath))
        {
            File.WriteAllText(approvedPath, actual);
            var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Tl.Gen.Tlb.PublicApi.approved.txt"));
            if (Directory.Exists(Path.GetDirectoryName(sourcePath)))
                File.WriteAllText(sourcePath, actual);
        }

        var expected = File.ReadAllText(approvedPath);
        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
    }

    public static string GetPublicApi(Assembly assembly)
    {
        var sb = new StringBuilder();
        var types = assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();

        foreach (var type in types)
        {
            sb.AppendLine($"type {type.FullName}");

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                if (member is MethodBase mb && mb.IsSpecialName)
                    continue;
                sb.AppendLine($"  {member.MemberType} {member}");
            }
        }

        return sb.ToString();
    }
}
