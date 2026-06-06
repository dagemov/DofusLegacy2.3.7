#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SSH_KEY="${SSH_KEY:-$ROOT_DIR/SSH/private_key_sebas.pem}"

if [[ "${1:-}" == "--execute" ]]; then
  CONFIRM_RESTART="${CONFIRM_RESTART:-1}" \
    pwsh -File "$ROOT_DIR/infrastructure/artifacts/combat-health/enable-vps-combat-telemetry.ps1" \
      -SshKey "$SSH_KEY"
else
  pwsh -File "$ROOT_DIR/infrastructure/artifacts/combat-health/enable-vps-combat-telemetry.ps1" \
    -SshKey "$SSH_KEY" -DryRun
fi
