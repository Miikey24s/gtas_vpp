#!/usr/bin/env bash
set -euo pipefail

PUBLIC_BASE_URL="${PUBLIC_BASE_URL:-https://gtas-vpp.annam.id.vn}"
PUBLIC_BASE_URL="${PUBLIC_BASE_URL%/}"
PUBLIC_HEALTH_URL="${PUBLIC_HEALTH_URL:-${PUBLIC_BASE_URL}/healthz}"

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

public_login_url="${PUBLIC_LOGIN_URL:-${PUBLIC_BASE_URL}/Account/Login}"
public_signalr_negotiate_url="${PUBLIC_SIGNALR_NEGOTIATE_URL:-${PUBLIC_BASE_URL}/_blazor/negotiate?negotiateVersion=1}"

login_html="$(curl "${curl_args[@]}" "$public_login_url")"
grep -Fq 'vpp-login-card' <<<"$login_html" \
  || { echo "Blazor smoke failed: login card was not rendered." >&2; exit 1; }
grep -Fq '_framework/blazor.web' <<<"$login_html" \
  || { echo "Blazor smoke failed: bootstrap script is missing." >&2; exit 1; }

negotiate_json="$(curl "${curl_args[@]}" \
  --request POST \
  --header 'Content-Type: text/plain;charset=UTF-8' \
  --data '' \
  "$public_signalr_negotiate_url")"
grep -Fq '"WebSockets"' <<<"$negotiate_json" \
  || { echo "Blazor smoke failed: SignalR did not advertise WebSockets." >&2; exit 1; }

echo "Blazor render and SignalR smoke checks passed."
