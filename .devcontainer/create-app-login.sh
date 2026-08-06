#!/usr/bin/env bash
# Creates the low-privilege `timetracker_app` login the running app connects as (mirrors production's
# timetracker-zak: db_datareader + db_datawriter only). Split into two modes because the database
# doesn't exist until migrations create it: the server-level login can be created before migrations
# run, but the database-level user + role grants must wait until after. See ADR-035.
set -euo pipefail

MODE="${1:?Usage: create-app-login.sh <login|grant>}"

if [ "$MODE" = "login" ]; then
  docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C \
    -v AppDbPassword="$APP_DB_PASSWORD" \
    -Q "IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'timetracker_app') CREATE LOGIN [timetracker_app] WITH PASSWORD = N'\$(AppDbPassword)';"
elif [ "$MODE" = "grant" ]; then
  docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -d TimeTrackerDb \
    -Q "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'timetracker_app') BEGIN CREATE USER [timetracker_app] FOR LOGIN [timetracker_app]; ALTER ROLE db_datareader ADD MEMBER [timetracker_app]; ALTER ROLE db_datawriter ADD MEMBER [timetracker_app]; END"
else
  echo "Unknown mode: $MODE" >&2
  exit 1
fi
