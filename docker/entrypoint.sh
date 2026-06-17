#!/bin/sh
set -eu

log() {
  printf '%s\n' "$*"
}

mkdir -p /app/logs

if [ -d /app/runtime/maps ]; then
  ln -sfn /app/runtime/maps /app/maps
fi

if [ -d /app/runtime/d2os ]; then
  ln -sfn /app/runtime/d2os /app/d2os
fi

if [ -d /app/runtime/data ]; then
  ln -sfn /app/runtime/data /app/data
fi

cat > /app/Config.xml <<EOF
# Sunshine configuration generated from environment
AuthBindIp=0.0.0.0
AuthIp=${WORLD_PUBLIC_HOST:-127.0.0.1}
AuthPort=${AUTH_PORT:-2450}
WorldBindIp=0.0.0.0
WorldIp=${WORLD_PUBLIC_HOST:-127.0.0.1}
WorldPort=${WORLD_PORT:-5557}
ProtocolVersion=${PROTOCOL_VERSION:-1375}

RateXp=${RATE_XP:-3}
RateDrop=${RATE_DROP:-1}
RateJobXp=${RATE_JOB_XP:-5}
RateMountXp=${RATE_MOUNT_XP:-1}
RateKamas=${RATE_KAMAS:-2}

Start_Kamas=${START_KAMAS:-75000000}

FightKamasLevel1Min=${FIGHT_KAMAS_LEVEL1_MIN:-250000}
FightKamasLevel1Max=${FIGHT_KAMAS_LEVEL1_MAX:-500000}
FightKamasLevel50Min=${FIGHT_KAMAS_LEVEL50_MIN:-500000}
FightKamasLevel50Max=${FIGHT_KAMAS_LEVEL50_MAX:-1000000}
FightKamasLevel100Min=${FIGHT_KAMAS_LEVEL100_MIN:-1000000}
FightKamasLevel100Max=${FIGHT_KAMAS_LEVEL100_MAX:-2000000}
FightKamasLevel150Min=${FIGHT_KAMAS_LEVEL150_MIN:-2000000}
FightKamasLevel150Max=${FIGHT_KAMAS_LEVEL150_MAX:-2500000}
FightKamasLevel190Min=${FIGHT_KAMAS_LEVEL190_MIN:-2500000}
FightKamasLevel190Max=${FIGHT_KAMAS_LEVEL190_MAX:-3000000}

AutoSaveInterval=${AUTO_SAVE_INTERVAL:-5}

MonsterTurnStartDelayMs=${MONSTER_TURN_START_DELAY_MS:-350}
MonsterTurnEndDelayMs=${MONSTER_TURN_END_DELAY_MS:-700}

CombatTelemetryEnabled=${FIGHT_TELEMETRY_ENABLED:-false}
CombatTelemetryLogDirectory=${FIGHT_TELEMETRY_LOG_DIRECTORY:-/app/logs/combat}
CombatTelemetryWriteTurnFlow=${COMBAT_TELEMETRY_WRITE_TURN_FLOW:-true}
CombatTelemetryWriteSpellCasts=${COMBAT_TELEMETRY_WRITE_SPELL_CASTS:-true}
EOF

mkdir -p "${FIGHT_TELEMETRY_LOG_DIRECTORY:-/app/logs/combat}"
mkdir -p "${FIGHT_TELEMETRY_LOG_DIRECTORY:-/app/logs/combat}/spell-casts"

cat > /app/Database.xml <<EOF
Database Sunshine

Database = ${MYSQL_DATABASE:-sunshine}
Hostname = ${MYSQL_HOST:-db}
Port = ${MYSQL_PORT:-3306}
Username = ${MYSQL_APP_USER:-sunshine}
Password = ${MYSQL_APP_PASSWORD:-change-me-app}
EOF

if [ -n "${MYSQL_APP_PASSWORD:-}" ] && [ -n "${MYSQL_APP_USER:-}" ]; then
  log "[entrypoint] Waiting for MariaDB ${MYSQL_HOST:-db}:${MYSQL_PORT:-3306}..."
  until mariadb-admin ping \
    -h"${MYSQL_HOST:-db}" \
    -P"${MYSQL_PORT:-3306}" \
    -u"${MYSQL_APP_USER}" \
    -p"${MYSQL_APP_PASSWORD}" \
    --skip-ssl \
    --silent >/dev/null 2>&1; do
    sleep 2
  done

  if mysql \
    -h"${MYSQL_HOST:-db}" \
    -P"${MYSQL_PORT:-3306}" \
    -u"root" \
    -p"${MYSQL_ROOT_PASSWORD}" \
    "${MYSQL_DATABASE:-sunshine}" \
    -e "UPDATE worlds SET Address='${WORLD_PUBLIC_HOST:-127.0.0.1}', Port=${WORLD_PORT:-5557} WHERE Id=18;" >/dev/null 2>&1; then
    log "[entrypoint] worlds.Id=18 synchronized with WORLD_PUBLIC_HOST=${WORLD_PUBLIC_HOST:-127.0.0.1}."
  else
    log "[entrypoint] Warning: worlds.Id=18 could not be updated. Check database/sunshine.sql."
  fi
fi

cd /app
exec "$@"
