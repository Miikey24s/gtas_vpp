#!/usr/bin/env bash
set -euo pipefail

run_as_root() {
  if (( EUID == 0 )); then
    "$@"
  else
    sudo "$@"
  fi
}

sshd_bin="$(command -v sshd || true)"
if [[ -z "$sshd_bin" && -x /usr/sbin/sshd ]]; then
  sshd_bin=/usr/sbin/sshd
fi

if [[ -n "$sshd_bin" ]]; then
  sshd_dir=/etc/ssh/sshd_config.d
  sshd_target="$sshd_dir/00-gtas-vpp-hardening.conf"
  sshd_backup="$(mktemp)"
  sshd_candidate="$(mktemp)"
  had_previous=false

  cleanup_ssh_files() {
    rm -f "$sshd_backup" "$sshd_candidate"
  }
  trap cleanup_ssh_files EXIT

  cat >"$sshd_candidate" <<'EOF'
# Managed by the GTAS VPP deployment. Keep SSH key access available for deployment.
PubkeyAuthentication yes
PasswordAuthentication no
KbdInteractiveAuthentication no
ChallengeResponseAuthentication no
PermitRootLogin prohibit-password
EOF

  run_as_root install -d -m 0755 "$sshd_dir"
  if run_as_root test -f "$sshd_target"; then
    run_as_root cp "$sshd_target" "$sshd_backup"
    had_previous=true
  fi
  run_as_root install -m 0644 "$sshd_candidate" "$sshd_target"

  restore_ssh_config() {
    if [[ "$had_previous" == true ]]; then
      run_as_root cp "$sshd_backup" "$sshd_target"
    else
      run_as_root rm -f "$sshd_target"
    fi
  }

  if ! run_as_root "$sshd_bin" -t; then
    restore_ssh_config
    echo "The SSH hardening configuration was invalid and has been restored." >&2
    exit 1
  fi

  if ! run_as_root systemctl reload ssh; then
    restore_ssh_config
    run_as_root "$sshd_bin" -t
    run_as_root systemctl reload ssh
    echo "SSH reload failed; the previous configuration was restored." >&2
    exit 1
  fi

  effective_sshd="$(run_as_root "$sshd_bin" -T)"
  if ! grep -q '^pubkeyauthentication yes$' <<<"$effective_sshd" \
    || ! grep -q '^passwordauthentication no$' <<<"$effective_sshd" \
    || ! grep -q '^kbdinteractiveauthentication no$' <<<"$effective_sshd" \
    || ! grep -Eq '^permitrootlogin (prohibit-password|without-password)$' <<<"$effective_sshd"; then
    restore_ssh_config
    run_as_root "$sshd_bin" -t
    run_as_root systemctl reload ssh
    echo "SSH hardening was not effective; the previous configuration was restored." >&2
    exit 1
  fi

  trap - EXIT
  cleanup_ssh_files
fi

if command -v ufw >/dev/null 2>&1; then
  # Add required rules before changing defaults so an interrupted run cannot lock SSH out.
  run_as_root ufw allow OpenSSH >/dev/null
  run_as_root ufw allow 80/tcp >/dev/null
  run_as_root ufw allow 443/tcp >/dev/null
  run_as_root ufw default deny incoming >/dev/null
  run_as_root ufw default allow outgoing >/dev/null

  # These services are loopback-only. Remove legacy public firewall exceptions.
  for rule in 1433/tcp 5000/tcp 8080/tcp 22/tcp; do
    run_as_root ufw --force delete allow "$rule" >/dev/null 2>&1 || true
  done
  run_as_root ufw --force enable >/dev/null

  ufw_status="$(run_as_root ufw status verbose)"
  grep -q '^Status: active$' <<<"$ufw_status"
  if grep -Eq '^(1433|5000|8080)(/tcp)?[[:space:]]+ALLOW' <<<"$ufw_status"; then
    echo "UFW still allows an internal application port." >&2
    exit 1
  fi
fi

echo "Host SSH and UFW hardening passed."
