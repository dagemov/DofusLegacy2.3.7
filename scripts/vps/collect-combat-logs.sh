#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SSH_KEY="${SSH_KEY:-$ROOT_DIR/SSH/private_key_sebas.pem}"

pwsh -File "$ROOT_DIR/infrastructure/artifacts/combat-health/collect-vps-combat-logs.ps1" \
  -SshKey "$SSH_KEY" -RunAnalyzer "$@"
