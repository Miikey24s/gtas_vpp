# GTAS VPP — Figma Make Context Router

This repository contains backend, Blazor, React, deployment, thesis, and design
artifacts. Do not treat the whole monorepo as one editable Figma project.

For React/Tailwind work, read these files before making any change:

1. `gtas_vpp_fe_react/Guidelines.md`
2. `gtas_vpp_fe_react/AGENTS.md`
3. `docs/design/VPP-PULSE-FIGMA-MAKE-BUILD-BRIEF.md`
4. `docs/design/VPP-PULSE-REACT-FRONTEND-MIGRATION-PLAN.md`

Required preflight:

- fetch the latest `origin/Nam`;
- verify commit `bf2cb10` is an ancestor of the working HEAD;
- create a dedicated `figma/*` branch;
- create or update `plan.md` before application-code edits;
- restrict changes to the approved React slice;
- never push directly to `Nam`, merge, deploy, or expose secrets.

The obsolete `agents/ui-improvements-font-size-alignment` branch and old Figma
chat/code are reference-only. If the current environment cannot read the files
above or is based on older source, stop and request a fresh codebase/context.
