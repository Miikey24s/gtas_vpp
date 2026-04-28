# Copilot Instructions

## Project Guidelines
- For claim-to-DTO mapping, user prefers manual non-generic mapping instead of generic Mapster-based ToDto.
- When deleting data, use soft delete via IsDeleted flag and avoid hard deletes.

## UI Design Preferences
- Redesign the Blazor Orders UI in an enterprise/table-focused style with an Ocean Blue palette:
  - Primary: #2563EB
  - Primary Dark: #1D4ED8
  - Primary Light: #DBEAFE
  - Accent: #0EA5E9
- Use a separate order-create route and implement virtualization for large item lists.
- Ensure the VPP request UI text is fully in English.
- Use fuller width layouts with reduced empty side spacing.