#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "$SCRIPT_DIR/../../.." && pwd)}"
VPS_HOST="${VPS_HOST:-174.138.35.107}"
SSH_USER="${SSH_USER:-root}"
SSH_KEY="${SSH_KEY:-}"
REMOTE_PATH="${REMOTE_PATH:-/opt/dofus-2.0.0}"
CONFIRM_BACKUP="${CONFIRM_BACKUP:-0}"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_ROOT="$REPO_ROOT/backups/vps/$TIMESTAMP"

if [[ -z "$SSH_KEY" ]]; then
  if [[ -f "$REPO_ROOT/SSH/private_key_sebas.pem" ]]; then
    SSH_KEY="$REPO_ROOT/SSH/private_key_sebas.pem"
  fi
fi

echo "VPS inventory backup plan"
echo "  Host: $VPS_HOST"
echo "  Output: $BACKUP_ROOT"
echo "  CONFIRM_BACKUP: $CONFIRM_BACKUP"

[[ -n "$SSH_KEY" && -f "$SSH_KEY" ]] || { echo "SSH key no encontrada." >&2; exit 1; }

if [[ "$CONFIRM_BACKUP" != "1" ]]; then
  echo "Modo seguro: no se conectó por SSH. Usa CONFIRM_BACKUP=1."
  exit 0
fi

mkdir -p "$BACKUP_ROOT"
SSH_TARGET="${SSH_USER}@${VPS_HOST}"
ssh -i "$SSH_KEY" -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$SSH_TARGET" "
set -eu
echo '=== hostname ==='
hostname
echo '=== uptime ==='
uptime
echo '=== docker ps ==='
docker ps -a
echo '=== docker images ==='
docker images
echo '=== docker compose files ==='
ls -la $REMOTE_PATH/docker 2>/dev/null || true
echo '=== docker compose config (head) ==='
cd $REMOTE_PATH/docker && docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml -f docker-compose-onelauncher-api.yml -f docker-compose-website.yml config 2>/dev/null | head -n 120 || true
" >"$BACKUP_ROOT/vps-inventory.txt"

created_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
cat >"$BACKUP_ROOT/manifest.json" <<EOF
{
  "backupType": "vps-inventory",
  "createdAtUtc": "$created_at",
  "vpsHost": "$VPS_HOST",
  "remotePath": "$REMOTE_PATH",
  "inventoryFile": "vps-inventory.txt",
  "production": false
}
EOF

echo "Inventory OK: $BACKUP_ROOT"
REPO_ROOT="$REPO_ROOT" bash "$SCRIPT_DIR/update-publish-lane.sh"
