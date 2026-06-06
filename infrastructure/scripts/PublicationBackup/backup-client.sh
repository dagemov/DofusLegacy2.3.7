#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-}"
CLIENT_ROOT="${CLIENT_ROOT:-}"
CONFIRM_BACKUP="${CONFIRM_BACKUP:-0}"

resolve_repo_root() {
  local dir="$SCRIPT_DIR"
  while [[ -n "$dir" && "$dir" != "/" ]]; do
    if [[ -d "$dir/Angular-tools/Admin" && -d "$dir/docs" ]]; then
      printf '%s' "$(cd "$dir/.." && pwd)/.."
      return
    fi
    dir="$(dirname "$dir")"
  done
  local candidate="$SCRIPT_DIR"
  while [[ -n "$candidate" && "$candidate" != "/" ]]; do
    if [[ -d "$candidate/Angular-tools/Admin" && -d "$candidate/docs" ]]; then
      printf '%s' "$candidate"
      return
    fi
    candidate="$(dirname "$candidate")"
  done
  echo "No se pudo resolver la raíz del repo." >&2
  exit 1
}

if [[ -z "$REPO_ROOT" ]]; then
  REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
  if [[ ! -d "$REPO_ROOT/Angular-tools/Admin" ]]; then
    REPO_ROOT="$(resolve_repo_root)"
  fi
fi

CLIENT_ROOT="${CLIENT_ROOT:-$REPO_ROOT/Client2.3.7}"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_ROOT="$REPO_ROOT/backups/client/$TIMESTAMP"

declare -a RELATIVE_PATHS=(
  "data/common/Items.d2o"
  "data/common/ItemSets.d2o"
  "data/common/ItemTypes.d2o"
  "data/i18n/i18n_es.d2i"
  "data/i18n/i18n_en.d2i"
)

echo "Publication client backup plan"
echo "  RepoRoot: $REPO_ROOT"
echo "  ClientRoot: $CLIENT_ROOT"
echo "  Output: $BACKUP_ROOT"
echo "  CONFIRM_BACKUP: $CONFIRM_BACKUP"

for rel in "${RELATIVE_PATHS[@]}"; do
  src="$CLIENT_ROOT/$rel"
  [[ -f "$src" ]] || { echo "Missing: $src" >&2; exit 1; }
  echo "  - $rel ($(wc -c <"$src") bytes)"
done

if [[ "$CONFIRM_BACKUP" != "1" ]]; then
  echo "Modo seguro: no se copió nada. Usa CONFIRM_BACKUP=1 para ejecutar."
  exit 0
fi

mkdir -p "$BACKUP_ROOT"
CHECKSUM_FILE="$BACKUP_ROOT/checksums.sha256"
echo "# SHA-256 — client publication backup" >"$CHECKSUM_FILE"

FILES_JSON="["
first=1
for rel in "${RELATIVE_PATHS[@]}"; do
  src="$CLIENT_ROOT/$rel"
  dest="$BACKUP_ROOT/$rel"
  mkdir -p "$(dirname "$dest")"
  cp "$src" "$dest"
  sha="$(sha256sum "$dest" | awk '{print $1}')"
  echo "$sha  $rel" >>"$CHECKSUM_FILE"
  if [[ $first -eq 0 ]]; then FILES_JSON+=","; fi
  first=0
  FILES_JSON+="{\"relativePath\":\"$rel\",\"sha256\":\"$sha\",\"sizeBytes\":$(wc -c <"$dest")}"
done
FILES_JSON+="]"

CREATED_AT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
cat >"$BACKUP_ROOT/manifest.json" <<EOF
{
  "backupType": "client-publication",
  "createdAtUtc": "$CREATED_AT",
  "clientRootPath": "$CLIENT_ROOT",
  "backupPath": "$BACKUP_ROOT",
  "files": $FILES_JSON,
  "phase": "4-controlled-lane",
  "production": false
}
EOF

echo "Backup OK: $BACKUP_ROOT"
SKIP_LANE_UPDATE="${SKIP_LANE_UPDATE:-0}"
if [[ "$SKIP_LANE_UPDATE" != "1" ]]; then
  REPO_ROOT="$REPO_ROOT" bash "$SCRIPT_DIR/update-publish-lane.sh"
fi
