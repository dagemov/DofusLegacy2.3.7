-- Applied manually on running VPS; init only runs on empty data dir.
-- Remote admin for Navicat (credentials from MYSQL_REMOTE_* in .env)

CREATE USER IF NOT EXISTS 'sunshine_remote'@'%' IDENTIFIED BY 'change-me-remote';
ALTER USER 'sunshine_remote'@'%' IDENTIFIED BY 'change-me-remote';
GRANT ALL PRIVILEGES ON `sunshine`.* TO 'sunshine_remote'@'%';
FLUSH PRIVILEGES;
