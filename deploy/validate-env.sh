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
    require_non_placeholder OPENAI_API_KEY
    ;;
  false)
    ;;
  *)
    errors+=("REPORT_INSIGHTS_ENABLED must be true or false")
    ;;
esac

if (( ${#errors[@]} > 0 )); then
  printf 'Environment validation failed:\n' >&2
  printf '  - %s\n' "${errors[@]}" >&2
  exit 1
fi

echo "Environment validation passed."
