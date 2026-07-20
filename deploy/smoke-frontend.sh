#!/usr/bin/env bash
set -euo pipefail

PUBLIC_BASE_URL="${PUBLIC_BASE_URL:-https://gtas-vpp.annam.id.vn}"
PUBLIC_BASE_URL="${PUBLIC_BASE_URL%/}"
PUBLIC_HEALTH_URL="${PUBLIC_HEALTH_URL:-${PUBLIC_BASE_URL}/healthz}"
FRONTEND_MODE="${FRONTEND_MODE:-react}"

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

case "$FRONTEND_MODE" in
  react)
    public_login_url="${PUBLIC_LOGIN_URL:-${PUBLIC_BASE_URL}/login}"
    public_deep_link_url="${PUBLIC_DEEP_LINK_URL:-${PUBLIC_BASE_URL}/app/orders}"

    login_html="$(curl "${curl_args[@]}" "$public_login_url")"
    grep -Fq 'id="root"' <<<"$login_html" \
      || { echo "React smoke failed: SPA root was not rendered." >&2; exit 1; }
    grep -Eq 'src="/assets/[^\"]+\.js"' <<<"$login_html" \
      || { echo "React smoke failed: fingerprinted production script is missing." >&2; exit 1; }
    if grep -Fq '/src/main.tsx' <<<"$login_html"; then
      echo "React smoke failed: development Vite entry point was exposed." >&2
      exit 1
    fi

    deep_link_html="$(curl "${curl_args[@]}" "$public_deep_link_url")"
    grep -Fq 'id="root"' <<<"$deep_link_html" \
      || { echo "React smoke failed: deep-link fallback did not return the SPA." >&2; exit 1; }

    asset_path="$(grep -Eo 'src="/assets/[^\"]+\.js"' <<<"$login_html" | head -n 1 | cut -d'"' -f2)"
    asset_headers="$(curl "${curl_args[@]}" --head "${PUBLIC_BASE_URL}${asset_path}")"
    grep -Eiq '^cache-control:.*immutable' <<<"$asset_headers" \
      || { echo "React smoke failed: fingerprinted asset is not immutable-cached." >&2; exit 1; }

    echo "React SPA render, deep-link, and cache smoke checks passed."
    ;;
  blazor)
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
    ;;
  *)
    echo "Unsupported FRONTEND_MODE: $FRONTEND_MODE (expected react or blazor)." >&2
    exit 2
    ;;
esac
