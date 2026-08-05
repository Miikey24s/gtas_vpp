#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${1:-}"

if [[ -z "$ENV_FILE" || ! -f "$ENV_FILE" ]]; then
  echo "Usage: $0 <environment-file>" >&2
  exit 2
fi

read_env_value() {
  local key="$1"
  local line value

  line="$(grep -E "^${key}=" "$ENV_FILE" | tail -n 1 || true)"
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

errors=()

require_non_placeholder() {
  local key="$1"
  local value

  if ! value="$(read_env_value "$key")" || [[ -z "$value" ]]; then
    errors+=("$key is missing or empty")
    return
  fi

  if [[ "$value" =~ (CHANGE_ME|change-this|replace-with|your-|example|placeholder) ]]; then
    errors+=("$key still contains a placeholder")
  fi
}

require_non_placeholder DB_SA_PASSWORD
require_non_placeholder JWT_KEY

validate_boolean() {
  local key="$1"
  local default_value="$2"
  local value

  value="$(read_env_value "$key" 2>/dev/null || printf '%s' "$default_value")"
  case "${value,,}" in
    true|false)
      ;;
    *)
      errors+=("$key must be true or false")
      ;;
  esac
}

validate_boolean EMAIL_NOTIFICATIONS_ENABLED false
validate_boolean EMAIL_SMTP_USE_SSL true

if jwt_key="$(read_env_value JWT_KEY 2>/dev/null)"; then
  jwt_bytes="$(LC_ALL=C printf '%s' "$jwt_key" | wc -c | tr -d ' ')"
  if (( jwt_bytes < 32 )); then
    errors+=("JWT_KEY must contain at least 32 bytes")
  fi
fi

if db_password="$(read_env_value DB_SA_PASSWORD 2>/dev/null)"; then
  if (( ${#db_password} < 16 )); then
    errors+=("DB_SA_PASSWORD must contain at least 16 characters")
  fi

  password_classes=0
  [[ "$db_password" =~ [a-z] ]] && ((password_classes += 1))
  [[ "$db_password" =~ [A-Z] ]] && ((password_classes += 1))
  [[ "$db_password" =~ [0-9] ]] && ((password_classes += 1))
  [[ "$db_password" =~ [^a-zA-Z0-9] ]] && ((password_classes += 1))
  if (( password_classes < 3 )); then
    errors+=("DB_SA_PASSWORD must use at least three character classes")
  fi
fi

report_insights_enabled="$(read_env_value REPORT_INSIGHTS_ENABLED 2>/dev/null || printf 'false')"
case "${report_insights_enabled,,}" in
  true)
    ai_provider_key_count=0
    for ai_key in OPENAI_API_KEY GROQ_API_KEY GEMINI_API_KEY GOOGLE_API_KEY; do
      if ai_value="$(read_env_value "$ai_key" 2>/dev/null)" && [[ -n "$ai_value" ]]; then
        require_non_placeholder "$ai_key"
        ai_provider_key_count=$((ai_provider_key_count + 1))
      fi
    done
    if (( ai_provider_key_count == 0 )); then
      errors+=("REPORT_INSIGHTS_ENABLED=true requires at least one of OPENAI_API_KEY, GROQ_API_KEY, GEMINI_API_KEY or GOOGLE_API_KEY")
    fi
    ;;
  false)
    ;;
  *)
    errors+=("REPORT_INSIGHTS_ENABLED must be true or false")
    ;;
esac

email_enabled="$(read_env_value EMAIL_NOTIFICATIONS_ENABLED 2>/dev/null || printf 'false')"
if [[ "${email_enabled,,}" == "true" ]]; then
  require_non_placeholder PUBLIC_BASE_URL
  require_non_placeholder EMAIL_FROM_ADDRESS
  require_non_placeholder EMAIL_SMTP_HOST
  require_non_placeholder EMAIL_SMTP_PORT
  require_non_placeholder EMAIL_SMTP_USERNAME
  require_non_placeholder EMAIL_SMTP_PASSWORD

  public_base_url="$(read_env_value PUBLIC_BASE_URL 2>/dev/null || true)"
  if [[ ! "$public_base_url" =~ ^https://[^/[:space:]]+/?$ ]]; then
    errors+=("PUBLIC_BASE_URL must be an HTTPS origin without a path")
  fi

  from_address="$(read_env_value EMAIL_FROM_ADDRESS 2>/dev/null || true)"
  if [[ ! "$from_address" =~ ^[^[:space:]@]+@[^[:space:]@]+\.[^[:space:]@]+$ ]]; then
    errors+=("EMAIL_FROM_ADDRESS must be a valid sender address")
  fi

  smtp_port="$(read_env_value EMAIL_SMTP_PORT 2>/dev/null || true)"
  if [[ ! "$smtp_port" =~ ^[0-9]+$ ]] || (( smtp_port < 1 || smtp_port > 65535 )); then
    errors+=("EMAIL_SMTP_PORT must be a valid TCP port")
  elif [[ "$smtp_port" == "25" || "$smtp_port" == "465" || "$smtp_port" == "587" ]]; then
    errors+=("EMAIL_SMTP_PORT=$smtp_port is blocked on DigitalOcean Droplets; use provider port 2587")
  fi

  smtp_use_ssl="$(read_env_value EMAIL_SMTP_USE_SSL 2>/dev/null || printf 'true')"
  if [[ "${smtp_use_ssl,,}" != "true" ]]; then
    errors+=("EMAIL_SMTP_USE_SSL must be true when real email is enabled")
  fi
fi

if (( ${#errors[@]} > 0 )); then
  printf 'Environment validation failed:\n' >&2
  printf '  - %s\n' "${errors[@]}" >&2
  exit 1
fi

echo "Environment validation passed."
