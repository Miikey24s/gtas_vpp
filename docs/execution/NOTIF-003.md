# NOTIF-003 — Local SMTP sandbox alternative

- Status: DONE via approved local sandbox alternative
- Path: `ALTERNATIVE` — loopback SMTP capture; no external provider, domain,
  recipient, credential or production mailbox.
- Owner/agent: Codex; D-010 and the active goal authorize the local-first
  release path while real-provider delivery remains conditional.
- Date: 2026-07-17 (Asia/Ho_Chi_Minh)

## Evidence

- `SmtpAccountEmailSender` was exercised with delivery enabled against a
  one-use SMTP listener bound to `127.0.0.1` on an ephemeral port.
- The test completed the SMTP greeting, envelope and `DATA` exchange and
  captured the recipient, subject and deterministic body.
- Assertions verified `RCPT TO:<employee@example.invalid>`, subject
  `GTAS VPP SMTP proof`, and body marker `sandbox-proof-20260717`.
- Targeted Release test result:
  `SmtpAdapter_DeliversMessageToLoopbackSandbox` — `1/1` passed.
- Existing tests continue to prove durable outbox deduplication, retry state and
  sent transitions; the in-app inbox remains authoritative if SMTP is disabled
  or unavailable.

## Boundary

This closes the approved local email-sandbox path only. It does not claim
delivery to a real address, provider/DNS configuration, bounce handling or
production mailbox acceptance. Those remain conditional production evidence.
