# tlbake CLI test configuration

`tests/test_tlbake_cli.py` runs the packaged `tlbake` CLI against the scenarios in
`config.json`. Every scenario is a self-contained description of inputs, CLI
options, and expected outcomes, so a new CLI behavior is proven by adding a
scenario instead of editing the runner. Run it with:

```sh
python3 -m unittest discover -s tests -p test_tlbake_cli.py
```

The runner locates `tools/Tl.Bake/bin/Release/net10.0/Tl.Bake.dll` and the
fixture assembly `tools/Tl.Bake.Tests/bin/Release/net10.0/Tl.Bake.Tests.dll`,
building either project first if the DLL is missing.

## Schema (v1)

Root object:

| field | meaning |
| --- | --- |
| `schemaVersion` | config schema version, currently `1` |
| `scenarios` | ordered scenario list; scenarios run in order and share one `$out` directory |

Scenario object:

| field | meaning |
| --- | --- |
| `name` | scenario name; becomes the unittest method name |
| `mode` | CLI mode: `bake`, `json`, `report`, `strip`, or `watch` |
| `inputs` | mode inputs (see below) |
| `options` | `assemblies` (list of `--assembly` paths), `cache` (`--cache` directory, bake only), `debounceMs` and `timeoutSeconds` (watch only) |
| `expected` | outcomes (see below) |

Inputs by mode:

| mode | inputs |
| --- | --- |
| `bake` | `json` — authoring JSON path |
| `report` | `tlb` — baked TLB path |
| `strip` | `tlb` — baked TLB path |
| `json` | none (assemblies option only) |
| `watch` | `initial` — JSON copied to the watch directory as `alpha.json`; `edits` — ordered list of file contents written over `alpha.json`, one per expected non-ready event |

Expected outcomes (all optional, all checked when present):

| field | meaning |
| --- | --- |
| `exitCode` | exact process exit code |
| `output` | `path` of a produced file plus its exact `sha256` |
| `stdoutContains` | list of substrings that must appear in stdout |
| `stderrContains` | list of substrings that must appear in stderr |
| `stdoutGolden` | file whose bytes must equal stdout exactly |
| `absentOutputs` | paths that must not exist after the run |
| `runs` | bake scenarios with `cache`: per-run `stdoutContains` for the repeated invocations |
| `events` | watch scenarios: exact ordered event kinds (`ready`, `rebuild`, `skip`, `diagnostic`); every line must also parse as JSON with `schemaVersion` `1` |

## Path tokens

| token | resolves to |
| --- | --- |
| `$configDir` | this directory (`tests/tlbake_cli`) |
| `$root` | repository root |
| `$testAssembly` | the built `Tl.Bake.Tests.dll` path |
| `$out` | per-run temporary output directory shared by all scenarios |
