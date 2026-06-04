#!/usr/bin/env bash
set -euo pipefail

BACKUP_ID="${1:-}"
EXECUTE="${EXECUTE:-0}"
CONFIRM_RESTORE="${CONFIRM_RESTORE:-0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "$SCRIPT_DIR/../../.." && pwd)}"
DB_CONTAINER_NAME="${DB_CONTAINER_NAME:-sunshine-db}"
DATABASE_NAME="${DATABASE_NAME:-sunshine}"

[[ -n "$BACKUP_ID" ]] || { echo "Uso: restore-db.sh <BackupId>" >&2; exit 1; }

BACKUP_DIR="$REPO_ROOT/backups/db/$BACKUP_ID"
DUMP_PATH="$BACKUP_DIR/sunshine.sql"
[[ -f "$DUMP_PATH" ]] || { echo "Dump no encontrado: $DUMP_PATH" >&2; exit 1; }

echo "Restore DB (solo contenedor local $DB_CONTAINER_NAME)"
echo "  Backup: $BACKUP_DIR"
echo "  EXECUTE: $EXECUTE"
echo "  CONFIRM_RESTORE: $CONFIRM_RESTORE"
echo "  Plan: docker exec -i $DB_CONTAINER_NAME mysql ... $DATABASE_NAME < sunshine.sql"

if [[ "$EXECUTE" != "1" ]]; then
  echo "Dry-run completo. Usa EXECUTE=1 CONFIRM_RESTORE=1 para restaurar."
  exit 0
fi

[[ "$CONFIRM_RESTORE" == "1" ]] || { echo "CONFIRM_RESTORE=1 requerido." >&2; exit 1; }

running="$(docker inspect -f '{{.State.Running}}' "$DB_CONTAINER_NAME" 2>/dev/null || echo missing)"
[[ "$running" == "true" ]] || { echo "Contenedor no está en ejecución." >&2; exit 1; }

ROOT_PASS="$(docker exec "$DB_CONTAINER_NAME" printenv MYSQL_ROOT_PASSWORD | tr -d '\r')"
docker exec -i "$DB_CONTAINER_NAME" mysql -uroot -p"$ROOT_PASS" "$DATABASE_NAME" <"$DUMP_PATH"
echo "Restore DB local OK."
