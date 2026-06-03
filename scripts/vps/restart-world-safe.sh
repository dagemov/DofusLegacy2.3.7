#!/usr/bin/env bash
set -euo pipefail

VPS_HOST="${VPS_HOST:-174.138.35.107}"
SSH_USER="${SSH_USER:-root}"
SSH_KEY="${SSH_KEY:-}"
WORLD_NAME_HINT="${WORLD_NAME_HINT:-sunshine-server}"
TAIL_LINES="${TAIL_LINES:-50}"
CONFIRM_RESTART="${CONFIRM_RESTART:-0}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

if [[ -z "$SSH_KEY" ]]; then
  SSH_KEY="$REPO_ROOT/SSH/private_key_sebas.pem"
fi

SSH_TARGET="${SSH_USER}@${VPS_HOST}"
SSH_BASE=(ssh -i "$SSH_KEY" -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$SSH_TARGET")

mapfile -t DISCOVERY < <("${SSH_BASE[@]}" "
set -eu
if command -v docker >/dev/null 2>&1; then
  docker ps -a --format 'docker|{{.Names}}|{{.Image}}'
fi
if command -v systemctl >/dev/null 2>&1; then
  systemctl list-units --type=service --all --no-legend | awk '{print \"systemd|\" \$1 \"|service\"}'
fi
")

KIND=""
NAME=""
META=""

for line in "${DISCOVERY[@]}"; do
  [[ "$line" =~ ^(docker|systemd)\| ]] || continue
  IFS='|' read -r current_kind current_name current_meta <<<"$line"
  if [[ "$current_name" =~ [Ww]orld|[Ss]unshine ]]; then
    if [[ -z "$NAME" || "$current_name" == *"$WORLD_NAME_HINT"* ]]; then
      KIND="$current_kind"
      NAME="$current_name"
      META="$current_meta"
    fi
  fi
done

if [[ -z "$NAME" ]]; then
  echo "No se detecto ningun servicio o contenedor world/sunshine." >&2
  exit 1
fi

echo "Target detectado:"
echo "  Kind: $KIND"
echo "  Name: $NAME"
echo "  Meta: $META"

if [[ "$CONFIRM_RESTART" != "1" ]]; then
  echo "Modo seguro: no se reinicio nada. Usa CONFIRM_RESTART=1 para ejecutar el restart real."
  exit 0
fi

if [[ "$KIND" == "docker" ]]; then
  "${SSH_BASE[@]}" "docker restart '$NAME' && docker logs --tail $TAIL_LINES '$NAME'"
elif [[ "$KIND" == "systemd" ]]; then
  "${SSH_BASE[@]}" "systemctl restart '$NAME' && journalctl -u '$NAME' -n $TAIL_LINES --no-pager"
else
  echo "Tipo de runtime no soportado: $KIND" >&2
  exit 1
fi
