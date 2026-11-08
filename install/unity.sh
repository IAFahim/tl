#!/usr/bin/env bash
# Tl for Unity installer (Linux, macOS, Windows Git Bash). Safe to run again.
#   curl -sSL https://raw.githubusercontent.com/IAFahim/tl/main/install/unity.sh | bash -s -- <project-path>
# Makes (or updates) a Universal 3D (URP) project on the newest Unity 7 editor with com.tl.unity from its git URL,
# imports every demo sample, builds their scenes and the Tl Demos hub, runs every demo's PlayMode test and prints one line per demo.
#   TL_DEMOS=all (default) or a comma list (force,events,crowd,motion,stats); --no-tests skips the test step.
set -euo pipefail

PKG_NAME="com.tl.unity"
PKG_URL="https://github.com/IAFahim/tl.unity.git?path=/com.tl.unity#main"
PACKS="com.tl.pack.motion=https://github.com/IAFahim/tl.git?path=/packs/Motion#main com.tl.pack.stats=https://github.com/IAFahim/tl.git?path=/packs/Stats#main"
PKG_REPO="https://github.com/IAFahim/tl.unity.git"
TEST_NAME="com.unity.test-framework"
TEST_VERSION="1.6.0"
TL_HOME="${TL_HOME:-$HOME/.tl}"
GUIDE="https://github.com/IAFahim/tl.unity/blob/main/docs/USING-ECS.md"
UNITY_TIMEOUT="${TL_UNITY_TIMEOUT:-2700}"
STEPS=6
TARGET=""
NO_TESTS="${TL_NO_TESTS:-0}"
for arg in "$@"; do
  case "$arg" in
    --no-tests) NO_TESTS=1 ;;
    -*) printf 'unknown option %s (usage: bash -s -- <project-path>)\n' "$arg" >&2; exit 1 ;;
    *) TARGET="$arg" ;;
  esac
done

if [ -t 1 ]; then GREEN=$'\033[0;32m'; RED=$'\033[0;31m'; NC=$'\033[0m'; else GREEN=""; RED=""; NC=""; fi
step() { printf '%s[%s]%s %s\n' "$GREEN" "$1" "$NC" "$2"; }
say() { printf '    %s\n' "$*"; }
die() { printf '%s[tl] ERROR:%s %s\n' "$RED" "$NC" "$*" >&2; exit 1; }

cat <<'EOF'

 Tl for Unity - installer
 Unity 7 (URP) + com.tl.unity + the Tl demos, nothing else to install

EOF

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) OS=windows ;;
  Darwin*) OS=macos ;;
  *) OS=linux ;;
esac

is_unity_project() { [ -f "$1/ProjectSettings/ProjectVersion.txt" ]; }

native() {
  if [ "$OS" = windows ]; then cygpath -m "$1"; else printf '%s' "$1"; fi
}

abs() {
  local dir base
  dir="$(dirname "$1")"
  base="$(basename "$1")"
  mkdir -p "$dir"
  case "$base" in
    .|..) (cd "$1" && pwd) ;;
    *) printf '%s/%s' "$(cd "$dir" && pwd)" "$base" ;;
  esac
}

editor_roots() {
  case "$OS" in
    windows)
      local pf="${PROGRAMFILES:-C:\\Program Files}"
      printf '%s\n' "$(cygpath -u "$pf")/Unity/Hub/Editor"
      ;;
    macos) printf '%s\n' "/Applications/Unity/Hub/Editor" ;;
    *) printf '%s\n' "$HOME/Unity/Hub/Editor" ;;
  esac
}

editor_binary() {
  case "$OS" in
    windows) printf '%s/Editor/Unity.exe' "$1" ;;
    macos) printf '%s/Unity.app/Contents/MacOS/Unity' "$1" ;;
    *) printf '%s/Editor/Unity' "$1" ;;
  esac
}

find_unity7() {
  local root dir name key best=""
  while IFS= read -r root; do
    [ -d "$root" ] || continue
    for dir in "$root"/7000.*; do
      [ -x "$(editor_binary "$dir")" ] || continue
      name="$(basename "$dir")"
      key="$(printf '%s' "$name" | sed -E 's/^([0-9]+)\.([0-9]+)\.([0-9]+)([a-z])([0-9]+).*/\1 \2 \3 \4 \5/')"
      best="$best
$key|$dir"
    done
  done < <(editor_roots)
  best="$(printf '%s\n' "$best" | sed '/^$/d' | sort -k1,1n -k2,2n -k3,3n -k4,4 -k5,5n | tail -n 1 | cut -d'|' -f2-)"
  [ -n "$best" ] || return 1
  editor_binary "$best"
}

find_unity_cli() {
  local candidate
  if command -v unity >/dev/null 2>&1; then command -v unity; return 0; fi
  for candidate in \
    "$HOME/.local/bin/unity" \
    "${LOCALAPPDATA:-/nonexistent}/Unity/bin/unity.exe" \
    "${LOCALAPPDATA:-/nonexistent}/Unity/bin/unity"; do
    if [ -x "$candidate" ]; then printf '%s' "$candidate"; return 0; fi
  done
  return 1
}

resolve_unity() {
  if [ -n "${UNITY:-}" ]; then
    [ -x "$UNITY" ] || die "UNITY is set to '$UNITY' but it is not an executable file"
    return 0
  fi
  if UNITY="$(find_unity7)"; then return 0; fi
  say "no Unity 7 editor installed, installing the newest one with the Unity CLI"
  local cli
  cli="$(find_unity_cli)" || die "no Unity 7 editor and no Unity CLI. Install the CLI (Windows: irm https://unity.com/install.ps1 | iex ; Linux/macOS: curl -fsSL https://unity.com/install.sh | bash), open a new shell, then run this again. Or set UNITY to an editor binary."
  "$cli" --no-banner install 7 --yes --accept-eula </dev/null
  UNITY="$(find_unity7)" || die "Unity 7 install finished but no 7000.* editor was found under $(editor_roots)"
}

editor_holders() {
  local project="$1" lock="$1/Temp/UnityLockfile" script
  if [ "$OS" = windows ]; then
    script="$LOGS/check-editor.ps1"
    cat > "$script" <<'PS'
$p = $env:TL_CHECK_PROJECT.Replace('/', '\').TrimEnd('\')
foreach ($proc in @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue)) {
  if ($proc.CommandLine -match '(?i)-projectpath(?:\s+|=)(?:"([^"]+)"|(\S+))') {
    $v = if ($Matches[1]) { $Matches[1] } else { $Matches[2] }
    if ($v.Replace('/', '\').TrimEnd('\') -ieq $p) {
      $w = if ($proc.CommandLine -match 'AssetImportWorker') { 1 } else { 0 }
      "$w $($proc.ProcessId)"
    }
  }
}
$lock = Join-Path $p 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $lock) {
  try { $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None'); $fs.Dispose() } catch [System.IO.IOException] { "2 unknown" } catch { }
}
PS
    TL_CHECK_PROJECT="$(native "$project")" powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$(native "$script")" </dev/null 2>/dev/null | tr -d '\r' | sort -n | awk 'NR==1{print $2}'
  else
    {
      ps -eo pid=,args= 2>/dev/null | P="$project" awk '
        { pid = $1; line = $0; sub(/^ *[0-9]+ +/, "", line); split(line, t, " ")
          if (t[1] !~ /(^|\/)Unity$/) next
          i = index(tolower(line), "-projectpath ")
          if (!i) next
          rest = substr(line, i + 13); sub(/^"/, "", rest)
          p = ENVIRON["P"]
          if (index(rest, p) != 1) next
          c = substr(rest, length(p) + 1, 1)
          if (c == "" || c == " " || c == "/" || c == "\"") print (line ~ /AssetImportWorker/ ? 1 : 0), pid }'
      if [ -e "$lock" ] && command -v lsof >/dev/null 2>&1; then
        lsof -t "$lock" 2>/dev/null | awk '{ print 2, $1 }'
      fi
    } | sort -n | awk 'NR==1{print $2}'
  fi
}

guard_project_closed() {
  is_unity_project "$PROJECT" || return 0
  local holder lock="$PROJECT/Temp/UnityLockfile"
  holder="$(editor_holders "$PROJECT")"
  if [ -n "$holder" ]; then
    die "Unity Editor is open on this project (PID $holder). Close it, then run this again. Project: $PROJECT"
  fi
  if [ -e "$lock" ]; then
    say "Temp/UnityLockfile is stale (no Unity process has this project open), removing it"
    rm -f "$lock" || die "could not remove the stale $lock"
  fi
}

diagnose_log() {
  local log="$1" source="$1" hits
  [ -f "$source" ] || source="$1.stdout"
  if [ ! -f "$source" ]; then CAUSE="Unity wrote no log"; return 0; fi
  if grep -qi 'another unity instance' "$source"; then
    CAUSE="another Unity instance has this project open: close the Editor, then run this again"
  elif grep -qiE 'no valid unity (editor )?license|not licensed|license.*(expired|invalid|not (been )?activated)|activation failed|unable to activate|failed to activate' "$source"; then
    CAUSE="Unity license problem: open Unity Hub, sign in and activate a license, then run this again"
  elif [ "$(error_count "$source")" -gt 0 ]; then
    CAUSE="compile errors ($(error_count "$source") error CS)"
    grep 'error CS' "$source" | head -n 3 | sed 's/^/    /'
  elif hits="$(grep -i 'aborting batchmode' "$source" | head -n 1)" && [ -n "$hits" ]; then
    CAUSE="$hits"
  else
    CAUSE="last lines of the log"
    sed '/^[[:space:]]*$/d' "$source" | tail -n 5 | sed 's/^/    /'
  fi
}

run_unity() {
  local log="$1"
  shift
  guard_project_closed
  local rc=0
  if command -v timeout >/dev/null 2>&1; then
    timeout "$UNITY_TIMEOUT" "$UNITY" -batchmode -nographics -quit -logFile "$(native "$log")" "$@" </dev/null >"$log.stdout" 2>&1 || rc=$?
  else
    "$UNITY" -batchmode -nographics -quit -logFile "$(native "$log")" "$@" </dev/null >"$log.stdout" 2>&1 || rc=$?
  fi
  return "$rc"
}

run_unity_tests() {
  local log="$1" xml="$2"
  shift 2
  guard_project_closed
  local rc=0
  rm -f "$xml"
  if command -v timeout >/dev/null 2>&1; then
    timeout "$UNITY_TIMEOUT" "$UNITY" -batchmode -nographics -logFile "$(native "$log")" -runTests -testResults "$(native "$xml")" "$@" </dev/null >"$log.stdout" 2>&1 || rc=$?
  else
    "$UNITY" -batchmode -nographics -logFile "$(native "$log")" -runTests -testResults "$(native "$xml")" "$@" </dev/null >"$log.stdout" 2>&1 || rc=$?
  fi
  return "$rc"
}

error_count() {
  local n
  n="$(grep -c 'error CS' "$1" 2>/dev/null || true)"
  printf '%s' "${n:-0}"
}

pick_json_tool() {
  local tool
  for tool in python3 python node; do
    if command -v "$tool" >/dev/null 2>&1 && "$tool" -c "1" >/dev/null 2>&1; then
      printf '%s' "$tool"
      return 0
    fi
  done
  return 1
}

add_dependency() {
  local manifest="$1" name="${2:-$PKG_NAME}" url="${3:-$PKG_URL}" tool file
  tool="$(pick_json_tool)" || die "need python3, python or node to edit $manifest"
  file="$(native "$manifest")"
  case "$tool" in
    node)
      node -e '
        const fs = require("fs");
        const [file, name, url] = process.argv.slice(1);
        const json = JSON.parse(fs.readFileSync(file, "utf8"));
        json.dependencies = json.dependencies || {};
        if (json.dependencies[name] === url) { console.log("present"); process.exit(0); }
        json.dependencies[name] = url;
        fs.writeFileSync(file, JSON.stringify(json, null, 2) + "\n");
        console.log("added");
      ' "$file" "$name" "$url"
      ;;
    *)
      "$tool" -I -c '
import json, sys
file, name, url = sys.argv[1:4]
with open(file, encoding="utf-8") as f:
    data = json.load(f)
deps = data.setdefault("dependencies", {})
if deps.get(name) == url:
    print("present")
    sys.exit(0)
deps[name] = url
with open(file, "w", encoding="utf-8", newline="\n") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
print("added")
' "$file" "$name" "$url"
      ;;
  esac
}

drop_testable() {
  local manifest="$1" tool file
  tool="$(pick_json_tool)" || die "need python3, python or node to edit $manifest"
  file="$(native "$manifest")"
  case "$tool" in
    node)
      node -e '
        const fs = require("fs");
        const [file, name] = process.argv.slice(1);
        const json = JSON.parse(fs.readFileSync(file, "utf8"));
        const list = json.testables || [];
        if (!list.includes(name)) { console.log("absent"); process.exit(0); }
        json.testables = list.filter((x) => x !== name);
        if (json.testables.length === 0) delete json.testables;
        fs.writeFileSync(file, JSON.stringify(json, null, 2) + "\n");
        console.log("dropped");
      ' "$file" "$PKG_NAME"
      ;;
    *)
      "$tool" -I -c '
import json, sys
file, name = sys.argv[1:3]
with open(file, encoding="utf-8") as f:
    data = json.load(f)
items = data.get("testables", [])
if name not in items:
    print("absent")
    sys.exit(0)
items = [x for x in items if x != name]
if items:
    data["testables"] = items
else:
    del data["testables"]
with open(file, "w", encoding="utf-8", newline="\n") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
print("dropped")
' "$file" "$PKG_NAME"
      ;;
  esac
}

drop_lock_entry() {
  local file="$1" name="${2:-$PKG_NAME}" tool
  [ -f "$file" ] || return 0
  tool="$(pick_json_tool)" || die "need python3, python or node to edit $file"
  file="$(native "$file")"
  case "$tool" in
    node)
      node -e '
        const fs = require("fs");
        const [file, name] = process.argv.slice(1);
        const json = JSON.parse(fs.readFileSync(file, "utf8"));
        const deps = json.dependencies || {};
        const entry = deps[name];
        if (!entry) { console.log("absent"); process.exit(0); }
        delete deps[name];
        fs.writeFileSync(file, JSON.stringify(json, null, 2) + "\n");
        console.log("removed");
      ' "$file" "$name"
      ;;
    *)
      "$tool" -I -c '
import json, sys
file, name = sys.argv[1:3]
with open(file, encoding="utf-8") as f:
    data = json.load(f)
deps = data.get("dependencies", {})
entry = deps.get(name)
if entry is None:
    print("absent")
    sys.exit(0)
del deps[name]
with open(file, "w", encoding="utf-8", newline="\n") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
print("removed")
' "$file" "$name"
      ;;
  esac
}

check_prerequisites() {
  command -v git >/dev/null 2>&1 || die "git is required (Unity fetches the package with it): install git, then run this again"
  if ! GIT_TERMINAL_PROMPT=0 git ls-remote "$PKG_REPO" HEAD </dev/null >/dev/null 2>&1; then
    die "git cannot read $PKG_REPO (private or not logged in): run \`gh auth login\` and \`gh auth setup-git\` first"
  fi
  pick_json_tool >/dev/null || die "need python3, python or node to edit Packages/manifest.json"
  say "git ok, $PKG_REPO readable"
}

urp_template() {
  local data
  case "$OS" in
    macos) data="$(dirname "$(dirname "$UNITY")")/Resources" ;;
    *) data="$(dirname "$UNITY")/Data/Resources" ;;
  esac
  ls "$data"/PackageManager/ProjectTemplates/com.unity.template.urp-blank-*.tgz 2>/dev/null | sort | tail -n 1
}

has_urp() {
  grep -q '"com.unity.render-pipelines.universal"' "$1/Packages/manifest.json" 2>/dev/null
}

add_test_framework() {
  local manifest="$1" tool file
  tool="$(pick_json_tool)"
  file="$(native "$manifest")"
  case "$tool" in
    node)
      node -e '
        const fs = require("fs");
        const [file, name, version] = process.argv.slice(1);
        const json = JSON.parse(fs.readFileSync(file, "utf8"));
        json.dependencies = json.dependencies || {};
        if (json.dependencies[name]) { console.log("present"); process.exit(0); }
        json.dependencies[name] = version;
        fs.writeFileSync(file, JSON.stringify(json, null, 2) + "\n");
        console.log("added");
      ' "$file" "$TEST_NAME" "$TEST_VERSION"
      ;;
    *)
      "$tool" -I -c '
import json, sys
file, name, version = sys.argv[1:4]
with open(file, encoding="utf-8") as f:
    data = json.load(f)
deps = data.setdefault("dependencies", {})
if name in deps:
    print("present")
    sys.exit(0)
deps[name] = version
with open(file, "w", encoding="utf-8", newline="\n") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
print("added")
' "$file" "$TEST_NAME" "$TEST_VERSION"
      ;;
  esac
}

write_helper() {
  mkdir -p "$(dirname "$HELPER")"
  cat > "$HELPER" <<'CS'
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
CS
}

remove_helper() {
  rm -f "$HELPER" "$HELPER.meta"
  rm -rf "$HELPER_DIR"
  rm -f "$HELPER_DIR.meta"
}

field() {
  printf '%s' "$1" | sed -n "s/.*$2=\([^ ]*\).*/\1/p"
}

TARGET="${TARGET:-${TL_PROJECT:-}}"
if [ -z "$TARGET" ]; then
  if is_unity_project "$PWD"; then TARGET="$PWD"; else die "usage: curl -sSL https://raw.githubusercontent.com/IAFahim/tl/main/install/unity.sh | bash -s -- <project-path>"; fi
fi
PROJECT="$(abs "$TARGET")"
NAME="$(basename "$PROJECT")"
LOGS="$TL_HOME/logs"
mkdir -p "$LOGS"
START="$(date +%s)"
HELPER_DIR="$PROJECT/Assets/TlInstaller"
HELPER="$HELPER_DIR/Editor/TlInstaller.cs"

step "1/$STEPS" "prerequisites"
check_prerequisites

step "2/$STEPS" "Unity 7 editor"
resolve_unity
say "editor: $UNITY"

step "3/$STEPS" "project: $PROJECT"
if is_unity_project "$PROJECT"; then
  say "Unity project exists, adding to it"
  guard_project_closed
  if ! has_urp "$PROJECT"; then
    say "this project does not use URP: Tl runs, but Entities Graphics draws nothing (the demo cube stays invisible)."
    say "make a new project with this installer, or install Universal RP and set it in Project Settings > Graphics"
  fi
else
  TEMPLATE="$(urp_template)"
  [ -n "$TEMPLATE" ] || die "the editor at $UNITY has no Universal 3D template (com.unity.template.urp-blank-*.tgz)"
  mkdir -p "$PROJECT"
  CREATE_LOG="$LOGS/create-$NAME.log"
  say "creating a Universal 3D (URP) project from $(basename "$TEMPLATE") (first import takes minutes)"
  rc=0
  run_unity "$CREATE_LOG" -createProject "$(native "$PROJECT")" -cloneFromTemplate "$(native "$TEMPLATE")" || rc=$?
  if ! is_unity_project "$PROJECT"; then
    diagnose_log "$CREATE_LOG"
    die "project creation failed (exit $rc): $CAUSE, see $CREATE_LOG"
  fi
  has_urp "$PROJECT" || die "the new project has no URP package, see $CREATE_LOG"
fi

step "4/$STEPS" "package from git, demo samples"
MANIFEST="$PROJECT/Packages/manifest.json"
[ -f "$MANIFEST" ] || die "missing $MANIFEST"
say "manifest $PKG_NAME: $(add_dependency "$MANIFEST")"
for pack in $PACKS; do say "manifest ${pack%%=*}: $(add_dependency "$MANIFEST" "${pack%%=*}" "${pack#*=}")"; done
say "manifest $TEST_NAME: $(add_test_framework "$MANIFEST")"
if [ "$(drop_testable "$MANIFEST")" = dropped ]; then
  say "removed $PKG_NAME from testables (its own test kinds would run next to the demo)"
fi
if [ "$(drop_lock_entry "$PROJECT/Packages/packages-lock.json")" = removed ]; then
  say "$PKG_NAME: refreshing to the latest main (dropped the pinned commit from Packages/packages-lock.json)"
fi
for pack in $PACKS; do drop_lock_entry "$PROJECT/Packages/packages-lock.json" "${pack%%=*}" >/dev/null; done
write_helper
IMPORT_LOG="$LOGS/import-$NAME.log"
rc=0
run_unity "$IMPORT_LOG" -projectPath "$(native "$PROJECT")" -executeMethod TlInstaller.Import || rc=$?
ERRORS="$(error_count "$IMPORT_LOG")"
if [ "$rc" -ne 0 ] || [ "$ERRORS" -ne 0 ] || ! grep -q 'TL-INSTALL imported' "$IMPORT_LOG"; then
  if grep -qiE 'unable to read|could not read|authentication failed|repository not found' "$IMPORT_LOG" 2>/dev/null; then
    say "Unity could not fetch $PKG_NAME from git: run \`gh auth login\` and \`gh auth setup-git\` first"
  fi
  remove_helper
  diagnose_log "$IMPORT_LOG"
  die "package or sample import failed (exit $rc): $CAUSE, see $IMPORT_LOG"
fi
RESOLVED="$(ls -t "$PROJECT/Library/PackageCache" 2>/dev/null | grep "^$PKG_NAME@" | head -n 1 || true)"
IMPORTED="$(grep -o 'TL-INSTALL imported [^ ]*' "$IMPORT_LOG" | cut -d' ' -f3 | tr '+' ' ' | paste -sd, - | sed 's/,/, /g')"
say "package ${RESOLVED:-$PKG_NAME}, samples imported: $IMPORTED, $ERRORS error CS"

step "5/$STEPS" "demo scenes and the Tl Demos hub"
BUILD_LOG="$LOGS/build-$NAME.log"
rc=0
run_unity "$BUILD_LOG" -projectPath "$(native "$PROJECT")" -executeMethod TlInstaller.Build || rc=$?
ERRORS="$(error_count "$BUILD_LOG")"
SCENE="$(grep -o 'TL-INSTALL scene=[^ ]*' "$BUILD_LOG" 2>/dev/null | head -n 1 | cut -d= -f2- | tr '+' ' ' || true)"
remove_helper
if [ "$rc" -ne 0 ] || [ "$ERRORS" -ne 0 ] || [ -z "$SCENE" ]; then
  diagnose_log "$BUILD_LOG"
  die "building the demo scenes failed (exit $rc): $CAUSE, see $BUILD_LOG"
fi
say "scene: $SCENE"

step "6/$STEPS" "every demo's PlayMode test, one line per demo"
TEST_LOG="$LOGS/test-$NAME.log"
TEST_XML="$LOGS/test-$NAME.xml"
if [ "$NO_TESTS" = 1 ]; then
  say "skipped (--no-tests)"
else
  rc=0
  run_unity_tests "$TEST_LOG" "$TEST_XML" -projectPath "$(native "$PROJECT")" -testPlatform PlayMode || rc=$?
  ERRORS="$(error_count "$TEST_LOG")"
  TEST_RUN="$(grep -o '<test-run [^>]*' "$TEST_XML" 2>/dev/null | head -n 1 || true)"
  PASSED="$(printf '%s' "$TEST_RUN" | sed -n 's/.* passed="\([0-9]*\)".*/\1/p')"
  FAILED="$(printf '%s' "$TEST_RUN" | sed -n 's/.* failed="\([0-9]*\)".*/\1/p')"
  BAD=""
  report() { if [ "$2" = ok ]; then printf '    ok   %-8s %s\n' "$1" "$3"; else printf '    FAIL %-8s %s\n' "$1" "$3"; BAD="$BAD $1"; fi; }
  last() { grep -o "$1" "$TEST_LOG" 2>/dev/null | tail -n 1 || true; }
  FORCE="$(last 'TL-DEMO force .*')"
  if [ -n "$FORCE" ] || printf '%s' "$IMPORTED" | grep -q 'Force demo'; then
    X="$(field "$FORCE" ' x')"; BACK="$(field "$FORCE" back)"; PIC="$(field "$FORCE" picture)"; WARN="$(field "$FORCE" warnings)"
    v=bad; { [ "$X" = 4.0333 ] && { [ "$BACK" = 0.0000 ] || [ "$BACK" = -0.0000 ]; } && [ "$WARN" = 0 ] && [ "$PIC" != off ]; } && v=ok
    report Force "$v" "push x $X m (expected 4.0333), back to $BACK (expected 0.0000), picture $PIC, warnings $WARN (expected 0)"
    DASH="$(last 'ControlDemo result .*')"
    v=bad; [ "$(field "$DASH" endZ)" = 3.0500 ] && v=ok
    report Control "$v" "dash started at x $(field "$DASH" dashAtX), cube ends at z $(field "$DASH" endZ) (expected 3.0500)"
  fi
  EVENTS="$(last 'TL-DEMO events .*')"
  if [ -n "$EVENTS" ] || printf '%s' "$IMPORTED" | grep -q 'Events demo'; then
    S="$(field "$EVENTS" strikes)"; H="$(field "$EVENTS" health)"; U="$(field "$EVENTS" hurt)"; K="$(field "$EVENTS" sparks)"
    v=bad; { [ "$S" = 15 ] && [ "$H" = 10 ] && [ "$U" = 15 ] && [ "$K" = 15 ]; } && v=ok
    report Events "$v" "strikes $S (expected 15), dummy health $H (expected 10), hurt $U, sparks $K (expected 15)"
  fi
  MOTION="$(last 'TL-DEMO motion .*')"
  if [ -n "$MOTION" ] || printf '%s' "$IMPORTED" | grep -q 'Motion demo'; then
    N="$(field "$MOTION" samples)"; M="$(field "$MOTION" mismatches)"
    v=bad; { [ "$M" = 0 ] && [ "${N:-0}" -ge 200 ]; } && v=ok
    report Motion "$v" "$N samples, $M positions differ from the .NET pack reference (expected 0)"
  fi
  STATS="$(last 'TL-DEMO stats .*')"
  if [ -n "$STATS" ] || printf '%s' "$IMPORTED" | grep -q 'Stats demo'; then
    A="$(field "$STATS" health200)"; B="$(field "$STATS" health450)"; C="$(field "$STATS" health600)"
    v=bad; { [ "$A" = 64/20 ] && [ "$B" = 53/20 ] && [ "$C" = 72/10 ]; } && v=ok
    report Stats "$v" "health/attack $A, $B, $C at steps 200, 450, 600 (expected 64/20, 53/20, 72/10)"
  fi
  ROWS="$(grep -o 'TL-BENCH crowd .*' "$TEST_LOG" 2>/dev/null || true)"
  if [ -n "$ROWS" ] || printf '%s' "$IMPORTED" | grep -q 'Crowd demo'; then
    TEXT=""; COUNT=0; ZERO=0
    while IFS= read -r row; do
      [ -n "$row" ] || continue
      COUNT=$((COUNT + 1)); [ "$(field "$row" warmB)" = 0 ] && ZERO=$((ZERO + 1))
      TEXT="$TEXT${TEXT:+; }$(field "$row" entities) dancers $(printf '%s' "$row" | sed -n 's|.*ns/entity=\([^ ]*\).*|\1|p') ns/entity $(field "$row" warmB) B"
    done <<< "$ROWS"
    v=bad; { [ "$COUNT" = 2 ] && [ "$ZERO" = 2 ]; } && v=ok
    report Crowd "$v" "$TEXT (expected 2 rows at 0 B)"
  fi
  say "PlayMode tests: passed ${PASSED:-?}, failed ${FAILED:-?}, error CS $ERRORS"
  if [ -n "$BAD" ] || [ "${FAILED:-1}" != 0 ] || [ "$ERRORS" -ne 0 ]; then
    die "a demo did not give the expected numbers ($BAD; test exit $rc), see $TEST_LOG"
  fi
fi

ELAPSED=$(( $(date +%s) - START ))
echo
say "Unity project:  $PROJECT"
say "package:        ${RESOLVED:-$PKG_NAME}"
say "done in ${ELAPSED}s"
echo
say "open Tl Demos, press Play"
say "  1. open it:  \"$UNITY\" -projectPath \"$(native "$PROJECT")\"   (or Unity Hub > Add > Add project from disk)"
say "  2. the scene $SCENE is open; press Play and click a demo (each demo has a Tl Demos button to come back)"
say "  3. each demo folder under Assets/Samples has a README: the first 10 minutes, the Timeline setup and the code"
say "  guide: $GUIDE"