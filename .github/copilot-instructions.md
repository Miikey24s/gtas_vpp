# Copilot Instructions

## Project Guidelines
- For claim-to-DTO mapping, user prefers manual non-generic mapping instead of generic Mapster-based ToDto.
- When deleting data, use soft delete via IsDeleted flag and avoid hard deletes.

## UI Design Preferences (UPDATED 2026-05-08 — User Confirmed)
- **Style**: Enterprise Modern (Atlassian Jira, Salesforce Lightning). Vuông vức, bo cong nhẹ, chuyên nghiệp.
- **Color Palette — Sky Blue/Teal**:
  - Primary 500: #0EA5E9 (sky blue)
  - Primary 600: #0284C7
  - Primary 700: #0369A1
  - Accent Teal: #14B8A6
  - Accent Teal Dark: #0D9488
  - Light Base background: #F7F8F9
  - Light Surface: #FFFFFF
  - Light Text Primary: #172B4D
  - Light Text Secondary: #44526C
  - Dark Base: #0A0A0A
  - Dark Surface: #141414
  - Dark Text Primary: #FAFAFA
- **Light mode default** + Dark mode toggle. Dark class: `.rz-theme-dark`.
- **Border radius**: 6px default (--vpp-radius-md), 8px for modals. NO large rounded corners.
- **Typography**: Poppins (headings) + Inter (body). Weight 600+ for titles.
- **Component style**: Clean cards with border + subtle shadow, solid header with bottom-border, left sidebar navigation. No heavy glassmorphism in light mode.
- **Density**: Balanced (vừa phải).
- **Animation**: Subtle only (fade/slide nhẹ, 150-200ms). No spring animations.
- **Responsive**: 3-tier (mobile <768px, tablet 768-1199px, desktop ≥1200px).
- **Language**: UI supports Vietnamese + English via `@Loc[]` localization keys. Mockups may use Vietnamese for illustration, actual code uses `@Loc["Key"]`.
- **MCP Radzen Blazor**: ALL phases must query MCP for component properties/events/API before writing code.
- CSS uses `--vpp-*` token namespace. Legacy `--vpp-ocean-*` tokens get alias mapping.