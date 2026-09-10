#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
project_copy="$(mktemp -d /tmp/tl-alpha3-unity.XXXXXX)"
report_path="${1:-/tmp/tl-alpha3-unity-results.xml}"
log_path="${TL_UNITY_LOG:-/tmp/tl-alpha3-unity-editor.log}"
cleanup() {
    if [[ "${TL_UNITY_KEEP_PROJECT:-0}" == "1" ]]; then
        printf 'Preserved Unity project: %s\n' "$project_copy"
        return
    fi
    rm -rf "$project_copy"
}
trap cleanup EXIT
cp -R "$script_dir/Project/." "$project_copy"
unity --no-banner --format json test "$project_copy" \
    --editor-version 6000.7.0a5 \
    --mode EditMode \
    --filter UnifiedTimelineProof.Tests.UnityExecutionTests \
    --output "$report_path" \
    --timeout 600 \
    -- -logFile "$log_path"
