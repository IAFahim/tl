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

    function Step($n, $text) { Write-Host "[$n] $text" -ForegroundColor Green }
    function Say($text) { Write-Host "    $text" }
    function Fail($text) { throw $text }

    function Test-UnityProject($dir) {
        Test-Path -LiteralPath (Join-Path $dir 'ProjectSettings\ProjectVersion.txt')
    }

    function Invoke-Bounded($file, [string[]]$arguments, [int]$ms) {
        $quoted = ($arguments | ForEach-Object { if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } }) -join ' '
        $p = Start-Process -FilePath $file -ArgumentList $quoted -PassThru -NoNewWindow
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
        return (Invoke-Bounded $script:unity $all $timeoutMs)
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

    Step '1/5' 'prerequisites'
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

    Step '2/5' 'Unity 7 editor'
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

    Step '3/5' "project: $project"
    if (Test-UnityProject $project) { Say 'Unity project exists, adding to it' }
    else {
        New-Item -ItemType Directory -Force -Path $project | Out-Null
        $createLog = Join-Path $logs "create-$projectName.log"
        Say 'creating the project in batch mode (first import takes minutes)'
        $rc = Invoke-Unity $createLog @('-createProject', $project)
        if (-not (Test-UnityProject $project)) { Fail "project creation failed (exit $rc), see $createLog" }
    }

    Step '4/5' 'package, tlb, Game assembly'
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
        & dotnet build (Join-Path $checkout 'tools\Tl.Bake') -c Release -nologo -v q
        if ($LASTEXITCODE -ne 0) { Fail 'dotnet build of tools/Tl.Bake failed' }
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

    Step '5/5' 'compile'
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
    $cache = Join-Path $project 'Library\PackageCache'
    $resolved = $null
    if (Test-Path -LiteralPath $cache) { $resolved = Get-ChildItem -LiteralPath $cache -Directory -Filter "$pkgName@*" | Select-Object -First 1 }
    if (-not $resolved) {
        Say "$pkgName is not in Library\PackageCache: run ``gh auth login`` first, then run this again"
        Fail "compile passed but $pkgName did not resolve, see $compileLog"
    }

    $elapsed = [int]((Get-Date) - $start).TotalSeconds
    Write-Host ''
    Say "Unity project:  $project"
    Say "package:        $($resolved.Name)"
    Say "tlb:            $tlb"
    Say "compile:        exit $rc, error CS $errors"
    Say "done in ${elapsed}s"
    Write-Host ''
    Say "next: follow $guide section 3 onward (the Game assembly replaces Demo in step 1)."
    Say '      the guide needs access to the private repo until tl.unity is public.'
    Say "      to use tlb in a shell: `$env:TL_TLB = '$tlb'"
}

$tlTarget = $env:TL_PROJECT
if (-not $tlTarget -and $tlScriptArgs -and $tlScriptArgs.Count -gt 0) { $tlTarget = [string]$tlScriptArgs[0] }
try {
    Install-TlUnity -Target $tlTarget
}
catch {
    Write-Host "[tl] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $global:LASTEXITCODE = 1
    Write-Error 'tl install failed' -ErrorAction SilentlyContinue
}
