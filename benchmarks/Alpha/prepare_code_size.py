import argparse
import pathlib


def project(repository: pathlib.Path) -> str:
    core = repository / "src/Tl.Core/bin/Release/net10.0/Tl.Core.dll"
    generator = repository / "src/Tl.Gen.CSharp/bin/Release/netstandard2.0/Tl.Gen.CSharp.dll"
    compiler = repository / "src/Tl.Gen.CSharp/bin/Release/netstandard2.0/Tl.Compiler.dll"
    return f"""<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <PublishAot>true</PublishAot>
    <StripSymbols>false</StripSymbols>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=\"Tl.Core\" HintPath=\"{core}\" />
    <Analyzer Include=\"{generator}\" />
    <Analyzer Include=\"{compiler}\" />
  </ItemGroup>
</Project>
"""


def domain(mode: str, count: int) -> str:
    declarations = []
    assets = []
    if mode == "assets":
        for index in range(count):
            name = f"Asset{index}"
            declarations.append(f"""public readonly partial struct {name} : ITimeline
{{
    public static void Define(scoped Builder builder)
    {{
        var track = builder.Track(new Track({index + 1})).Use<Job>();
        builder.Clip(track, new Clip({index + 1}), 0u, 2u);
    }}
}}""")
            assets.append(name)
    else:
        tracks = []
        for index in range(count):
            tracks.append(f"""        var track{index} = builder.Track(new Track({index + 1})).Use<Job>();
        builder.Clip(track{index}, new Clip({index + 1}), 0u, 2u);""")
        declarations.append(f"""public readonly partial struct Wide : ITimeline
{{
    public static void Define(scoped Builder builder)
    {{
{chr(10).join(tracks)}
    }}
}}""")
        assets.append("Wide")
    asset_chain = "".join(f".Asset<{asset}>()" for asset in assets)
    return f"""using System.Runtime.CompilerServices;
using Tl;
namespace Scale;
public readonly record struct Clip(int Value);
public readonly record struct Track(int Value) : IBlend<Clip>
{{
    public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
}}
public readonly struct Job : ITimelineJob<Track, Clip>
{{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Execute(in Frame<Track, Clip> frame, ref long value)
        => value = unchecked(value * 397 + frame.Direction * frame.Track.Value + frame.Clip.Value + frame.GameTick);
}}
{chr(10).join(declarations)}
public readonly struct Rows;
public readonly partial struct Catalog : ITimelineCatalog
{{
    public static void Define(scoped CatalogBuilder builder)
    {{
        builder.Schema<Rows>(){asset_chain};
    }}
}}
"""


def program(mode: str, count: int) -> str:
    asset_count = count if mode == "assets" else 1
    return f"""using Scale;
var states = new Catalog.State[1];
var values = new long[1];
var query = new Catalog.Query().Rows(states, values);
for (var asset = 1; asset <= {asset_count}; asset++)
{{
    states[0] = new Catalog.State((Catalog.Asset)asset);
    query.Tick((uint)asset);
    query.Tick((uint)asset + 1u, -1);
}}
var batchStates = new[] {{ new Catalog.State((Catalog.Asset)1), new Catalog.State((Catalog.Asset){asset_count}) }};
var batchValues = new long[2];
var batch = new Catalog.Query().Rows(batchStates, batchValues);
batch.Tick(1u, 1);
batch.Tick(2u, -1);
Console.WriteLine(values[0] + batchValues[0] + batchValues[1]);
"""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", type=pathlib.Path, required=True)
    parser.add_argument("--output", type=pathlib.Path, required=True)
    args = parser.parse_args()
    repository = args.repository.resolve()
    for mode in ("assets", "tracks"):
        for count in (1, 16, 256):
            directory = args.output / f"{mode}-{count}"
            directory.mkdir(parents=True, exist_ok=True)
            (directory / "Scale.csproj").write_text(project(repository))
            (directory / "Domain.cs").write_text(domain(mode, count))
            (directory / "Program.cs").write_text(program(mode, count))


if __name__ == "__main__":
    main()
