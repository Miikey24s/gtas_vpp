#!/usr/bin/env bash
set -Eeuo pipefail

APP_ROOT="${APP_ROOT:-/app/gtas-vpp}"
NGINX_TARGET="${NGINX_TARGET:-/etc/nginx/sites-available/gtas-vpp}"
PUBLIC_BASE_URL="${PUBLIC_BASE_URL:-https://gtas-vpp.annam.id.vn}"
source_config="deploy/nginx/gtas-vpp.conf"

if [[ ! -f "$source_config" ]]; then
  echo "Missing Nginx frontend config: $source_config" >&2
  exit 1
fi

backup="$APP_ROOT/shared/nginx.before-switch.conf"
had_previous=false
if sudo test -f "$NGINX_TARGET"; then
  sudo cp "$NGINX_TARGET" "$backup"
  had_previous=true
fi

restore_previous() {
  trap - ERR
  set +e
  if [[ "$had_previous" == "true" && -f "$backup" ]]; then
    sudo cp "$backup" "$NGINX_TARGET"
    sudo nginx -t && sudo systemctl reload nginx
  fi
}

trap 'restore_previous' ERR

sudo install -m 0644 "$source_config" "$NGINX_TARGET"
sudo ln -sfn "$NGINX_TARGET" /etc/nginx/sites-enabled/gtas-vpp
sudo nginx -t
sudo systemctl reload nginx

PUBLIC_BASE_URL="$PUBLIC_BASE_URL" \
SMOKE_RETRY_COUNT="${SMOKE_RETRY_COUNT:-10}" \
  bash deploy/smoke-frontend.sh

trap - ERR
echo "Blazor public frontend config installed successfully."
