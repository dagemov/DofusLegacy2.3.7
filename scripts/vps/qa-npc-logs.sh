#!/usr/bin/env bash
set -euo pipefail

OUT="/opt/dofus-2.0.0-build/runtime-logs/qa-npc-$(date +%Y%m%d_%H%M%S).log"

mkdir -p /opt/dofus-2.0.0-build/runtime-logs

docker logs sunshine-server 2>&1 \
  | grep -E '\[NpcReplyRaw\]|\[NpcReply\]|\[NpcAction\]|\[JobLearn\]|\[Harvest\]|\[JobXp\]' \
  | tail -200 > "$OUT"

echo "$OUT"
