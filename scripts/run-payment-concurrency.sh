#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(
  cd "$(dirname "${BASH_SOURCE[0]}")/.."
  pwd
)"

cd "$ROOT_DIR"

CONTAINER="efratagro-adubos-mysql"
TEST_DB="efratagro_concurrency_test"
TEST_USER="efratagro_test"
TEST_PASSWORD="EfratAgroTestOnly_2026!"

TEST_CONNECTION_STRING="Server=127.0.0.1;Port=13307;Database=${TEST_DB};User=${TEST_USER};Password=${TEST_PASSWORD};"

cleanup() {
  echo
  echo "===== CLEANUP ====="

  docker exec \
    -i \
    "$CONTAINER" \
    sh -lc \
    'mysql -uroot -p"$MYSQL_ROOT_PASSWORD"' \
    >/dev/null 2>&1 <<'SQL' || true
DROP DATABASE IF EXISTS `efratagro_concurrency_test`;
DROP USER IF EXISTS 'efratagro_test'@'%';
SQL

  echo "Temporary MySQL test resources removed."
}

trap cleanup EXIT

echo "===== CREATE TEMPORARY DATABASE ====="

docker exec \
  -i \
  "$CONTAINER" \
  sh -lc \
  'mysql -uroot -p"$MYSQL_ROOT_PASSWORD"' \
  <<'SQL'
DROP DATABASE IF EXISTS `efratagro_concurrency_test`;
DROP USER IF EXISTS 'efratagro_test'@'%';

CREATE DATABASE `efratagro_concurrency_test`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

CREATE USER
  'efratagro_test'@'%'
  IDENTIFIED BY
  'EfratAgroTestOnly_2026!';

GRANT ALL PRIVILEGES
  ON `efratagro_concurrency_test`.*
  TO 'efratagro_test'@'%';

FLUSH PRIVILEGES;
SQL

echo
echo "===== APPLY MIGRATIONS ====="

ConnectionStrings__DefaultConnection="$TEST_CONNECTION_STRING" \
ASPNETCORE_ENVIRONMENT=Development \
dotnet tool run dotnet-ef database update \
  --project src/EfratAgro.Adubos.Infrastructure \
  --startup-project src/EfratAgro.Adubos.Api

echo
echo "===== CONCURRENCY TEST ====="

ConnectionStrings__ConcurrencyTest="$TEST_CONNECTION_STRING" \
dotnet test \
  tests/EfratAgro.Adubos.MySqlIntegrationTests/EfratAgro.Adubos.MySqlIntegrationTests.csproj \
  --filter ConcurrentPayments_MustNotOverpaySameReceivable

