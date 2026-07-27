#!/usr/bin/env bash
set -Eeuo pipefail

APP_ROOT="${APP_ROOT:-/app/gtas-vpp}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
APP_NETWORK="${APP_NETWORK:-gtas-vpp-internal}"
DB_CONTAINER="${DB_CONTAINER:-gtas-vpp-db}"
BACKEND_CONTAINER="${BACKEND_CONTAINER:-gtas-vpp-backend}"
FRONTEND_CONTAINER="${FRONTEND_CONTAINER:-gtas-vpp-frontend}"
DB_FALLBACK_CONTAINER="${DB_FALLBACK_CONTAINER:-${DB_CONTAINER}-previous}"
PUBLIC_BASE_URL="${PUBLIC_BASE_URL:-https://gtas-vpp.annam.id.vn}"
PUBLIC_HEALTH_URL="${PUBLIC_HEALTH_URL:-${PUBLIC_BASE_URL%/}/healthz}"
DEPLOY_SHA="${DEPLOY_SHA:-unknown}"

: "${BE_IMAGE:?BE_IMAGE is required}"
: "${FE_IMAGE:?FE_IMAGE is required}"

compose() {
  docker compose -f "$COMPOSE_FILE" "$@"
}

read_env_value() {
  local key="$1"
  local line value

  line="$(grep -E "^${key}=" .env | tail -n 1 || true)"
  [[ -n "$line" ]] || return 1
  value="${line#*=}"
  value="${value%$'\r'}"
  if [[ ${#value} -ge 2 && "$value" == \"*\" ]]; then
    value="${value:1:${#value}-2}"
  elif [[ ${#value} -ge 2 && "$value" == \'*\' ]]; then
    value="${value:1:${#value}-2}"
  fi
  printf '%s' "$value"
}

read_deploy_state_value() {
  local state_file="$1"
  local key="$2"
  local line value

  line="$(grep -E "^${key}=" "$state_file" | tail -n 1 || true)"
  [[ -n "$line" ]] || return 1
  value="${line#*=}"
  value="${value%$'\r'}"
  printf '%s' "$value"
}

load_last_known_db_runtime() {
  local state_file="$APP_ROOT/current/deploy-state.env"
  local pinned_image pinned_volume

  if [[ ! -f "$state_file" ]]; then
    echo "Cannot recover a missing SQL Server container without the last successful deploy state." >&2
    return 1
  fi

  pinned_image="$(read_deploy_state_value "$state_file" DB_IMAGE)" || return 1
  pinned_volume="$(read_deploy_state_value "$state_file" DB_DATA_VOLUME)" || return 1
  if [[ ! "$pinned_image" =~ ^sha256:[0-9a-f]{64}$ ]]; then
    echo "The last successful deploy state does not contain an immutable SQL Server image." >&2
    return 1
  fi
  if [[ ! "$pinned_volume" =~ ^[A-Za-z0-9_.-]+$ ]]; then
    echo "The last successful deploy state contains an invalid SQL Server volume name." >&2
    return 1
  fi
  docker image inspect "$pinned_image" >/dev/null 2>&1 || {
    echo "The last-known SQL Server image is not available locally." >&2
    return 1
  }
  docker volume inspect "$pinned_volume" >/dev/null 2>&1 || {
    echo "The last-known SQL Server data volume is unavailable." >&2
    return 1
  }

  DB_IMAGE="$pinned_image"
  DB_DATA_VOLUME="$pinned_volume"
  export DB_IMAGE DB_DATA_VOLUME
  DB_RUNTIME_PINNED=true
  echo "Loaded the pinned SQL Server runtime from the last successful deploy state."
}

require_pinned_db_runtime() {
  if [[ "${DB_RUNTIME_PINNED:-false}" != "true" \
    || ! "${DB_IMAGE:-}" =~ ^sha256:[0-9a-f]{64}$ \
    || ! "${DB_DATA_VOLUME:-}" =~ ^[A-Za-z0-9_.-]+$ ]]; then
    echo "SQL Server recovery requires a validated immutable image and named data volume." >&2
    return 1
  fi
  docker image inspect "$DB_IMAGE" >/dev/null 2>&1 || return 1
  docker volume inspect "$DB_DATA_VOLUME" >/dev/null 2>&1 || return 1
}

wait_for_healthy() {
  local container="$1"
  local attempts="${2:-60}"
  local status=""

  for ((attempt = 1; attempt <= attempts; attempt += 1)); do
    status="$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else if .State.Running}}running{{else}}stopped{{end}}' "$container" 2>/dev/null || true)"
    if [[ "$status" == "healthy" ]]; then
      return 0
    fi
    if [[ "$status" == "unhealthy" || "$status" == "stopped" ]]; then
      docker logs --tail 100 "$container" 2>/dev/null || true
      return 1
    fi
    sleep 2
  done

  docker logs --tail 100 "$container" 2>/dev/null || true
  echo "$container did not become healthy in time (last status: ${status:-missing})." >&2
  return 1
}

db_can_connect() {
  local password="$1"
  docker exec \
    -e CHECK_DB_PASSWORD="$password" \
    "$DB_CONTAINER" bash -euc '
      /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$CHECK_DB_PASSWORD" -l 2 -C -b -Q "SELECT 1;" >/dev/null
    ' >/dev/null 2>&1
}

wait_for_db_connection() {
  local password="$1"
  local attempts="${2:-180}"

  for ((attempt = 1; attempt <= attempts; attempt += 1)); do
    if db_can_connect "$password"; then
      return 0
    fi
    sleep 2
  done

  docker logs --tail 100 "$DB_CONTAINER" 2>/dev/null || true
  echo "SQL Server did not accept a verified connection in time." >&2
  return 1
}

ensure_desired_db_container() {
  if ! docker container inspect "$DB_CONTAINER" >/dev/null 2>&1; then
    require_pinned_db_runtime || return 1
    echo "Recreating the missing SQL Server container from the pinned runtime state." >&2
    compose up -d --no-deps db || return 1
  fi
  wait_for_db_connection "$desired_db_password" 180
}

OLD_BE_IMAGE="$(docker inspect --format='{{.Config.Image}}' "$BACKEND_CONTAINER" 2>/dev/null || true)"
OLD_FE_IMAGE="$(docker inspect --format='{{.Config.Image}}' "$FRONTEND_CONTAINER" 2>/dev/null || true)"
DEPLOYING_APPS=false
NGINX_SWITCHED=false
DB_PASSWORD_ROLL_FORWARD_REQUIRED=false
APP_ENV_ROLL_FORWARD_REQUIRED=false
DB_RUNTIME_PINNED=false
current_db_password=""
desired_db_password=""
active_db_password=""

rollback_apps() {
  trap - ERR
  set +e
  local rollback_failed=false

  if [[ -z "$OLD_BE_IMAGE" || -z "$OLD_FE_IMAGE" ]]; then
    echo "No complete previous application image pair is available for rollback." >&2
    return 1
  fi

  echo "Rolling application containers back to their previous images with the current environment..." >&2
  BE_IMAGE="$OLD_BE_IMAGE" FE_IMAGE="$OLD_FE_IMAGE" \
    docker compose -f "$COMPOSE_FILE" up -d --no-deps --force-recreate backend \
      || rollback_failed=true
  wait_for_healthy "$BACKEND_CONTAINER" 60 || rollback_failed=true
  BE_IMAGE="$OLD_BE_IMAGE" FE_IMAGE="$OLD_FE_IMAGE" \
    docker compose -f "$COMPOSE_FILE" up -d --no-deps --force-recreate frontend \
      || rollback_failed=true
  wait_for_healthy "$FRONTEND_CONTAINER" 60 || rollback_failed=true
  [[ "$rollback_failed" == "false" ]]
}

rollback_nginx() {
  trap - ERR
  set +e
  local backup="$APP_ROOT/shared/nginx.before-switch.conf"
  local target="/etc/nginx/sites-available/gtas-vpp"

  if [[ "$NGINX_SWITCHED" != "true" || ! -f "$backup" ]]; then
    return 0
  fi

  echo "Restoring the previous public frontend routing..." >&2
  sudo cp "$backup" "$target" || return 1
  sudo nginx -t || return 1
  sudo systemctl reload nginx
}

roll_forward_db_password() {
  trap - ERR
  set +e

  if [[ -z "$desired_db_password" ]]; then
    echo "The desired SQL Server credential is unavailable for roll-forward recovery." >&2
    return 1
  fi

  if ! docker container inspect "$DB_CONTAINER" >/dev/null 2>&1; then
    require_pinned_db_runtime || return 1
    echo "Recreating the missing SQL Server container from the pinned runtime state." >&2
    compose up -d --no-deps db || return 1
  fi

  if db_can_connect "$desired_db_password"; then
    return 0
  fi

  if [[ -z "$current_db_password" ]]; then
    echo "No active SQL Server credential is available to complete roll-forward recovery." >&2
    return 1
  fi

  echo "Completing SQL Server credential roll-forward after a failed reconciliation..." >&2
  local reconciled=false
  for ((attempt = 1; attempt <= 60; attempt += 1)); do
    if db_can_connect "$desired_db_password"; then
      reconciled=true
      break
    fi
    if docker exec \
      -e ACTIVE_DB_PASSWORD="$current_db_password" \
      -e NEW_DB_PASSWORD="$desired_db_password" \
      "$DB_CONTAINER" bash -euc '
        /opt/mssql-tools18/bin/sqlcmd \
          -S localhost -U sa -P "$ACTIVE_DB_PASSWORD" -l 2 -C -b \
          -v NEW_PASSWORD="$NEW_DB_PASSWORD" \
          -Q "ALTER LOGIN [sa] WITH PASSWORD = N'\''\$(NEW_PASSWORD)'\'';"
      '; then
      reconciled=true
      break
    fi
    sleep 2
  done

  if [[ "$reconciled" != "true" ]]; then
    echo "Could not complete SQL Server credential roll-forward." >&2
    return 1
  fi
  wait_for_db_connection "$desired_db_password" 180
}

db_container_matches_expected_runtime() {
  local actual_data_volume=""
  local actual_image=""

  actual_image="$(docker inspect --format='{{.Image}}' "$DB_CONTAINER" 2>/dev/null)"
  if [[ -n "${DB_IMAGE:-}" && "$actual_image" != "$DB_IMAGE" ]]; then
    echo "Recovered SQL Server container is using an unexpected image." >&2
    return 1
  fi

  if [[ -n "${DB_DATA_VOLUME:-}" ]]; then
    actual_data_volume="$(
      docker inspect \
        --format='{{range .Mounts}}{{if eq .Destination "/var/opt/mssql"}}{{.Name}}{{end}}{{end}}' \
        "$DB_CONTAINER" 2>/dev/null
    )"
    if [[ "$actual_data_volume" != "$DB_DATA_VOLUME" ]]; then
      echo "Recovered SQL Server container is using an unexpected data volume." >&2
      return 1
    fi
  fi

  while IFS= read -r binding; do
    if [[ -n "$binding" && "$binding" != 127.0.0.1:* ]]; then
      echo "Recovered SQL Server container has an unsafe host binding: $binding" >&2
      return 1
    fi
  done < <(docker port "$DB_CONTAINER" 1433/tcp 2>/dev/null || true)

  return 0
}

reconcile_db_container_secret_metadata() {
  trap - ERR
  set +e

  local container_password=""
  container_password="$(
    docker inspect --format='{{range .Config.Env}}{{println .}}{{end}}' "$DB_CONTAINER" \
      2>/dev/null \
      | sed -n 's/^MSSQL_SA_PASSWORD=//p' \
      | tail -n 1
  )"

  if [[ -n "$container_password" && "$container_password" == "$desired_db_password" ]]; then
    return 0
  fi

  echo "Recreating the SQL Server container with the desired credential metadata and named data volume..." >&2

  if ! compose up -d --no-deps --force-recreate db \
    || ! wait_for_db_connection "$desired_db_password" 180 \
    || ! db_container_matches_expected_runtime; then
    echo "Could not recreate SQL Server with the desired credential metadata." >&2
    return 1
  fi

  container_password="$(
    docker inspect --format='{{range .Config.Env}}{{println .}}{{end}}' "$DB_CONTAINER" \
      2>/dev/null \
      | sed -n 's/^MSSQL_SA_PASSWORD=//p' \
      | tail -n 1
  )"
  if [[ -z "$container_password" || "$container_password" != "$desired_db_password" ]]; then
    echo "Recreated SQL Server container does not reference the desired credential." >&2
    return 1
  fi
}

on_error() {
  local exit_code="$1"
  local line="$2"
  local recovery_failed=false
  echo "Deployment failed at line $line (exit $exit_code)." >&2
  if [[ "$DB_PASSWORD_ROLL_FORWARD_REQUIRED" == "true" ]]; then
    if ! roll_forward_db_password; then
      recovery_failed=true
    elif ! reconcile_db_container_secret_metadata; then
      recovery_failed=true
    fi
  elif [[ -n "$active_db_password" ]]; then
    wait_for_db_connection "$active_db_password" 180 || recovery_failed=true
  elif [[ -n "$desired_db_password" ]]; then
    ensure_desired_db_container || recovery_failed=true
  fi
  if [[ "$DEPLOYING_APPS" == "true" || "$APP_ENV_ROLL_FORWARD_REQUIRED" == "true" ]]; then
    rollback_nginx || recovery_failed=true
    rollback_apps || recovery_failed=true
  fi
  if [[ "$recovery_failed" == "true" ]]; then
    echo "Automatic recovery was incomplete; keep the desired credential and investigate before retrying." >&2
  fi
  exit "$exit_code"
}

trap 'on_error $? $LINENO' ERR

if [[ ! -f "$COMPOSE_FILE" || ! -f .env ]]; then
  echo "Run this script from a release directory containing $COMPOSE_FILE and .env." >&2
  exit 2
fi

bash deploy/validate-env.sh .env

if docker container inspect "$DB_FALLBACK_CONTAINER" >/dev/null 2>&1; then
  echo "A legacy SQL Server fallback container exists; refusing automatic destructive recovery." >&2
  exit 1
fi
while IFS= read -r compose_db_container; do
  if [[ -n "$compose_db_container" && "$compose_db_container" != "$DB_CONTAINER" ]]; then
    echo "An unexpected Docker Compose SQL Server service container exists; refusing reconciliation." >&2
    exit 1
  fi
done < <(
  docker ps -a \
    --filter label=com.docker.compose.project=gtas-vpp \
    --filter label=com.docker.compose.service=db \
    --format '{{.Names}}'
)

desired_db_password="$(read_env_value DB_SA_PASSWORD)"
unsafe_db_binding=false
db_exists=false
db_password_changed=false
if docker container inspect "$DB_CONTAINER" >/dev/null 2>&1; then
  db_exists=true
  DB_IMAGE="$(docker inspect --format='{{.Image}}' "$DB_CONTAINER")"
  DB_DATA_VOLUME="$(
    docker inspect \
      --format='{{range .Mounts}}{{if eq .Destination "/var/opt/mssql"}}{{.Name}}{{end}}{{end}}' \
      "$DB_CONTAINER"
  )"
  if [[ -z "$DB_DATA_VOLUME" ]]; then
    echo "The existing SQL Server container is not using a named /var/opt/mssql volume." >&2
    exit 1
  fi
  if [[ ! "$DB_IMAGE" =~ ^sha256:[0-9a-f]{64}$ \
    || ! "$DB_DATA_VOLUME" =~ ^[A-Za-z0-9_.-]+$ ]]; then
    echo "The existing SQL Server runtime cannot be pinned safely." >&2
    exit 1
  fi
  export DB_IMAGE DB_DATA_VOLUME
  DB_RUNTIME_PINNED=true
  while IFS= read -r binding; do
    [[ -z "$binding" || "$binding" == 127.0.0.1:* ]] || unsafe_db_binding=true
  done < <(docker port "$DB_CONTAINER" 1433/tcp 2>/dev/null || true)

  current_db_password="$(
    docker inspect --format='{{range .Config.Env}}{{println .}}{{end}}' "$DB_CONTAINER" \
      | sed -n 's/^MSSQL_SA_PASSWORD=//p' \
      | tail -n 1
  )"
  if [[ -z "$current_db_password" ]]; then
    echo "Cannot inspect the existing SQL Server credential for safe rotation." >&2
    exit 1
  fi

  for ((attempt = 1; attempt <= 60; attempt += 1)); do
    if db_can_connect "$current_db_password"; then
      active_db_password="$current_db_password"
      break
    fi
    if db_can_connect "$desired_db_password"; then
      active_db_password="$desired_db_password"
      break
    fi
    sleep 2
  done
  if [[ -z "$active_db_password" ]]; then
    echo "SQL Server is not reachable with either the container or desired credential." >&2
    exit 1
  fi
  [[ "$current_db_password" == "$desired_db_password" ]] || db_password_changed=true
else
  load_last_known_db_runtime
fi

require_pinned_db_runtime
compose config --quiet
docker network inspect "$APP_NETWORK" >/dev/null 2>&1 || docker network create "$APP_NETWORK" >/dev/null

if [[ "$db_exists" == "true" && ( "$unsafe_db_binding" == "true" || "$db_password_changed" == "true" ) ]]; then
  echo "Taking a safety backup before reconciling the SQL Server container."
  DB_PASSWORD="$active_db_password" \
    bash deploy/backup-db-pair.sh before-db-reconcile /var/opt/mssql/data

  if [[ "$db_password_changed" == "true" ]]; then
    DB_PASSWORD_ROLL_FORWARD_REQUIRED=true
    # From this point onward, every recovery path must recreate the previous
    # application images with the current .env. ALTER LOGIN is roll-forward
    # only, so existing containers may otherwise retain the obsolete secret.
    APP_ENV_ROLL_FORWARD_REQUIRED=true
    if [[ "$active_db_password" != "$desired_db_password" ]]; then
      echo "Rotating the SQL Server sa credential without exposing either value."
      docker exec \
        -e ACTIVE_DB_PASSWORD="$active_db_password" \
        -e NEW_DB_PASSWORD="$desired_db_password" \
        "$DB_CONTAINER" bash -euc '
          /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U sa -P "$ACTIVE_DB_PASSWORD" -l 2 -C -b \
            -v NEW_PASSWORD="$NEW_DB_PASSWORD" \
            -Q "ALTER LOGIN [sa] WITH PASSWORD = N'\''\$(NEW_PASSWORD)'\'';"
        '
    else
      echo "Resuming an interrupted SQL Server credential reconciliation."
    fi
  fi

  echo "Recreating SQL Server with the named data volume and desired runtime metadata."
  compose up -d --no-deps --force-recreate db
else
  compose up -d --no-deps db
fi

wait_for_db_connection "$desired_db_password" 180

if [[ "$db_password_changed" == "true" && "$current_db_password" != "$desired_db_password" ]]; then
  if db_can_connect "$current_db_password"; then
    echo "The previous SQL Server credential is still accepted after rotation." >&2
    exit 1
  fi
  echo "Previous SQL Server credential was rejected as expected."
fi

db_container_matches_expected_runtime

while IFS= read -r binding; do
  if [[ -n "$binding" && "$binding" != 127.0.0.1:* ]]; then
    echo "Unsafe SQL Server host binding remains after reconciliation: $binding" >&2
    exit 1
  fi
done < <(docker port "$DB_CONTAINER" 1433/tcp 2>/dev/null || true)

bash deploy/backup-db-pair.sh pre-deploy

compose --profile tools pull backend frontend migrator
compose run --rm --no-deps migrator

DEPLOYING_APPS=true
compose up -d --no-deps --force-recreate backend
wait_for_healthy "$BACKEND_CONTAINER" 60
compose up -d --no-deps --force-recreate frontend
wait_for_healthy "$FRONTEND_CONTAINER" 60

if command -v nginx >/dev/null 2>&1; then
  APP_ROOT="$APP_ROOT" \
  PUBLIC_BASE_URL="$PUBLIC_BASE_URL" \
    bash deploy/install-nginx-config.sh
  NGINX_SWITCHED=true
fi

if command -v systemctl >/dev/null 2>&1; then
  sudo install -m 0644 deploy/systemd/gtas-vpp-backup.service \
    /etc/systemd/system/gtas-vpp-backup.service
  sudo install -m 0644 deploy/systemd/gtas-vpp-backup.timer \
    /etc/systemd/system/gtas-vpp-backup.timer
  sudo systemctl daemon-reload
  sudo systemctl enable --now gtas-vpp-backup.timer
fi

bash deploy/harden-host.sh
bash deploy/audit-host.sh

PUBLIC_BASE_URL="$PUBLIC_BASE_URL" \
PUBLIC_HEALTH_URL="$PUBLIC_HEALTH_URL" \
SMOKE_RETRY_COUNT=10 \
  bash deploy/smoke-frontend.sh

cat > deploy-state.env <<EOF
DEPLOY_SHA=$DEPLOY_SHA
DB_IMAGE=$DB_IMAGE
DB_DATA_VOLUME=${DB_DATA_VOLUME:-gtas-vpp_sqlserver-data}
BE_IMAGE=$BE_IMAGE
FE_IMAGE=$FE_IMAGE
DEPLOYED_AT=$(date -u +%Y-%m-%dT%H:%M:%SZ)
EOF
chmod 600 deploy-state.env

DEPLOYING_APPS=false
NGINX_SWITCHED=false
DB_PASSWORD_ROLL_FORWARD_REQUIRED=false
APP_ENV_ROLL_FORWARD_REQUIRED=false
trap - ERR
compose ps
echo "Deployment completed and public frontend smoke checks passed."
