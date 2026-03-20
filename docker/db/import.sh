#!/bin/bash
set -e

mysql -u root -p"$MYSQL_ROOT_PASSWORD" --default-character-set=utf8mb4 "$MYSQL_DATABASE" < /sql/01_schema.sql
mysql -u root -p"$MYSQL_ROOT_PASSWORD" --default-character-set=utf8mb4 "$MYSQL_DATABASE" < /sql/02_translations.sql
