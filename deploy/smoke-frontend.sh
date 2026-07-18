#!/usr/bin/env bash
set -euo pipefail

PUBLIC_BASE_URL="${PUBLIC_BASE_URL:-https://gtas-vpp.annam.id.vn}"
PUBLIC_BASE_URL="${PUBLIC_BASE_URL%/}"
PUBLIC_HEALTH_URL="${PUBLIC_HEALTH_URL:-${PUBLIC_BASE_URL}/healthz}"
PUBLIC_LOGIN_URL="${PUBLIC_LOGIN_URL:-${PUBLIC_BASE_URL}/Account/Login}"
PUBLIC_SIGNALR_NEGOTIATE_URL="${PUBLIC_SIGNALR_NEGOTIATE_URL:-${PUBLIC_BASE_URL}/_blazor/negotiate?negotiateVersion=1}"

curl_args=(
  --fail
  --silent
  --show-error
  --location
  --retry "${SMOKE_RETRY_COUNT:-5}"
  --retry-delay 2
  --retry-all-errors
  --connect-timeout 5
  --max-time 30
)

curl "${curl_args[@]}" "$PUBLIC_HEALTH_URL" >/dev/null

login_html="$(curl "${curl_args[@]}" "$PUBLIC_LOGIN_URL")"
grep -Fq 'vpp-login-card' <<<"$login_html" \
  || { echo "Frontend smoke failed: login card was not rendered." >&2; exit 1; }
grep -Fq '_framework/blazor.web' <<<"$login_html" \
  || { echo "Frontend smoke failed: Blazor bootstrap script is missing." >&2; exit 1; }

negotiate_json="$(curl "${curl_args[@]}" \
  --request POST \
  --header 'Content-Type: text/plain;charset=UTF-8' \
  --data '' \
  "$PUBLIC_SIGNALR_NEGOTIATE_URL")"
grep -Fq '"WebSockets"' <<<"$negotiate_json" \
  || { echo "Frontend smoke failed: SignalR did not advertise WebSockets." >&2; exit 1; }

echo "Frontend render and SignalR smoke checks passed."
