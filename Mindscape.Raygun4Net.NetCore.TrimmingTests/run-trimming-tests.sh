#!/usr/bin/env bash
# Publishes the trimming test app in several modes, runs each one, and checks that the trimmed payloads
# match the untrimmed payload. Regression test for https://github.com/MindscapeHQ/raygun4net/issues/570
#
# Usage: ./run-trimming-tests.sh [output-dir] [modes...]
#   modes: none partial full aot   (default: none partial full)
#   The untrimmed "none" build is the baseline, so it always runs first even when it isn't listed.
set -euo pipefail

cd "$(dirname "$0")"
OUT="${1:-$(pwd)/artifacts}"
shift || true
if [ $# -gt 0 ]; then REQUESTED=("$@"); else REQUESTED=(partial full); fi
MODES=(none)
for mode in "${REQUESTED[@]}"; do
  [ "$mode" = none ] || MODES+=("$mode")
done
TFM="${TFM:-net10.0}"
RID="${RID:-$(dotnet --info | awk '/^ *RID:/ {print $2; exit}')}"

rm -rf "$OUT"
mkdir -p "$OUT"
RUN_FAILED=0

for mode in "${MODES[@]}"; do
  case "$mode" in
    none|partial|full|aot) ;;
    *) echo "unknown mode $mode" >&2; exit 2 ;;
  esac

  echo "=== publish: $mode ($TFM, $RID) ==="
  rm -rf obj bin
  dotnet publish -c Release -r "$RID" --self-contained -p:TrimTestMode="$mode" -p:TrimTestTargetFramework="$TFM" -o "$OUT/$mode" > "$OUT/$mode.publish.log" 2>&1 \
    || { cat "$OUT/$mode.publish.log"; exit 1; }
  grep -oE "warning IL[0-9]+:.*" "$OUT/$mode.publish.log" | sed 's/ \[.*//' | sort -u > "$OUT/$mode.warnings.txt" || true
  echo "    trim/AOT warnings: $(wc -l < "$OUT/$mode.warnings.txt" | tr -d ' ')"

  status=0
  "$OUT/$mode/Mindscape.Raygun4Net.NetCore.TrimmingTests" > "$OUT/$mode.json" 2> "$OUT/$mode.stderr.txt" || status=$?
  echo "    payload bytes: $(wc -c < "$OUT/$mode.json" | tr -d ' '), exit code: $status"
  sed 's/^/    /' "$OUT/$mode.stderr.txt"
  [ "$status" -eq 0 ] || RUN_FAILED=1
done

# Compare every mode's payload *shape* (keys, not values: timestamps/machine stats differ) with the untrimmed one.
python3 - "$OUT" "${MODES[@]}" <<'PY'
import json, sys
out, modes = sys.argv[1], sys.argv[2:]

# Every payload, including the baseline, must have these. Without this check a broken baseline would pass.
REQUIRED = [
    ".OccurredOn",
    ".Details.MachineName",
    ".Details.Error.ClassName",
    ".Details.Error.Message",
    ".Details.Error.StackTrace[].MethodName",
    ".Details.Environment.OSVersion",
    ".Details.Client.Name",
    ".Details.Tags[]",
    ".Details.UserCustomData.String",
    ".Details.User.Identifier",
    ".Details.Breadcrumbs[].Message",
    ".Details.TraceContext.TraceId",
]

# Known Native AOT gaps, not trimming problems: StackFrame.GetMethod() returns null, so the stack trace, its images
# and the breadcrumb location are empty. Everything else must still match.
AOT_KNOWN_GAPS = (
    ".Details.Error.StackTrace[]",
    ".Details.Error.Images[]",
    ".Details.Breadcrumbs[].ClassName",
    ".Details.Breadcrumbs[].MethodName",
)

def shape(v, path=""):
    if isinstance(v, dict):
        for k, x in v.items():
            yield from shape(x, f"{path}.{k}")
    elif isinstance(v, list):
        # Stack frames vary in count between JIT and AOT; only compare the first element's shape.
        if v:
            yield from shape(v[0], f"{path}[]")
    else:
        yield path

def load(m):
    try:
        return json.load(open(f"{out}/{m}.json"))
    except ValueError:
        return {}

payloads = {m: load(m) for m in modes}
baseline = set(shape(payloads["none"]))
failed = False
for m, p in payloads.items():
    keys = set(shape(p))
    ignored = AOT_KNOWN_GAPS if m == "aot" else ()
    expected = {k for k in baseline | set(REQUIRED) if not k.startswith(ignored)}
    missing = sorted(expected - keys)
    skipped = len({k for k in baseline if k.startswith(ignored)} - keys) if ignored else 0
    note = f"  (ignored {skipped} known AOT stack trace gaps)" if skipped else ""
    print(f"{m:8} keys={len(keys):3}  missing={len(missing)}{note}")
    for k in missing[:15]:
        print(f"           - {k}")
    failed |= bool(missing)
sys.exit(1 if failed else 0)
PY

exit "$RUN_FAILED"
