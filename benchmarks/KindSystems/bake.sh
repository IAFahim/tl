#!/usr/bin/env bash
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
scratch="$(mktemp -d)"
(cd "$here" && python3 gen.py)
dotnet build "$here/KindSystems.csproj" -c Release -p:NuGetAudit=false >/dev/null
dotnet build "$root/tools/Tl.Bake/Tl.Bake.csproj" -c Release -p:NuGetAudit=false >/dev/null
mkdir -p "$here/libs"
for lib in shapes many pairs; do
  dotnet "$root/tools/Tl.Bake/bin/Release/net10.0/Tl.Bake.dll" library "$here/lib-$lib" --assembly "$here/bin/Release/net10.0/KindSystems.dll" --namespace KindBench --out "$here/libs/$lib.tlib" --ids "$scratch/ids-$lib.cs" --kinds "$scratch/kinds-$lib.cs"
done
