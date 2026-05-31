#!/bin/sh
set -eu

sql_escape() {
  printf "%s" "$1" | sed "s/'/''/g"
}

DB_NAME_ESCAPED="$(sql_escape "${MYSQL_DATABASE:-sunshine}")"
APP_USER_ESCAPED="$(sql_escape "${MYSQL_APP_USER:-${MYSQL_USER:-sunshine}}")"
APP_PASSWORD_ESCAPED="$(sql_escape "${MYSQL_APP_PASSWORD:-${MYSQL_PASSWORD:-change-me-app}}")"
REMOTE_USER_ESCAPED="$(sql_escape "${MYSQL_REMOTE_USER:-}")"
REMOTE_PASSWORD_ESCAPED="$(sql_escape "${MYSQL_REMOTE_PASSWORD:-}")"

export MYSQL_PWD="${MYSQL_ROOT_PASSWORD}"
mysql --protocol=socket -uroot <<SQL
CREATE USER IF NOT EXISTS '${APP_USER_ESCAPED}'@'%' IDENTIFIED BY '${APP_PASSWORD_ESCAPED}';
GRANT ALL PRIVILEGES ON \`${DB_NAME_ESCAPED}\`.* TO '${APP_USER_ESCAPED}'@'%';
SQL

if [ -n "${MYSQL_REMOTE_USER:-}" ]; then
  export MYSQL_PWD="${MYSQL_ROOT_PASSWORD}"
mysql --protocol=socket -uroot <<SQL
CREATE USER IF NOT EXISTS '${REMOTE_USER_ESCAPED}'@'%' IDENTIFIED BY '${REMOTE_PASSWORD_ESCAPED}';
GRANT ALL PRIVILEGES ON \`${DB_NAME_ESCAPED}\`.* TO '${REMOTE_USER_ESCAPED}'@'%';
SQL
fi

mysql --protocol=socket -uroot -e "FLUSH PRIVILEGES;"
