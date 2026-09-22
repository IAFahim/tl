using System.Reflection;
using System.Text;
using Xunit;

namespace Tl.Core.Tests;

public class PublicApiTests
{
    [Fact]
    public void PublicApiMatchesApproval()
    {
        var assembly = typeof(ITrack<,>).Assembly;
        var actual = GetPublicApi(assembly);

        var approvedPath = Path.Combine(AppContext.BaseDirectory, "PublicApi.approved.txt");
        Assert.True(File.Exists(approvedPath), "Missing public API approval: PublicApi.approved.txt");
        var expected = File.ReadAllText(approvedPath);
        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
    }

    private static string GetPublicApi(Assembly assembly)
    {
        var sb = new StringBuilder();
        var types = assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();

        foreach (var type in types)
        {
            sb.AppendLine($"type {type.FullName}");

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                if (member is MethodBase { IsSpecialName: true })
                    continue; // Skip property getters/setters/event add/remove
                sb.AppendLine($"  {member.MemberType} {member}");
            }
        }

        return sb.ToString();
    }
}
