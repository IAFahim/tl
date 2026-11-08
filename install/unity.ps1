$tlScriptArgs = $args

function Install-TlUnity {
    param([string]$Target)

    $ErrorActionPreference = 'Continue'
    $pkgName = 'com.tl.unity'
    $pkgUrl = 'https://github.com/IAFahim/tl.unity.git?path=/com.tl.unity#main'
    $pkgRepo = 'https://github.com/IAFahim/tl.unity.git'
    $packs = [ordered]@{ 'com.tl.pack.force' = 'https://github.com/IAFahim/tl.git?path=/packs/Force#main'; 'com.tl.pack.inventory' = 'https://github.com/IAFahim/tl.git?path=/packs/Inventory#main'; 'com.tl.pack.motion' = 'https://github.com/IAFahim/tl.git?path=/packs/Motion#main'; 'com.tl.pack.stats' = 'https://github.com/IAFahim/tl.git?path=/packs/Stats#main' }
    $testName = 'com.unity.test-framework'
    $testVersion = '1.6.0'
    $guide = 'https://github.com/IAFahim/tl.unity/blob/main/docs/USING-ECS.md'
    $tlHome = if ($env:TL_HOME) { $env:TL_HOME } else { Join-Path $HOME '.tl' }
    $timeoutMs = 2700000
    if ($env:TL_UNITY_TIMEOUT) { $timeoutMs = [int]$env:TL_UNITY_TIMEOUT * 1000 }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $start = Get-Date
    $steps = 6
    $helperSource = @'
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TlInstaller
{
    public static void Import()
    {
        var wanted = (Environment.GetEnvironmentVariable("TL_DEMOS") ?? "all").ToLowerInvariant();
        var imported = 0;
        foreach (var sample in Sample.FindByPackage("com.tl.unity", null))
        {
            if (sample.displayName.Contains("IL2CPP"))
                continue;
            var key = sample.displayName.ToLowerInvariant().Replace(" demo", "").Trim();
            if (wanted != "all" && key != "demo hub" && !wanted.Split(',').Select(w => w.Trim()).Contains(key))
                continue;
            sample.Import(Sample.ImportOptions.OverridePreviousImports);
            Debug.Log("TL-INSTALL imported " + sample.displayName.Replace(' ', '+'));
            imported++;
        }
        if (imported == 0)
            throw new InvalidOperationException("the com.tl.unity package has no demo samples");
        AssetDatabase.Refresh();
    }

    public static void Build()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(Types).ToList();
        var stubs = types.FirstOrDefault(t => t.FullName == "Tl.Unity.Editor.TlMenuStubFiles");
        if (stubs != null)
            stubs.GetMethod("Sync").Invoke(null, null);
        var scenes = new List<string>();
        foreach (var builder in types.Where(t => t.IsAbstract && t.IsSealed && t.Name.EndsWith("DemoBuilder", StringComparison.Ordinal) && t.Name != "DemoHubBuilder").OrderBy(t => t.Name, StringComparer.Ordinal))
            foreach (var name in new[] { "Build", "BuildControl" })
            {
                var method = builder.GetMethod(name, Type.EmptyTypes);
                if (method == null || method.ReturnType != typeof(string))
                    continue;
                var scene = (string)method.Invoke(null, null);
                scenes.Add(scene);
                Debug.Log("TL-INSTALL built " + scene.Replace(' ', '+'));
            }
        if (scenes.Count == 0)
            throw new InvalidOperationException("no demo builder compiled");
        var hub = types.FirstOrDefault(t => t.Name == "DemoHubBuilder");
        string open;
        if (hub != null)
            open = (string)hub.GetMethod("Build").Invoke(null, null);
        else
        {
            EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
            open = scenes[0];
        }
        EditorSceneManager.OpenScene(open);
        Debug.Log("TL-INSTALL scene=" + open.Replace(' ', '+') + " demos=" + scenes.Count);
    }

    static IEnumerable<Type> Types(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException error)
        {
            return error.Types.Where(t => t != null);
        }
    }
}
'@

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

    function Get-EditorHolder {
        $norm = $project.Replace('/', '\').TrimEnd('\')
        $hits = @()
        foreach ($proc in @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue)) {
            if ($proc.CommandLine -match '(?i)-projectpath(?:\s+|=)(?:"([^"]+)"|(\S+))') {
                $value = if ($Matches[1]) { $Matches[1] } else { $Matches[2] }
                if ($value.Replace('/', '\').TrimEnd('\') -ieq $norm) {
                    $hits += [pscustomobject]@{ ProcessId = $proc.ProcessId; Worker = [bool]($proc.CommandLine -match 'AssetImportWorker') }
                }
            }
        }
        if ($hits.Count -eq 0) { return $null }
        return (($hits | Sort-Object Worker, ProcessId | Select-Object -First 1).ProcessId)
    }

    function Test-FileHeld($path) {
        try { $stream = [System.IO.File]::Open($path, 'Open', 'ReadWrite', 'None'); $stream.Dispose(); return $false }
        catch [System.IO.IOException] { return $true }
        catch { return $false }
    }

    function Assert-ProjectClosed {
        if (-not (Test-UnityProject $project)) { return }
        $holder = Get-EditorHolder
        $lock = Join-Path $project 'Temp\UnityLockfile'
        if (-not $holder -and (Test-Path -LiteralPath $lock) -and (Test-FileHeld $lock)) { $holder = 'unknown' }
        if ($holder) { Fail "Unity Editor is open on this project (PID $holder). Close it, then run this again. Project: $project" }
        if (Test-Path -LiteralPath $lock) {
            Say 'Temp/UnityLockfile is stale (no Unity process has this project open), removing it'
            Remove-Item -LiteralPath $lock -Force -ErrorAction Stop
        }
    }

    function Get-FailureCause($log) {
        $source = $log
        if (-not (Test-Path -LiteralPath $source)) { $source = "$log.stdout" }
        if (-not (Test-Path -LiteralPath $source)) { return 'Unity wrote no log' }
        if (Select-String -LiteralPath $source -Pattern 'another unity instance' -Quiet) {
            return 'another Unity instance has this project open: close the Editor, then run this again'
        }
        if (Select-String -LiteralPath $source -Pattern 'no valid unity (editor )?license|not licensed|license.*(expired|invalid|not (been )?activated)|activation failed|unable to activate|failed to activate' -Quiet) {
            return 'Unity license problem: open Unity Hub, sign in and activate a license, then run this again'
        }
        $count = Get-ErrorCount $source
        if ($count -gt 0) {
            Select-String -LiteralPath $source -Pattern 'error CS' -SimpleMatch | Select-Object -First 3 | ForEach-Object { Say "  $($_.Line)" }
            return "compile errors ($count error CS)"
        }
        $abort = Select-String -LiteralPath $source -Pattern 'aborting batchmode' | Select-Object -First 1
        if ($abort) { return $abort.Line.Trim() }
        Get-Content -LiteralPath $source | Where-Object { $_.Trim() } | Select-Object -Last 5 | ForEach-Object { Say "  $_" }
        return 'last lines of the log'
    }

    function Remove-LockEntry {
        $lockPath = Join-Path $project 'Packages\packages-lock.json'
        if (-not (Test-Path -LiteralPath $lockPath)) { return '' }
        $lockJson = [System.IO.File]::ReadAllText($lockPath) | ConvertFrom-Json
        if (-not $lockJson.PSObject.Properties['dependencies']) { return 'absent' }
        $names = @($pkgName) + @($packs.Keys) | Where-Object { $lockJson.dependencies.PSObject.Properties[$_] }
        if (-not $names) { return 'absent' }
        foreach ($name in $names) { $lockJson.dependencies.PSObject.Properties.Remove($name) }
        $lockText = ($lockJson | ConvertTo-Json -Depth 32) -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText($lockPath, $lockText + "`n", $utf8)
        return 'removed'
    }

    function Remove-Testable {
        $manifestPath = Join-Path $project 'Packages\manifest.json'
        $manifestJson = [System.IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
        $listed = $manifestJson.PSObject.Properties['testables']
        if (-not $listed -or @($listed.Value) -notcontains $pkgName) { return 'absent' }
        $kept = @($listed.Value | Where-Object { $_ -ne $pkgName })
        if ($kept.Count -eq 0) { $manifestJson.PSObject.Properties.Remove('testables') }
        else { $manifestJson.testables = $kept }
        $manifestText = ($manifestJson | ConvertTo-Json -Depth 32) -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText($manifestPath, $manifestText + "`n", $utf8)
        return 'dropped'
    }

    function Invoke-Unity($log, [string[]]$extra) {
        Assert-ProjectClosed
        $all = @('-batchmode', '-nographics', '-quit', '-logFile', $log) + $extra
        return (Invoke-Bounded $script:unity $all $timeoutMs "$log.stdout")
    }

    function Invoke-UnityTests($log, $xml, [string[]]$extra) {
        Assert-ProjectClosed
        Remove-Item -LiteralPath $xml -Force -ErrorAction SilentlyContinue
        $all = @('-batchmode', '-nographics', '-logFile', $log, '-runTests', '-testResults', $xml) + $extra
        return (Invoke-Bounded $script:unity $all $timeoutMs "$log.stdout")
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

    function Write-Json($path, $value) {
        $text = ($value | ConvertTo-Json -Depth 32) -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText($path, $text + "`n", $utf8)
    }

    function Set-Dependency($json, $name, $value, [bool]$replace) {
        $existing = $json.dependencies.PSObject.Properties[$name]
        if ($existing -and ($existing.Value -eq $value -or -not $replace)) { return 'present' }
        $json.dependencies | Add-Member -NotePropertyName $name -NotePropertyValue $value -Force
        return 'added'
    }

    function Remove-Helper {
        if (Test-Path -LiteralPath $helperDir) { Remove-Item -LiteralPath $helperDir -Recurse -Force -ErrorAction SilentlyContinue }
        if (Test-Path -LiteralPath "$helperDir.meta") { Remove-Item -LiteralPath "$helperDir.meta" -Force -ErrorAction SilentlyContinue }
    }

    if (-not $Target) {
        if (Test-UnityProject (Get-Location).Path) { $Target = (Get-Location).Path }
        else { Fail 'no project path: set $env:TL_PROJECT = ''C:\path\MyGame'' before the irm line, or run it inside a Unity project folder' }
    }
    if (-not [System.IO.Path]::IsPathRooted($Target)) { $Target = Join-Path (Get-Location).Path $Target }
    $project = [System.IO.Path]::GetFullPath($Target).TrimEnd('\')
    $projectName = Split-Path -Leaf $project
    $logs = Join-Path $tlHome 'logs'
    New-Item -ItemType Directory -Force -Path $logs | Out-Null
    $helperDir = Join-Path $project 'Assets\TlInstaller'
    $helper = Join-Path $helperDir 'Editor\TlInstaller.cs'

    Write-Host ''
    Write-Host ' Tl for Unity - installer'
    Write-Host ' Unity 7 (URP) + com.tl.unity + the Tl demos, nothing else to install'
    Write-Host ''

    Step "1/$steps" 'prerequisites'
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Fail 'git is required (Unity fetches the package with it): winget install Git.Git, then open a new shell and run this again' }
    $oldPrompt = $env:GIT_TERMINAL_PROMPT
    $env:GIT_TERMINAL_PROMPT = '0'
    $null = & git ls-remote $pkgRepo HEAD 2>$null
    $gitRc = $LASTEXITCODE
    $env:GIT_TERMINAL_PROMPT = $oldPrompt
    if ($gitRc -ne 0) { Fail "git cannot read $pkgRepo (private or not logged in): run ``gh auth login`` and ``gh auth setup-git`` first" }
    Say "git ok, $pkgRepo readable"

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
    $manifest = Join-Path $project 'Packages\manifest.json'
    if (Test-UnityProject $project) {
        Say 'Unity project exists, adding to it'
        Assert-ProjectClosed
        if (-not (Select-String -LiteralPath $manifest -Pattern '"com.unity.render-pipelines.universal"' -Quiet)) {
            Say 'this project does not use URP: Tl runs, but Entities Graphics draws nothing (the demo cube stays invisible).'
            Say 'make a new project with this installer, or install Universal RP and set it in Project Settings > Graphics'
        }
    }
    else {
        $templates = Join-Path (Split-Path -Parent $script:unity) 'Data\Resources\PackageManager\ProjectTemplates'
        $template = Get-ChildItem -LiteralPath $templates -Filter 'com.unity.template.urp-blank-*.tgz' -ErrorAction SilentlyContinue | Sort-Object Name | Select-Object -Last 1
        if (-not $template) { Fail "the editor at $($script:unity) has no Universal 3D template (com.unity.template.urp-blank-*.tgz)" }
        New-Item -ItemType Directory -Force -Path $project | Out-Null
        $createLog = Join-Path $logs "create-$projectName.log"
        Say "creating a Universal 3D (URP) project from $($template.Name) (first import takes minutes)"
        $rc = Invoke-Unity $createLog @('-createProject', $project, '-cloneFromTemplate', $template.FullName)
        if (-not (Test-UnityProject $project)) { Fail "project creation failed (exit $rc): $(Get-FailureCause $createLog), see $createLog" }
        if (-not (Select-String -LiteralPath $manifest -Pattern '"com.unity.render-pipelines.universal"' -Quiet)) { Fail "the new project has no URP package, see $createLog" }
    }

    Step "4/$steps" 'package from git, demo samples'
    if (-not (Test-Path -LiteralPath $manifest)) { Fail "missing $manifest" }
    $json = [System.IO.File]::ReadAllText($manifest) | ConvertFrom-Json
    if (-not $json.PSObject.Properties['dependencies']) { $json | Add-Member -NotePropertyName dependencies -NotePropertyValue ([pscustomobject]@{}) }
    Say "manifest ${pkgName}: $(Set-Dependency $json $pkgName $pkgUrl $true)"
    foreach ($pack in $packs.Keys) { Say "manifest ${pack}: $(Set-Dependency $json $pack $packs[$pack] $true)" }
    Say "manifest ${testName}: $(Set-Dependency $json $testName $testVersion $false)"
    Write-Json $manifest $json
    if ((Remove-Testable) -eq 'dropped') { Say "removed $pkgName from testables (its own test kinds would run next to the demo)" }
    if ((Remove-LockEntry) -eq 'removed') { Say "${pkgName}: refreshing to the latest main (dropped the pinned commit from Packages/packages-lock.json)" }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $helper) | Out-Null
    [System.IO.File]::WriteAllText($helper, $helperSource, $utf8)
    $importLog = Join-Path $logs "import-$projectName.log"
    $rc = Invoke-Unity $importLog @('-projectPath', $project, '-executeMethod', 'TlInstaller.Import')
    $errors = Get-ErrorCount $importLog
    if ($rc -ne 0 -or $errors -ne 0 -or -not (Select-String -LiteralPath $importLog -Pattern 'TL-INSTALL imported' -SimpleMatch -Quiet)) {
        if (Select-String -LiteralPath $importLog -Pattern 'unable to read|could not read|authentication failed|repository not found' -Quiet -ErrorAction SilentlyContinue) {
            Say "Unity could not fetch $pkgName from git: run ``gh auth login`` and ``gh auth setup-git`` first"
        }
        Remove-Helper
        Fail "package or sample import failed (exit $rc): $(Get-FailureCause $importLog), see $importLog"
    }
    $resolved = Get-ChildItem -LiteralPath (Join-Path $project 'Library\PackageCache') -Directory -Filter "$pkgName@*" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    $resolvedName = if ($resolved) { $resolved.Name } else { $pkgName }
    $imported = @(Select-String -LiteralPath $importLog -Pattern 'TL-INSTALL imported (\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value.Replace('+', ' ') })
    Say "package $resolvedName, samples imported: $($imported -join ', '), $errors error CS"

    Step "5/$steps" 'demo scenes and the Tl Demos hub'
    $buildLog = Join-Path $logs "build-$projectName.log"
    $rc = Invoke-Unity $buildLog @('-projectPath', $project, '-executeMethod', 'TlInstaller.Build')
    $errors = Get-ErrorCount $buildLog
    $sceneLine = Select-String -LiteralPath $buildLog -Pattern 'TL-INSTALL scene=(\S+)' -ErrorAction SilentlyContinue | Select-Object -First 1
    Remove-Helper
    if ($rc -ne 0 -or $errors -ne 0 -or -not $sceneLine) { Fail "building the demo scenes failed (exit $rc): $(Get-FailureCause $buildLog), see $buildLog" }
    $scene = $sceneLine.Matches[0].Groups[1].Value.Replace('+', ' ')
    Say "scene: $scene"

    Step "6/$steps" 'every demo''s PlayMode test, one line per demo'
    $testLog = Join-Path $logs "test-$projectName.log"
    $testXml = Join-Path $logs "test-$projectName.xml"
    if ($env:TL_NO_TESTS -eq '1' -or $tlScriptArgs -contains '--no-tests') {
        Say 'skipped (--no-tests)'
    }
    else {
        Assert-ProjectClosed
        Remove-Item -LiteralPath $testXml -Force -ErrorAction SilentlyContinue
        $rc = Invoke-Bounded $script:unity @('-batchmode', '-logFile', $testLog, '-runTests', '-testResults', $testXml, '-projectPath', $project, '-testPlatform', 'PlayMode') $timeoutMs "$testLog.stdout"
        $errors = Get-ErrorCount $testLog
        $passed = '?'; $failed = '?'
        if (Test-Path -LiteralPath $testXml) {
            $run = Select-String -LiteralPath $testXml -Pattern '<test-run [^>]*' | Select-Object -First 1
            if ($run) { $passed = Get-Field ($run.Line -replace '"', '') 'passed'; $failed = Get-Field ($run.Line -replace '"', '') 'failed' }
        }
        function Last($pattern) {
            $hit = Select-String -LiteralPath $testLog -Pattern $pattern -ErrorAction SilentlyContinue | Select-Object -Last 1
            if ($hit) { return $hit.Matches[0].Value }
            return ''
        }
        $bad = New-Object System.Collections.ArrayList
        function Report($name, $ok, $text) {
            $mark = if ($ok) { 'ok  ' } else { 'FAIL' }
            Write-Host ("    {0} {1,-9} {2}" -f $mark, $name, $text)
            if (-not $ok) { [void]$bad.Add($name) }
        }
        $force = Last 'TL-DEMO force .*'
        if ($force -or $imported -contains 'Force demo') {
            $x = Get-Field $force 'x'; $back = Get-Field $force 'back'; $picture = Get-Field $force 'picture'; $warn = Get-Field $force 'warnings'
            Report 'Force' ($x -eq '4.0333' -and ($back -eq '0.0000' -or $back -eq '-0.0000') -and $warn -eq '0' -and $picture -ne 'off') "push x $x m (expected 4.0333), back to $back (expected 0.0000), picture $picture, warnings $warn (expected 0)"
            $dash = Last 'ControlDemo result .*'
            Report 'Control' ((Get-Field $dash 'endZ') -eq '3.0500') "dash started at x $(Get-Field $dash 'dashAtX'), cube ends at z $(Get-Field $dash 'endZ') (expected 3.0500)"
        }
        $events = Last 'TL-DEMO events .*'
        if ($events -or $imported -contains 'Events demo') {
            $strikes = Get-Field $events 'strikes'; $health = Get-Field $events 'health'; $hurt = Get-Field $events 'hurt'; $sparks = Get-Field $events 'sparks'
            Report 'Events' ($strikes -eq '15' -and $health -eq '10' -and $hurt -eq '15' -and $sparks -eq '15') "strikes $strikes (expected 15), dummy health $health (expected 10), hurt $hurt, sparks $sparks (expected 15)"
        }
        $motion = Last 'TL-DEMO motion .*'
        if ($motion -or $imported -contains 'Motion demo') {
            $samples = Get-Field $motion 'samples'; $off = Get-Field $motion 'mismatches'
            Report 'Motion' ($off -eq '0' -and $samples -and [int]$samples -ge 200) "$samples samples, $off positions differ from the .NET pack reference (expected 0)"
        }
        $stats = Last 'TL-DEMO stats .*'
        if ($stats -or $imported -contains 'Stats demo') {
            $h200 = Get-Field $stats 'health200'; $h450 = Get-Field $stats 'health450'; $h600 = Get-Field $stats 'health600'
            Report 'Stats' ($h200 -eq '64/20' -and $h450 -eq '53/20' -and $h600 -eq '72/10') "health/attack $h200, $h450, $h600 at steps 200, 450, 600 (expected 64/20, 53/20, 72/10)"
        }
        $inventory = Last 'TL-DEMO inventory .*'
        if ($inventory -or $imported -contains 'Inventory demo') {
            $bag = Get-Field $inventory 'final'; $coin = Get-Field $inventory 'coinAt'; $gem = Get-Field $inventory 'gemAt'
            Report 'Inventory' ($bag -eq 'Wood_1_Stone_3_Plank_2_Potion_1_Sword_1_Coin_10_Key_0_Gem_1' -and $coin -eq '420' -and $gem -eq '480') "bag $($bag -replace '_', ' ') at step 600, coins at tick $coin, gem at tick $gem (expected Wood 1 Stone 3 Plank 2 Potion 1 Sword 1 Coin 10 Key 0 Gem 1, 420, 480)"
        }
        $rows = @(Select-String -LiteralPath $testLog -Pattern 'TL-BENCH crowd .*' -ErrorAction SilentlyContinue | ForEach-Object { $_.Matches[0].Value })
        if ($rows.Count -gt 0 -or $imported -contains 'Crowd demo') {
            $text = ($rows | ForEach-Object { "$(Get-Field $_ 'entities') dancers $(Get-Field $_ 'ns/entity') ns/entity $(Get-Field $_ 'warmB') B" }) -join '; '
            $zero = @($rows | Where-Object { (Get-Field $_ 'warmB') -eq '0' }).Count
            Report 'Crowd' ($rows.Count -eq 2 -and $zero -eq 2) "$text (expected 2 rows at 0 B)"
        }
        Say "PlayMode tests: passed $passed, failed $failed, error CS $errors"
        if ($bad.Count -gt 0 -or $failed -ne '0' -or $errors -ne 0) { Fail "a demo did not give the expected numbers ($($bad -join ', '); test exit $rc), see $testLog" }
    }

    $elapsed = [int]((Get-Date) - $start).TotalSeconds
    Write-Host ''
    Say "Unity project:  $project"
    Say "package:        $resolvedName"
    Say "done in ${elapsed}s"
    Write-Host ''
    Say "open Tl Demos, press Play"
    Say "  1. open it:  & '$($script:unity)' -projectPath '$project'   (or Unity Hub > Add > Add project from disk)"
    Say "  2. the scene $scene is open; press Play and click a demo (each demo has a Tl Demos button to come back)"
    Say "  3. each demo folder under Assets/Samples has a README: the first 10 minutes, the Timeline setup and the code"
    Say "  guide: $guide"
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
    if ($PSCommandPath) { exit 1 }
}
