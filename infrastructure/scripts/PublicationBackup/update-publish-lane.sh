#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "$SCRIPT_DIR/../../.." && pwd)}"
TARGET_ITEM_ID="${TARGET_ITEM_ID:-12617}"
LANE_DIR="$REPO_ROOT/Infrastructure/staging-client/publish-lane"
PACKAGE_REL="Infrastructure/staging-client/publication-package-phase3c/$TARGET_ITEM_ID"
PACKAGE_DIR="$REPO_ROOT/$PACKAGE_REL"
VALIDATION_PATH="$PACKAGE_DIR/validation-report.json"

mkdir -p "$LANE_DIR"

latest_backup_utc() {
  local root="$1"
  [[ -d "$root" ]] || return 0
  local latest
  latest="$(ls -1 "$root" 2>/dev/null | sort -r | head -n 1)" || return 0
  [[ -n "$latest" ]] || return 0
  local manifest="$root/$latest/manifest.json"
  [[ -f "$manifest" ]] || { echo "$latest"; return 0; }
  python3 - <<PY "$manifest"
import json, sys
print(json.load(open(sys.argv[1], encoding="utf-8")).get("createdAtUtc", ""))
PY
}

validation_status=""
last_validation_utc=""
if [[ -f "$VALIDATION_PATH" ]]; then
  read -r validation_status last_validation_utc < <(python3 - <<PY "$VALIDATION_PATH"
import json, sys
d=json.load(open(sys.argv[1], encoding="utf-8"))
print(d.get("ValidationStatus",""), d.get("CheckedAt",""))
PY
)
fi

client_backup="$(latest_backup_utc "$REPO_ROOT/backups/client")"
db_backup="$(latest_backup_utc "$REPO_ROOT/backups/db")"

lane_status="READY"
blocking=()
warnings=()

if [[ ! -d "$PACKAGE_DIR" ]]; then
  lane_status="NEEDS_VALIDATION"
  blocking+=("Paquete staging no encontrado en $PACKAGE_REL.")
elif [[ "$validation_status" != "READY_FOR_CONTROLLED_PUBLISH" && "$validation_status" != "VALID_STAGING_PACKAGE" ]]; then
  lane_status="NEEDS_VALIDATION"
  blocking+=("ValidationStatus actual: ${validation_status:-sin reporte}.")
fi

if [[ -z "$client_backup" ]]; then
  [[ "$lane_status" == "READY" ]] && lane_status="NEEDS_BACKUP"
  blocking+=("No existe backup cliente en backups/client/.")
fi
if [[ -z "$db_backup" ]]; then
  warnings+=("No existe backup DB en backups/db/.")
fi

if [[ ${#blocking[@]} -eq 0 && "$validation_status" =~ ^(READY_FOR_CONTROLLED_PUBLISH|VALID_STAGING_PACKAGE)$ && -n "$client_backup" ]]; then
  lane_status="READY"
elif [[ ${#blocking[@]} -gt 0 ]]; then
  if [[ "$lane_status" != "NEEDS_VALIDATION" && -z "$client_backup" ]]; then
    lane_status="NEEDS_BACKUP"
  elif [[ "$lane_status" != "NEEDS_VALIDATION" ]]; then
    lane_status="BLOCKED"
  fi
fi

python3 - <<PY
import json, datetime
blocking = $(printf '%s\n' "${blocking[@]:-}" | python3 -c 'import json,sys; print(json.dumps([l for l in sys.stdin.read().splitlines() if l]))')
warnings = $(printf '%s\n' "${warnings[@]:-}" | python3 -c 'import json,sys; print(json.dumps([l for l in sys.stdin.read().splitlines() if l]))')
state = {
  "PublishLaneStatus": "$lane_status",
  "TargetItemId": int("$TARGET_ITEM_ID"),
  "StagingPackagePath": "$PACKAGE_REL",
  "LastEvaluatedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
  "LastValidationUtc": $( [[ -n "$last_validation_utc" ]] && echo "\"$last_validation_utc\"" || echo "null" ),
  "LastValidationStatus": $( [[ -n "$validation_status" ]] && echo "\"$validation_status\"" || echo "null" ),
  "LastClientBackupUtc": $( [[ -n "$client_backup" ]] && echo "\"$client_backup\"" || echo "null" ),
  "LastDbBackupUtc": $( [[ -n "$db_backup" ]] && echo "\"$db_backup\"" || echo "null" ),
  "RequiresClientBackupBeforePublish": True,
  "ProductionPublishBlocked": True,
  "BlockingReasons": blocking,
  "Warnings": warnings,
  "NextManualSteps": [
    "Publicación real sigue bloqueada (ProductionPublishBlocked=true).",
    "Ejecutar backup-client y backup-db con CONFIRM_BACKUP=1, luego re-evaluar lane."
  ],
  "Pipeline": ["publication-package", "backup-validation", "patch-validation", "ready-to-publish"]
}
open("$LANE_DIR/lane-state.json", "w", encoding="utf-8").write(json.dumps(state, indent=2))
print("Publish lane:", lane_status)
PY
