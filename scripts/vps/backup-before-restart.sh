#!/usr/bin/env bash
set -euo pipefail

VPS_HOST="${VPS_HOST:-174.138.35.107}"
SSH_USER="${SSH_USER:-root}"
SSH_KEY="${SSH_KEY:-}"
DB_NAME_HINT="${DB_NAME_HINT:-sunshine-db}"
REMOTE_BACKUP_DIR="${REMOTE_BACKUP_DIR:-/root/backups/sunshine}"
CONFIRM_BACKUP="${CONFIRM_BACKUP:-0}"
TABLES="${TABLES:-items accounts worlds_characters characters characters_items characters_spells characters_stats npcs npcs_items}"

if [[ -z "$SSH_KEY" ]]; then
  if [[ -f "$HOME/Downloads/keys/private_key_sebas.pem" ]]; then
    SSH_KEY="$HOME/Downloads/keys/private_key_sebas.pem"
  elif [[ -f "$HOME/Downloads/private_key_sebas.pem" ]]; then
    SSH_KEY="$HOME/Downloads/private_key_sebas.pem"
  elif [[ -n "${USERPROFILE:-}" ]]; then
    win_home="${USERPROFILE//\\//}"
    if [[ "$win_home" =~ ^([A-Za-z]):(.*)$ ]]; then
      drive="${BASH_REMATCH[1],,}"
      rest="${BASH_REMATCH[2]}"
      if [[ -f "/$drive$rest/Downloads/keys/private_key_sebas.pem" ]]; then
        SSH_KEY="/$drive$rest/Downloads/keys/private_key_sebas.pem"
      elif [[ -f "/$drive$rest/Downloads/private_key_sebas.pem" ]]; then
        SSH_KEY="/$drive$rest/Downloads/private_key_sebas.pem"
      fi
    fi
  elif command -v powershell.exe >/dev/null 2>&1; then
    win_home="$(powershell.exe -NoProfile -Command '$env:USERPROFILE' | tr -d '\r' | tail -n 1)"
    win_home="${win_home//\\//}"
    if [[ "$win_home" =~ ^([A-Za-z]):(.*)$ ]]; then
      drive="${BASH_REMATCH[1],,}"
      rest="${BASH_REMATCH[2]}"
      if [[ -f "/$drive$rest/Downloads/keys/private_key_sebas.pem" ]]; then
        SSH_KEY="/$drive$rest/Downloads/keys/private_key_sebas.pem"
      elif [[ -f "/$drive$rest/Downloads/private_key_sebas.pem" ]]; then
        SSH_KEY="/$drive$rest/Downloads/private_key_sebas.pem"
      fi
    fi
  fi
fi

if [[ -z "$SSH_KEY" || ! -f "$SSH_KEY" ]]; then
  echo "SSH key not found. Set SSH_KEY to a local non-tracked PEM file." >&2
  exit 1
fi

SSH_TARGET="${SSH_USER}@${VPS_HOST}"
SSH_BASE=(ssh -i "$SSH_KEY" -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$SSH_TARGET")

mapfile -t DISCOVERY < <("${SSH_BASE[@]}" "
set -eu
docker ps -a --format '{{.Names}}'
")

DB_CONTAINER=""
for name in "${DISCOVERY[@]}"; do
  if [[ "$name" == *"$DB_NAME_HINT"* ]]; then
    DB_CONTAINER="$name"
    break
  fi
done

if [[ -z "$DB_CONTAINER" ]]; then
  echo "No DB container matching '$DB_NAME_HINT' was detected." >&2
  exit 1
fi

echo "DB target detected:"
echo "  Container: $DB_CONTAINER"
echo "  Remote dir: $REMOTE_BACKUP_DIR"
echo "  Tables: $TABLES"

if [[ "$CONFIRM_BACKUP" != "1" ]]; then
  echo "Safe mode: no backup created. Use CONFIRM_BACKUP=1 to execute the dump."
  exit 0
fi

remote_script=$(cat <<EOF
set -euo pipefail
mkdir -p '$REMOTE_BACKUP_DIR'
stamp=\$(date -u +%Y%m%dT%H%M%SZ)
file='$REMOTE_BACKUP_DIR'/sunshine-pre-restart-\$stamp.sql
docker exec '$DB_CONTAINER' sh -lc 'exec mariadb-dump --single-transaction --quick -uroot -p"\$MYSQL_ROOT_PASSWORD" "\$MYSQL_DATABASE" $TABLES' > "\$file"
if [ ! -s "\$file" ]; then
  echo "Backup file is empty: \$file" >&2
  exit 1
fi
bytes=\$(wc -c < "\$file")
printf 'BACKUP_FILE=%s\n' "\$file"
printf 'BACKUP_BYTES=%s\n' "\$bytes"
EOF
)

"${SSH_BASE[@]}" "$remote_script"
