# Security policy

## Deployment secrets

GTAS VPP requires deployment secrets to be supplied through environment variables
or the deployment platform secret store. Never commit real values for:

- `DB_SA_PASSWORD`
- `JWT_KEY` (at least 32 UTF-8 bytes)
- `PASSWORD_ENCRYPTION_KEY` (must match the legacy GTAS_MENU database)
- GitHub, Radzen, Jira, SSH, or container-registry tokens

The application intentionally fails fast when required authentication secrets are
missing. The legacy TripleDES encoder remains only for compatibility with the
existing GTAS_MENU login procedure; new password storage must use a one-way
password hasher and a gradual migration path.

## Exposed-secret response

Any credential that has appeared in Git history must be considered compromised:

1. Revoke or rotate it at the provider first.
2. Remove it from current files and replace it with an environment variable.
3. Coordinate a history rewrite only after revocation, then have every contributor
   re-clone or reset to the rewritten branch.
4. Review GitHub push-protection and audit logs after the cleanup.

Do not paste an exposed secret into an issue, commit message, pull request, or chat
while reporting the incident.

## Reporting

Report security issues privately to the repository owner. Include the affected
component, reproduction steps, impact, and a suggested mitigation without adding
real credentials or personal data.
