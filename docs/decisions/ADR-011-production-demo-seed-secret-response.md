# ADR-011 — DigitalOcean demo state and security ownership

- Status: Accepted operational constraint
- Date: 2026-07-15
- Decision: D-011
- Direct tasks: SEC-001, DEP-002

## Context

A DigitalOcean environment exists and has previously run `MigrateAndSeed`. Legacy compatibility credentials were demo-only, and the project owner can rotate/check accounts. This establishes risk and ownership, not permission for an agent to connect.

## Decision

Treat demo accounts and any exposed/reused secrets as needing P0 audit, rotation/revocation and evidence before server demo. Production-like startup must not seed demo users. Keep secret values out of source, chat, logs and evidence; record only provider/key names, status and safe hashes where needed.

## Consequences and guardrails

- SEC-001 and DEP-002 remain `BLOCKED_EXTERNAL` until explicit server/secret authority is granted in those tasks.
- SEC-002 and DEP-001 may proceed locally without that authority.
- History rewrite is a separate approved operation; rotation is the primary recovery.
