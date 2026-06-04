#!/usr/bin/env bash
set -euo pipefail

BACKUP_ID="${1:-}"
EXECUTE="${EXECUTE:-0}"
CONFIRM_RESTORE="${CONFIRM_RESTORE:-0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "$SCRIPT_DIR/../../.." && pwd)}"

[[ -n "$BACKUP_ID" ]] || { echo "Uso: restore-client.sh <BackupId> [EXECUTE=1]" >&2; exit 1; }

BACKUP_DIR="$REPO_ROOT/backups/client/$BACKUP_ID"
MANIFEST="$BACKUP_DIR/manifest.json"
[[ -f "$MANIFEST" ]] || { echo "Backup no encontrado: $BACKUP_DIR" >&2; exit 1; }

RESTORE_TARGET="$REPO_ROOT/Infrastructure/staging-client/client-restore-sandbox"
echo "Restore client (sandbox only — NO Client2.3.7 real)"
echo "  Backup: $BACKUP_DIR"
echo "  Target sandbox: $RESTORE_TARGET"
echo "  EXECUTE: $EXECUTE"
echo "  CONFIRM_RESTORE: $CONFIRM_RESTORE"

mapfile -t RELS < <(python3 - <<'PY' "$MANIFEST"
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
for f in data.get("files", []):
    print(f["relativePath"])
PY
)

for rel in "${RELS[@]}"; do
  echo "  $rel -> $RESTORE_TARGET/$rel"
done

if [[ "$EXECUTE" != "1" ]]; then
  echo "Dry-run completo. Usa EXECUTE=1 CONFIRM_RESTORE=1 para copiar al sandbox."
  exit 0
fi

[[ "$CONFIRM_RESTORE" == "1" ]] || { echo "CONFIRM_RESTORE=1 requerido." >&2; exit 1; }

for rel in "${RELS[@]}"; do
  src="$BACKUP_DIR/$rel"
  dest="$RESTORE_TARGET/$rel"
  mkdir -p "$(dirname "$dest")"
  cp "$src" "$dest"
done

echo "Restore sandbox OK: $RESTORE_TARGET"
