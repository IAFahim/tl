$tlScriptArgs = $args

function Install-TlUnity {
    param([string]$Target)

    $ErrorActionPreference = 'Continue'
    $pkgName = 'com.tl.unity'
    $pkgUrl = 'https://github.com/IAFahim/tl.unity.git?path=/com.tl.unity#main'
    $pkgRepo = 'https://github.com/IAFahim/tl.unity.git'
    $tlRepo = 'https://github.com/IAFahim/tl.git'
    $guide = 'https://github.com/IAFahim/tl.unity/blob/main/docs/USING-ECS.md'
    $tlbRel = 'tools/Tl.Bake/bin/Release/net10.0/Tl.Bake.dll'
    $tlHome = if ($env:TL_HOME) { $env:TL_HOME } else { Join-Path $HOME '.tl' }
    $timeoutMs = 2700000
    if ($env:TL_UNITY_TIMEOUT) { $timeoutMs = [int]$env:TL_UNITY_TIMEOUT * 1000 }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $start = Get-Date
    $demo = $false
    if ($env:TL_DEMO -and $env:TL_DEMO -notin @('0', 'false', 'no', 'off')) { $demo = $true }
    $steps = 5

    function Step($n, $text) { Write-Host "[$n] $text" -ForegroundColor Green }
    function Say($text) { Write-Host "    $text" }
    function Fail($text) { throw $text }

    function Test-UnityProject($dir) {
        Test-Path -LiteralPath (Join-Path $dir 'ProjectSettings\ProjectVersion.txt')
    }

    function Invoke-Bounded($file, [string[]]$arguments, [int]$ms, [string]$outFile = '', [string]$workDir = '') {
        $quoted = ($arguments | ForEach-Object { if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } }) -join ' '
        $params = @{ FilePath = $file; ArgumentList = $quoted; PassThru = $true; NoNewWindow = $true }
        if ($outFile) { $params.RedirectStandardOutput = $outFile; $params.RedirectStandardError = "$outFile.err" }
        if ($workDir) { $params.WorkingDirectory = $workDir }
        $p = Start-Process @params
        $null = $p.Handle
        if (-not $p.WaitForExit($ms)) {
            try { $p.Kill() } catch { }
            Fail "timed out after $([int]($ms / 1000)) s: $file"
        }
        $p.WaitForExit()
        return [int]$p.ExitCode
    }

    function Invoke-Unity($log, [string[]]$extra) {
        $all = @('-batchmode', '-nographics', '-quit', '-logFile', $log) + $extra
        return (Invoke-Bounded $script:unity $all $timeoutMs "$log.stdout")
    }

    function Invoke-UnityTests($log, $xml, [string[]]$extra) {
        Remove-Item -LiteralPath $xml -Force -ErrorAction SilentlyContinue
        $all = @('-batchmode', '-nographics', '-logFile', $log, '-runTests', '-testResults', $xml) + $extra
        return (Invoke-Bounded $script:unity $all $timeoutMs "$log.stdout")
    }

    function Invoke-Tlb($name, [string[]]$arguments) {
        $out = Join-Path $logs "demo-$name-$projectName.log"
        $rc = Invoke-Bounded 'dotnet' (@($tlb) + $arguments) 300000 $out $project
        if ($rc -ne 0) {
            if (Test-Path -LiteralPath $out) { Get-Content -LiteralPath $out -Tail 5 | ForEach-Object { Say $_ } }
            Fail "tlb $name failed (exit $rc), see $out"
        }
    }

    function Get-ErrorCount($log) {
        if (Test-Path -LiteralPath $log) { return @(Select-String -LiteralPath $log -Pattern 'error CS' -SimpleMatch).Count }
        return 0
    }

    function Get-Field($text, $name) {
        $m = [regex]::Match($text, "$name=(\S+)")
        if ($m.Success) { return $m.Groups[1].Value }
        return ''
    }

    function Get-VersionKey($name) {
        if ($name -match '^(\d+)\.(\d+)\.(\d+)([a-z])(\d+)') {
            return ('{0:D6}.{1:D4}.{2:D4}.{3}.{4:D4}' -f [int]$Matches[1], [int]$Matches[2], [int]$Matches[3], $Matches[4], [int]$Matches[5])
        }
        return ''
    }

    function Find-Unity7 {
        $roots = @()
        if ($env:ProgramFiles) { $roots += (Join-Path $env:ProgramFiles 'Unity\Hub\Editor') }
        $found = @()
        foreach ($root in $roots) {
            if (-not (Test-Path -LiteralPath $root)) { continue }
            foreach ($dir in (Get-ChildItem -LiteralPath $root -Directory -Filter '7000.*')) {
                $exe = Join-Path $dir.FullName 'Editor\Unity.exe'
                $key = Get-VersionKey $dir.Name
                if ((Test-Path -LiteralPath $exe) -and $key) { $found += [pscustomobject]@{ Key = $key; Exe = $exe } }
            }
        }
        if ($found.Count -eq 0) { return $null }
        return ($found | Sort-Object Key | Select-Object -Last 1).Exe
    }

    function Find-UnityCli {
        $cmd = Get-Command unity -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($cmd) { return $cmd.Source }
        if ($env:LOCALAPPDATA) {
            $candidate = Join-Path $env:LOCALAPPDATA 'Unity\bin\unity.exe'
            if (Test-Path -LiteralPath $candidate) { return $candidate }
        }
        return $null
    }

    function Get-DotnetSdk10 {
        $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $dotnet) { return $null }
        $line = & $dotnet.Source --list-sdks | Where-Object { $_ -match '^10\.' } | Select-Object -Last 1
        if ($line) { return ($line -split ' ')[0] }
        return $null
    }

    foreach ($a in $tlScriptArgs) { if ("$a" -eq '--demo') { $demo = $true } }
    if ($demo) { $steps = 10 }
    if (-not $Target) {
        if (Test-UnityProject (Get-Location).Path) { $Target = (Get-Location).Path }
        else { Fail 'no project path: set $env:TL_PROJECT = ''C:\path\MyGame'' before the iwr line, or run it inside a Unity project folder' }
    }
    if (-not [System.IO.Path]::IsPathRooted($Target)) { $Target = Join-Path (Get-Location).Path $Target }
    $project = [System.IO.Path]::GetFullPath($Target).TrimEnd('\')
    $projectName = Split-Path -Leaf $project
    $logs = Join-Path $tlHome 'logs'
    New-Item -ItemType Directory -Force -Path $logs | Out-Null

    Write-Host ''
    Write-Host ' Tl for Unity - installer'
    Write-Host ' Unity 7 + com.tl.unity + tlb + Game assembly, no git clone needed'
    Write-Host ''

    Step "1/$steps" 'prerequisites'
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Fail 'git is required: winget install Git.Git, then open a new shell and run this again' }
    $oldPrompt = $env:GIT_TERMINAL_PROMPT
    $env:GIT_TERMINAL_PROMPT = '0'
    $null = & git ls-remote $pkgRepo HEAD 2>$null
    $gitRc = $LASTEXITCODE
    $env:GIT_TERMINAL_PROMPT = $oldPrompt
    if ($gitRc -ne 0) { Fail "git cannot read $pkgRepo (private or not logged in): run ``gh auth login`` first" }
    $sdk = Get-DotnetSdk10
    if (-not $sdk) {
        Say 'the .NET 10 SDK is required to build tlb and is not installed. Install it with:'
        Say '    winget install Microsoft.DotNet.SDK.10'
        Say 'then open a new shell and run this again.'
        Fail 'missing .NET 10 SDK'
    }
    Say "git ok, $pkgRepo readable, .NET $sdk"

    Step "2/$steps" 'Unity 7 editor'
    $script:unity = $env:UNITY
    if ($script:unity) {
        if (-not (Test-Path -LiteralPath $script:unity)) { Fail "UNITY is set to '$($script:unity)' but that file does not exist" }
    }
    else {
        $script:unity = Find-Unity7
        if (-not $script:unity) {
            Say 'no Unity 7 editor installed, installing the newest one with the Unity CLI'
            $cli = Find-UnityCli
            if (-not $cli) { Fail 'no Unity 7 editor and no Unity CLI. Install the CLI with: irm https://unity.com/install.ps1 | iex   then open a new shell and run this again. Or set $env:UNITY to an editor Unity.exe.' }
            $rc = Invoke-Bounded $cli @('--no-banner', 'install', '7', '--yes', '--accept-eula') 7200000
            $script:unity = Find-Unity7
            if (-not $script:unity) { Fail "Unity 7 install finished (exit $rc) but no 7000.* editor was found under $env:ProgramFiles\Unity\Hub\Editor" }
        }
    }
    Say "editor: $($script:unity)"

    Step "3/$steps" "project: $project"
    if (Test-UnityProject $project) { Say 'Unity project exists, adding to it' }
    else {
        New-Item -ItemType Directory -Force -Path $project | Out-Null
        $createLog = Join-Path $logs "create-$projectName.log"
        Say 'creating the project in batch mode (first import takes minutes)'
        $rc = Invoke-Unity $createLog @('-createProject', $project)
        if (-not (Test-UnityProject $project)) { Fail "project creation failed (exit $rc), see $createLog" }
    }

    Step "4/$steps" 'package, tlb, Game assembly'
    $manifest = Join-Path $project 'Packages\manifest.json'
    if (-not (Test-Path -LiteralPath $manifest)) { Fail "missing $manifest" }
    $json = [System.IO.File]::ReadAllText($manifest) | ConvertFrom-Json
    if (-not $json.PSObject.Properties['dependencies']) { $json | Add-Member -NotePropertyName dependencies -NotePropertyValue ([pscustomobject]@{}) }
    $existing = $json.dependencies.PSObject.Properties[$pkgName]
    if ($existing -and $existing.Value -eq $pkgUrl) { Say "manifest ${pkgName}: present" }
    else {
        $json.dependencies | Add-Member -NotePropertyName $pkgName -NotePropertyValue $pkgUrl -Force
        $text = ($json | ConvertTo-Json -Depth 32) -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText($manifest, $text + "`n", $utf8)
        Say "manifest ${pkgName}: added"
    }

    $checkout = Join-Path $tlHome 'tl'
    $dll = Join-Path $checkout ($tlbRel -replace '/', '\')
    $before = ''
    if (Test-Path -LiteralPath (Join-Path $checkout '.git')) {
        $before = (& git -C $checkout rev-parse HEAD)
        & git -C $checkout fetch --depth 1 --quiet origin main
        if ($LASTEXITCODE -eq 0) { & git -C $checkout reset --hard --quiet FETCH_HEAD }
        else { Say 'git fetch failed, using the existing checkout' }
    }
    else {
        New-Item -ItemType Directory -Force -Path $tlHome | Out-Null
        & git clone --depth 1 --quiet $tlRepo $checkout
        if ($LASTEXITCODE -ne 0) { Fail "git clone $tlRepo failed" }
    }
    $after = (& git -C $checkout rev-parse HEAD)
    if ((-not (Test-Path -LiteralPath $dll)) -or ($before -ne $after)) {
        Say 'building tlb'
        $buildLog = Join-Path $logs 'tlb-build.log'
        $buildRc = Invoke-Bounded 'dotnet' @('build', (Join-Path $checkout 'tools\Tl.Bake'), '-c', 'Release', '-nologo', '-v', 'q') 900000 $buildLog
        if ($buildRc -ne 0) {
            if (Test-Path -LiteralPath $buildLog) { Get-Content -LiteralPath $buildLog -Tail 20 | ForEach-Object { Say $_ } }
            Fail "dotnet build of tools/Tl.Bake failed (exit $buildRc), see $buildLog"
        }
    }
    else { Say 'tlb is up to date' }
    if (-not (Test-Path -LiteralPath $dll)) { Fail "tlb build did not produce $dll" }
    $tlb = $dll -replace '\\', '/'
    New-Item -ItemType Directory -Force -Path (Join-Path $project '.tl') | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $project '.tl\tlb.path'), $tlb + "`n", $utf8)
    Say "tlb: $tlb"

    $asmdef = Join-Path $project 'Assets\Game\Game.asmdef'
    if (Test-Path -LiteralPath $asmdef) { Say 'Assets/Game/Game.asmdef exists, left as is' }
    else {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $asmdef) | Out-Null
        $asmText = @'
{
  "name": "Game",
  "rootNamespace": "Game",
  "references": [
    "Tl.Core",
    "Tl.Unity",
    "Tl.Unity.Authoring",
    "Tl.Unity.Generate",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Entities",
    "Unity.Entities.Hybrid",
    "Unity.Mathematics"
  ],
  "allowUnsafeCode": true,
  "autoReferenced": true
}
'@
        [System.IO.File]::WriteAllText($asmdef, ($asmText -replace "`r`n", "`n") + "`n", $utf8)
        Say 'wrote Assets/Game/Game.asmdef'
    }

    Step "5/$steps" 'compile'
    $compileLog = Join-Path $logs "compile-$projectName.log"
    $rc = Invoke-Unity $compileLog @('-projectPath', $project)
    $errors = 0
    if (Test-Path -LiteralPath $compileLog) { $errors = @(Select-String -LiteralPath $compileLog -Pattern 'error CS' -SimpleMatch).Count }
    if ($rc -ne 0 -or $errors -ne 0) {
        Select-String -LiteralPath $compileLog -Pattern 'error CS' -SimpleMatch | Select-Object -First 20 | ForEach-Object { Say $_.Line }
        if (Select-String -LiteralPath $compileLog -Pattern 'unable to read|could not read|authentication failed|repository not found' -Quiet) {
            Say "Unity could not fetch $pkgName from git: run ``gh auth login`` first"
        }
        Fail "compile failed (exit $rc, $errors error CS), see $compileLog"
    }
    $compileRc = $rc
    $compileErrors = $errors
    $cache = Join-Path $project 'Library\PackageCache'
    $resolved = $null
    if (Test-Path -LiteralPath $cache) { $resolved = Get-ChildItem -LiteralPath $cache -Directory -Filter "$pkgName@*" | Select-Object -First 1 }
    if (-not $resolved) {
        Say "$pkgName is not in Library\PackageCache: run ``gh auth login`` first, then run this again"
        Fail "compile passed but $pkgName did not resolve, see $compileLog"
    }

    $demoRel = 'Assets/EcsDemo'
    $demoRoot = Join-Path $project 'Assets\EcsDemo'
    if ($demo) {
        Step "6/$steps" "demo: copy the sample into $demoRel"
        $sample = Join-Path $cache "$($resolved.Name)\Samples~\EcsDemo"
        if (Test-Path -LiteralPath $demoRoot) { Say "$demoRel exists, left as is (delete the folder to get the sample again)" }
        else {
            if (-not (Test-Path -LiteralPath $sample)) { Fail "$($resolved.Name) has no Samples~/EcsDemo: this package version is too old for the demo" }
            New-Item -ItemType Directory -Force -Path (Join-Path $project 'Assets') | Out-Null
            Copy-Item -LiteralPath $sample -Destination $demoRoot -Recurse
            Say "copied Samples~/EcsDemo from $($resolved.Name)"
        }

        Step "7/$steps" 'demo: compile, Timeline Library asset, staged assemblies'
        $env:TL_TLB = $tlb
        $prepLog = Join-Path $logs "demo-prepare-$projectName.log"
        $rc = Invoke-Unity $prepLog @('-projectPath', $project, '-executeMethod', 'EcsDemoSetup.Prepare')
        $demoErrors = Get-ErrorCount $prepLog
        $prepared = (Test-Path -LiteralPath $prepLog) -and (Select-String -LiteralPath $prepLog -Pattern 'EcsDemo prepared' -SimpleMatch -Quiet)
        if ($rc -ne 0 -or $demoErrors -ne 0 -or -not $prepared) {
            Select-String -LiteralPath $prepLog -Pattern 'error CS' -SimpleMatch | Select-Object -First 20 | ForEach-Object { Say $_.Line }
            Fail "demo prepare failed (exit $rc, $demoErrors error CS), see $prepLog"
        }
        Say "compiled with $demoErrors error CS, library asset and stage ready"

        Step "8/$steps" 'demo: bake capacities (tlb kinds) and the library (tlb library)'
        Invoke-Tlb 'kinds' @('kinds', '--library', "$demoRel/Timelines", '--namespace', 'Demo', '--out', "$demoRel/Kinds.g.cs", '--assembly', '.tl/stage/Demo.dll', '--diagnostics', 'json')
        Invoke-Tlb 'library' @('library', "$demoRel/Timelines", '--assembly', '.tl/stage/Demo.dll', '--out', "$demoRel/Timelines/game.tlib", '--ids', "$demoRel/Timelines/Timelines.g.cs", '--kinds', "$demoRel/Kinds.g.cs", '--namespace', 'Demo')
        $tlibPath = Join-Path $demoRoot 'Timelines\game.tlib'
        $bytesPath = Join-Path $demoRoot 'Timelines\game.bytes'
        Copy-Item -LiteralPath $tlibPath -Destination $bytesPath -Force
        $caps = (Select-String -LiteralPath (Join-Path $demoRoot 'Kinds.g.cs') -Pattern 'const int (\w+) = (\d+)' | ForEach-Object { "$($_.Matches[0].Groups[1].Value) $($_.Matches[0].Groups[2].Value)" }) -join ', '
        Say "capacities: $caps"
        Say "library: $demoRel/Timelines/game.bytes ($((Get-Item -LiteralPath $bytesPath).Length) bytes)"

        Step "9/$steps" 'demo: scene and SubScene'
        $sceneLog = Join-Path $logs "demo-scene-$projectName.log"
        $rc = Invoke-Unity $sceneLog @('-projectPath', $project, '-executeMethod', 'EcsDemoSetup.Build')
        $demoErrors = Get-ErrorCount $sceneLog
        $built = (Test-Path -LiteralPath $sceneLog) -and (Select-String -LiteralPath $sceneLog -Pattern 'EcsDemo scene' -SimpleMatch -Quiet)
        if ($rc -ne 0 -or $demoErrors -ne 0 -or -not $built) {
            Select-String -LiteralPath $sceneLog -Pattern 'error CS' -SimpleMatch | Select-Object -First 20 | ForEach-Object { Say $_.Line }
            Fail "demo scene failed (exit $rc, $demoErrors error CS), see $sceneLog"
        }
        Say "wrote $demoRel/Demo.unity with the SubScene DemoContent"

        Step "10/$steps" 'demo: PlayMode test (the scene runs in the default world)'
        $testLog = Join-Path $logs "demo-test-$projectName.log"
        $testXml = Join-Path $logs "demo-test-$projectName.xml"
        $rc = Invoke-UnityTests $testLog $testXml @('-projectPath', $project, '-testPlatform', 'PlayMode')
        $demoErrors = Get-ErrorCount $testLog
        $hit = $null
        if (Test-Path -LiteralPath $testLog) { $hit = Select-String -LiteralPath $testLog -Pattern 'EcsDemo result (.*)$' | Select-Object -Last 1 }
        if (-not $hit) { Fail "the demo test printed no result (exit $rc, $demoErrors error CS), see $testLog" }
        $text = $hit.Matches[0].Groups[1].Value
        $passed = ''
        $failed = ''
        if (Test-Path -LiteralPath $testXml) {
            $run = ([xml](Get-Content -LiteralPath $testXml -Raw)).'test-run'
            $passed = [string]$run.passed
            $failed = [string]$run.failed
        }
        $w = Get-Field $text 'walkerHealth'; $d = Get-Field $text 'dummyHealth'
        $c = Get-Field $text 'cues'; $l = Get-Field $text 'lastCue'
        $x30 = Get-Field $text 'x30'; $x50 = Get-Field $text 'x50'
        $dp = Get-Field $text 'droppedPulses'; $ds = Get-Field $text 'droppedStarts'
        Write-Host ''
        Say 'demo result'
        Say "  Walker health  $w   (guide: 58)"
        Say "  Dummy health   $d   (guide: 93)"
        Say "  Cues           $c, last cue $l   (guide: 2, last 2)"
        Say "  X at step 30   $x30"
        Say "  X at step 50   $x50   (guide: equal, ten steps back undo ten steps forward)"
        Say "  Dropped        $dp pulses, $ds starts   (guide: 0 and 0)"
        Say "  PlayMode test  passed $passed, failed $failed, error CS $demoErrors"
        $good = ($w -eq '58') -and ($d -eq '93') -and ($c -eq '2') -and ($l -eq '2') -and ($x30 -ne '') -and ($x30 -eq $x50) -and ($dp -eq '0') -and ($ds -eq '0') -and ($passed -eq '1') -and ($failed -eq '0') -and ($demoErrors -eq 0)
        if (-not $good) { Fail "the demo numbers are not the guide's numbers (test exit $rc), see $testLog" }
        Say '  all numbers match the guide'
    }
    $elapsed = [int]((Get-Date) - $start).TotalSeconds
    Write-Host ''
    Say "Unity project:  $project"
    Say "package:        $($resolved.Name)"
    Say "tlb:            $tlb"
    Say "compile:        exit $compileRc, error CS $compileErrors"
    Say "done in ${elapsed}s"
    Write-Host ''
    if ($demo) {
        Say 'next: open the project and press Play'
        Say "  1. open it:  & '$($script:unity)' -projectPath '$project'   (or Unity Hub > Add > Add project from disk)"
        Say "  2. double-click $demoRel/Demo.unity in the Project window, press Play"
        Say '  3. Window > Entities > Hierarchy: Walker and Dummy in the runtime world; Window > General > Test Runner > PlayMode > Run All repeats the numbers above'
        Say "  guide: $guide"
    }
    else {
        Say "next: `$env:TL_DEMO = 1 before the irm line adds and runs the ECS demo, or follow $guide section 3 onward"
        Say '      (the Game assembly replaces Demo in step 1).'
    }
    Say "tlb in a shell: `$env:TL_TLB = '$tlb'"
}

$tlTarget = $env:TL_PROJECT
if (-not $tlTarget) { $tlTarget = @($tlScriptArgs | ForEach-Object { [string]$_ } | Where-Object { $_ -and -not $_.StartsWith('--') })[0] }
try {
    Install-TlUnity -Target $tlTarget
}
catch {
    Write-Host "[tl] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $global:LASTEXITCODE = 1
    Write-Error 'tl install failed' -ErrorAction SilentlyContinue
}
