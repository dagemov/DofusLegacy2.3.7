#!/bin/bash
set -eu

ROOT="/opt/dofus-2.0.0-build/Sunshine net11.0/Sunshine net11.0"
rm -f "$ROOT/Sunshine.WorldServer/Game/Dialogs/VirtualShopDirectoryDialog.cs"

bash /tmp/patch-csproj-shop.sh
python3 /tmp/apply-shop-server-patch.py

cd /opt/dofus-2.0.0-build/docker
DOCKER_BUILDKIT=1 docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml build sunshine
docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml up -d sunshine
sleep 15
docker logs sunshine-server 2>&1 | grep -E 'Commands Loaded|VirtualShopRegistry|READY|tienda' | tail -12
