#!/bin/sh
set -eu
export MYSQL_PWD="${MYSQL_ROOT_PASSWORD:-change-me-root}"
mariadb -uroot <<'SQL'
CREATE USER IF NOT EXISTS 'sunshine_remote'@'%' IDENTIFIED BY 'change-me-remote';
ALTER USER 'sunshine_remote'@'%' IDENTIFIED BY 'change-me-remote';
GRANT ALL PRIVILEGES ON `sunshine`.* TO 'sunshine_remote'@'%';
FLUSH PRIVILEGES;
SHOW GRANTS FOR 'sunshine_remote'@'%';
SQL
export MYSQL_PWD='change-me-remote'
mariadb -usunshine_remote -h127.0.0.1 sunshine -e 'SELECT COUNT(*) AS account_count FROM accounts;'
