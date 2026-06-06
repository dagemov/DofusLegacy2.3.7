#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "$SCRIPT_DIR/../../.." && pwd)}"
DB_CONTAINER_NAME="${DB_CONTAINER_NAME:-sunshine-db}"
DATABASE_NAME="${DATABASE_NAME:-sunshine}"
CONFIRM_BACKUP="${CONFIRM_BACKUP:-0}"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_ROOT="$REPO_ROOT/backups/db/$TIMESTAMP"
DUMP_PATH="$BACKUP_ROOT/sunshine.sql"

running="$(docker inspect -f '{{.State.Running}}' "$DB_CONTAINER_NAME" 2>/dev/null || echo missing)"
if [[ "$running" != "true" ]]; then
  echo "Contenedor '$DB_CONTAINER_NAME' no está en ejecución." >&2
  exit 1
fi

echo "Publication DB backup plan"
echo "  Container: $DB_CONTAINER_NAME"
echo "  Database: $DATABASE_NAME"
echo "  Output: $DUMP_PATH"
echo "  CONFIRM_BACKUP: $CONFIRM_BACKUP"

if [[ "$CONFIRM_BACKUP" != "1" ]]; then
  echo "Modo seguro: no se ejecutó mysqldump. Usa CONFIRM_BACKUP=1."
  exit 0
fi

mkdir -p "$BACKUP_ROOT"
ROOT_PASS="$(docker exec "$DB_CONTAINER_NAME" printenv MYSQL_ROOT_PASSWORD | tr -d '\r')"
[[ -n "$ROOT_PASS" ]] || { echo "MYSQL_ROOT_PASSWORD no encontrado." >&2; exit 1; }

docker exec "$DB_CONTAINER_NAME" mysqldump -uroot -p"$ROOT_PASS" --single-transaction --skip-lock-tables "$DATABASE_NAME" >"$DUMP_PATH"
[[ -s "$DUMP_PATH" ]] || { echo "Dump vacío." >&2; exit 1; }

sha="$(sha256sum "$DUMP_PATH" | awk '{print $1}')"
created_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
size_bytes="$(wc -c <"$DUMP_PATH")"
cat >"$BACKUP_ROOT/manifest.json" <<EOF
{
  "backupType": "database-sunshine",
  "createdAtUtc": "$created_at",
  "database": "$DATABASE_NAME",
  "container": "$DB_CONTAINER_NAME",
  "dumpFile": "sunshine.sql",
  "sha256": "$sha",
  "sizeBytes": $size_bytes,
  "production": false
}
EOF
echo "$sha  sunshine.sql" >"$BACKUP_ROOT/checksums.sha256"
echo "Backup OK: $BACKUP_ROOT"
REPO_ROOT="$REPO_ROOT" bash "$SCRIPT_DIR/update-publish-lane.sh"
