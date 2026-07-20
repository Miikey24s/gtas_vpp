#!/usr/bin/env bash
set -euo pipefail

failures=0
warnings=0

fail() {
  echo "FAIL: $*" >&2
  ((failures += 1))
}

warn() {
  echo "WARN: $*" >&2
  ((warnings += 1))
}

for command_name in docker nginx systemctl curl ss; do
  command -v "$command_name" >/dev/null 2>&1 || fail "$command_name is not installed"
done

if [[ -r /etc/os-release ]]; then
  grep -E '^(PRETTY_NAME|VERSION_ID)=' /etc/os-release
fi
docker version --format 'Docker Engine {{.Server.Version}}' 2>/dev/null || true
docker compose version 2>/dev/null || true
nginx -v 2>&1 || true

for container in gtas-vpp-db gtas-vpp-backend gtas-vpp-frontend gtas-vpp-react-frontend; do
  status="$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else if .State.Running}}running{{else}}stopped{{end}}' "$container" 2>/dev/null || true)"
  if [[ "$container" == "gtas-vpp-db" ]]; then
    [[ "$status" == "running" || "$status" == "healthy" ]] \
      || fail "$container state is ${status:-missing}"
  else
    [[ "$status" == "healthy" ]] || fail "$container health is ${status:-missing}"
  fi
done

if ! docker exec gtas-vpp-db bash -euc '
  /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "SELECT 1;" >/dev/null
'; then
  fail "SQL Server did not pass the authenticated query probe"
fi

for mapping in \
  "gtas-vpp-db:1433/tcp" \
  "gtas-vpp-backend:8080/tcp" \
  "gtas-vpp-frontend:5000/tcp" \
  "gtas-vpp-react-frontend:8080/tcp"; do
  container="${mapping%%:*}"
  container_port="${mapping#*:}"
  while IFS= read -r binding; do
    [[ -z "$binding" ]] && continue
    case "$binding" in
      127.0.0.1:*)
        ;;
      *)
        fail "$container publishes $container_port outside loopback: $binding"
        ;;
    esac
  done < <(docker port "$container" "$container_port" 2>/dev/null || true)
done

while IFS= read -r listener; do
  [[ -z "$listener" ]] && continue
  address="$(awk '{print $4}' <<<"$listener")"
  case "$address" in
    127.0.0.1:*|\[::1\]:*)
      ;;
    *)
      fail "internal service is publicly bound: $address"
      ;;
  esac
done < <(ss -lntH | awk '$4 ~ /:(1433|5000|5100|8080)$/')

systemctl is-enabled --quiet gtas-vpp-backup.timer \
  || fail "gtas-vpp-backup.timer is not enabled"
systemctl is-active --quiet gtas-vpp-backup.timer \
  || fail "gtas-vpp-backup.timer is not active"

if systemctl list-unit-files certbot.timer >/dev/null 2>&1; then
  systemctl is-enabled --quiet certbot.timer || warn "certbot.timer is not enabled"
  systemctl is-active --quiet certbot.timer || warn "certbot.timer is not active"
else
  warn "certbot.timer is not installed; verify the certificate renewal mechanism"
fi

if command -v ufw >/dev/null 2>&1; then
  ufw_status="$(sudo ufw status verbose)"
  sed -n '1,24p' <<<"$ufw_status"
  grep -q '^Status: active$' <<<"$ufw_status" || fail "UFW is not active"
  if grep -Eq '^(1433|5000|5100|8080)(/tcp)?[[:space:]]+ALLOW' <<<"$ufw_status"; then
    fail "UFW allows an internal application port"
  fi
else
  warn "UFW is not installed; confirm a DigitalOcean Cloud Firewall protects the Droplet"
fi

if command -v sshd >/dev/null 2>&1; then
  sshd_config="$(sudo sshd -T 2>/dev/null || true)"
  grep -q '^passwordauthentication no$' <<<"$sshd_config" \
    || warn "SSH password authentication is not explicitly disabled"
  grep -Eq '^permitrootlogin (no|prohibit-password|without-password)$' <<<"$sshd_config" \
    || warn "SSH root login policy should be reviewed"
fi

df -h / /var/lib/docker 2>/dev/null || df -h /
docker system df

if (( failures > 0 )); then
  echo "Host audit failed with $failures failure(s) and $warnings warning(s)." >&2
  exit 1
fi

echo "Host audit passed with $warnings warning(s)."
