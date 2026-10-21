#if !TL_CHECKED
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Tl.Core.Tests;

public class IlScanCheckedTests
{
    [Fact]
    public void ShippedReleaseILContainsNoCheckedGuardCalls()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Tl.Core.dll");
        Assert.True(File.Exists(path), $"Missing {path}");
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();

        var checkedTokens = TokensOf(reader, "Tl", "Checked");
        Assert.NotEmpty(checkedTokens);

        var calls = 0;
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            foreach (var methodHandle in type.GetMethods())
            {
                var method = reader.GetMethodDefinition(methodHandle);
                if (method.RelativeVirtualAddress == 0) continue;
                var body = pe.GetMethodBody(method.RelativeVirtualAddress);
                var il = body.GetILBytes() ?? [];
                for (var i = 0; i < il.Length - 4; i++)
                {
                    if (il[i] != 0x28 && il[i] != 0x6F && il[i] != 0x73) continue;
                    var token = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                    if (checkedTokens.Contains(token)) calls++;
                }
            }
        }
        Assert.True(
            calls == 0,
            $"Tl.Core.dll without TL_CHECKED contains {calls} call sites into Tl.Checked; the O(n) domain and overlap guards must lower to zero IL.");
    }

    static HashSet<int> TokensOf(MetadataReader reader, string namespaceName, string typeName)
    {
        var tokens = new HashSet<int>();
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            if (reader.GetString(type.Namespace) != namespaceName || reader.GetString(type.Name) != typeName) continue;
            foreach (var method in type.GetMethods())
                tokens.Add(MetadataTokens.GetToken(method));
        }
        return tokens;
    }
}
#endif
