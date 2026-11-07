#!/usr/bin/env bash
# Tl for Unity installer (Linux, macOS, Windows Git Bash). Safe to run again.
#   curl -sSL https://raw.githubusercontent.com/IAFahim/tl/main/install/unity.sh | bash -s -- <project-path>
set -euo pipefail

PKG_NAME="com.tl.unity"
PKG_URL="https://github.com/IAFahim/tl.unity.git?path=/com.tl.unity#main"
PKG_REPO="https://github.com/IAFahim/tl.unity.git"
TL_REPO="https://github.com/IAFahim/tl.git"
TL_HOME="${TL_HOME:-$HOME/.tl}"
TLB_REL="tools/Tl.Bake/bin/Release/net10.0/Tl.Bake.dll"
GUIDE="https://github.com/IAFahim/tl.unity/blob/main/docs/USING-ECS.md"
UNITY_TIMEOUT="${TL_UNITY_TIMEOUT:-2700}"

if [ -t 1 ]; then GREEN=$'\033[0;32m'; RED=$'\033[0;31m'; NC=$'\033[0m'; else GREEN=""; RED=""; NC=""; fi
step() { printf '%s[%s]%s %s\n' "$GREEN" "$1" "$NC" "$2"; }
say() { printf '    %s\n' "$*"; }
die() { printf '%s[tl] ERROR:%s %s\n' "$RED" "$NC" "$*" >&2; exit 1; }

cat <<'EOF'

 Tl for Unity - installer
 Unity 7 + com.tl.unity + tlb + Game assembly, no git clone needed

EOF

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) OS=windows ;;
  Darwin*) OS=macos ;;
  *) OS=linux ;;
esac

native() {
  if [ "$OS" = windows ]; then cygpath -m "$1"; else printf '%s' "$1"; fi
}

abs() {
  local dir base
  dir="$(dirname "$1")"
  base="$(basename "$1")"
  mkdir -p "$dir"
  printf '%s/%s' "$(cd "$dir" && pwd)" "$base"
}

is_unity_project() { [ -f "$1/ProjectSettings/ProjectVersion.txt" ]; }

TARGET="${1:-${TL_PROJECT:-}}"
if [ -z "$TARGET" ]; then
  if is_unity_project "$PWD"; then TARGET="$PWD"; else die "usage: curl -sSL https://raw.githubusercontent.com/IAFahim/tl/main/install/unity.sh | bash -s -- <project-path>"; fi
fi
PROJECT="$(abs "$TARGET")"
LOGS="$TL_HOME/logs"
mkdir -p "$LOGS"
START="$(date +%s)"

dotnet_hint() {
  case "$OS" in
    windows) printf '%s' "winget install Microsoft.DotNet.SDK.10" ;;
    macos) printf '%s' "brew install --cask dotnet-sdk" ;;
    *) printf '%s' "sudo apt-get install -y dotnet-sdk-10.0   (or: curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0)" ;;
  esac
}

check_prerequisites() {
  command -v git >/dev/null 2>&1 || die "git is required: install git, then run this again"
  if ! GIT_TERMINAL_PROMPT=0 git ls-remote "$PKG_REPO" HEAD </dev/null >/dev/null 2>&1; then
    die "git cannot read $PKG_REPO (private or not logged in): run \`gh auth login\` first"
  fi
  if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
    say "the .NET 10 SDK is required to build tlb and is not installed. Install it with:"
    say "    $(dotnet_hint)"
    say "then open a new shell and run this again."
    exit 1
  fi
  say "git ok, $PKG_REPO readable, .NET $(dotnet --list-sdks | grep '^10\.' | tail -n 1 | cut -d' ' -f1)"
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

run_unity() {
  local log="$1"
  shift
  local rc=0
  if command -v timeout >/dev/null 2>&1; then
    timeout "$UNITY_TIMEOUT" "$UNITY" -batchmode -nographics -quit -logFile "$(native "$log")" "$@" </dev/null || rc=$?
  else
    "$UNITY" -batchmode -nographics -quit -logFile "$(native "$log")" "$@" </dev/null || rc=$?
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
  local manifest="$1" tool file
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
      ' "$file" "$PKG_NAME" "$PKG_URL"
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
' "$file" "$PKG_NAME" "$PKG_URL"
      ;;
  esac
}

ensure_tlb() {
  local checkout="$TL_HOME/tl" before after
  local dll="$checkout/$TLB_REL"
  if [ -d "$checkout/.git" ]; then
    before="$(git -C "$checkout" rev-parse HEAD)"
    if git -C "$checkout" fetch --depth 1 --quiet origin main </dev/null; then
      git -C "$checkout" reset --hard --quiet FETCH_HEAD
    else
      say "git fetch failed, using the existing checkout"
    fi
    after="$(git -C "$checkout" rev-parse HEAD)"
  else
    mkdir -p "$TL_HOME"
    git clone --depth 1 --quiet "$TL_REPO" "$checkout" </dev/null
    before=""
    after="$(git -C "$checkout" rev-parse HEAD)"
  fi
  if [ ! -f "$dll" ] || [ "$before" != "$after" ]; then
    say "building tlb"
    dotnet build "$checkout/tools/Tl.Bake" -c Release -nologo -v q </dev/null
  else
    say "tlb is up to date"
  fi
  [ -f "$dll" ] || die "tlb build did not produce $dll"
  TLB="$(native "$dll")"
}

write_asmdef() {
  local dir="$PROJECT/Assets/Game" file="$PROJECT/Assets/Game/Game.asmdef"
  mkdir -p "$dir"
  if [ -f "$file" ]; then say "Assets/Game/Game.asmdef exists, left as is"; return 0; fi
  cat > "$file" <<'JSON'
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
JSON
  say "wrote Assets/Game/Game.asmdef"
}

step "1/5" "prerequisites"
check_prerequisites

step "2/5" "Unity 7 editor"
resolve_unity
say "editor: $UNITY"

step "3/5" "project: $PROJECT"
if is_unity_project "$PROJECT"; then
  say "Unity project exists, adding to it"
else
  mkdir -p "$PROJECT"
  CREATE_LOG="$LOGS/create-$(basename "$PROJECT").log"
  say "creating the project in batch mode (first import takes minutes)"
  rc=0
  run_unity "$CREATE_LOG" -createProject "$(native "$PROJECT")" || rc=$?
  is_unity_project "$PROJECT" || die "project creation failed (exit $rc), see $CREATE_LOG"
fi

step "4/5" "package, tlb, Game assembly"
MANIFEST="$PROJECT/Packages/manifest.json"
[ -f "$MANIFEST" ] || die "missing $MANIFEST"
say "manifest $PKG_NAME: $(add_dependency "$MANIFEST")"
ensure_tlb
mkdir -p "$PROJECT/.tl"
printf '%s\n' "$TLB" > "$PROJECT/.tl/tlb.path"
say "tlb: $TLB"
write_asmdef

step "5/5" "compile"
COMPILE_LOG="$LOGS/compile-$(basename "$PROJECT").log"
rc=0
run_unity "$COMPILE_LOG" -projectPath "$(native "$PROJECT")" || rc=$?
ERRORS="$(error_count "$COMPILE_LOG")"
if [ "$rc" -ne 0 ] || [ "$ERRORS" -ne 0 ]; then
  grep 'error CS' "$COMPILE_LOG" | head -n 20 || true
  if grep -qiE 'unable to read|could not read|authentication failed|repository not found' "$COMPILE_LOG" 2>/dev/null; then
    say "Unity could not fetch $PKG_NAME from git: run \`gh auth login\` first"
  fi
  die "compile failed (exit $rc, $ERRORS error CS), see $COMPILE_LOG"
fi
RESOLVED="$(ls "$PROJECT/Library/PackageCache" 2>/dev/null | grep "^$PKG_NAME@" || true)"
if [ -z "$RESOLVED" ]; then
  say "$PKG_NAME is not in Library/PackageCache: run \`gh auth login\` first, then run this again"
  die "compile passed but $PKG_NAME did not resolve, see $COMPILE_LOG"
fi

ELAPSED=$(( $(date +%s) - START ))
echo
say "Unity project:  $PROJECT"
say "package:        $RESOLVED"
say "tlb:            $TLB"
say "compile:        exit $rc, error CS $ERRORS"
say "done in ${ELAPSED}s"
echo
say "next: follow $GUIDE section 3 onward (the Game assembly replaces Demo in step 1)."
say "      the guide needs access to the private repo until tl.unity is public."
say "      to use tlb in a shell: export TL_TLB=\"$TLB\""
