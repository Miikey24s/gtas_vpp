# Security policy

## Deployment secrets

GTAS VPP requires deployment secrets to be supplied through environment variables
or the deployment platform secret store. Never commit real values for:

- `DB_SA_PASSWORD`
- `JWT_KEY` (at least 32 UTF-8 bytes)
- GitHub, Radzen, Jira, SSH, or container-registry tokens

The application intentionally fails fast when required authentication secrets are
missing. Application credentials are owned by ASP.NET Core Identity and stored
only as one-way password hashes. The legacy GTAS_MENU/TripleDES login path is
retired and must not be re-enabled as a compatibility shortcut.

## Exposed-secret response

Any credential that has appeared in Git history must be considered compromised:

1. Revoke or rotate it at the provider first.
2. Remove it from current files and replace it with an environment variable.
3. Coordinate a history rewrite only after revocation, then have every contributor
   re-clone or reset to the rewritten branch.
4. Review GitHub push-protection and audit logs after the cleanup.

Do not paste an exposed secret into an issue, commit message, pull request, or chat
while reporting the incident.

## Repository scanning gates

Pull requests and pushes run a current-tree Gitleaks gate through
`scripts/security/Invoke-Gitleaks.ps1`. The script downloads the official Gitleaks
8.30.1 archive, verifies its pinned SHA-256 checksum, and emits fully redacted
findings. It scans tracked files plus not-ignored candidate files; ignored local
secret stores such as `.vscode/mcp.json`, build output, and `.env` are deliberately
not copied into the scan workspace or Docker build context.

Run the same gate locally from PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  scripts/security/Invoke-Gitleaks.ps1 -Mode Current
```

The all-ref history gate is intentionally separate because known historical
credentials must be revoked before history is rewritten. After the provider owner
has recorded revocation, run this read-only verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  scripts/security/Invoke-Gitleaks.ps1 -Mode History -PostRevocation
```

Do not add broad allowlists by directory, commit, rule, or entropy threshold. A
false-positive exception must be limited to the exact reviewed finding, document
its owner, reason, and expiry, and must never suppress a real provider credential.
Known historical findings are not eligible for an allowlist merely to make CI
green.

## Reporting

Report security issues privately to the repository owner. Include the affected
component, reproduction steps, impact, and a suggested mitigation without adding
real credentials or personal data.
