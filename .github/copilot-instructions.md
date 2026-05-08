# Copilot Instructions

## Project Guidelines
- For claim-to-DTO mapping, user prefers manual non-generic mapping instead of generic Mapster-based ToDto.
- When deleting data, use soft delete via IsDeleted flag and avoid hard deletes.

## UI Design Preferences (UPDATED 2026-05-07 — User Confirmed)
- **Style**: Modern Playful (NOT enterprise/table-focused). Reference: Linear.app, Vercel dashboard.
- **Color Palette — Bright Blue/Teal (NOT Ocean Blue #2563EB)**:
  - Primary 500: #0EA5E9 (sky blue)
  - Primary 600: #0284C7
  - Primary 700: #0369A1
  - Accent Teal: #14B8A6
  - Accent Teal Dark: #0D9488
  - Base background: #0A0A0A (dark)
  - Surface: #141414
  - Text Primary: #FAFAFA
  - Text Secondary: #A1A1AA
- **Dark mode default** (not light-first). Light mode as secondary theme.
- **Large rounded corners**: 12px default, 16px for cards, 20px for modals.
- **Typography**: Poppins (headings) + Inter (body). Weight 600+ for titles.
- **Component style**: KPI cards with gradient backgrounds, glassmorphism header, left sidebar navigation.
- **Responsive**: 3-tier (mobile <768px, tablet 768-1199px, desktop ≥1200px).
- **Language**: UI supports Vietnamese + English via `@Loc[]` localization keys. Mockups may use Vietnamese for illustration, actual code uses `@Loc["Key"]`.
- **MCP Radzen Blazor**: ALL phases must query MCP for component properties/events/API before writing code.
- CSS uses `--vpp-*` token namespace. Legacy `--vpp-ocean-*` tokens get alias mapping.