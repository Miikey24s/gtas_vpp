# GTAS VPP — UI/UX Redesign Plan (Chi Tiết)

> **⚠️ DESIGN DIRECTION ĐÃ XÁC NHẬN**: User đã chọn chuyển từ Ocean Blue enterprise sang **Bright Blue/Teal Modern Playful + Dark mode mặc định**. `copilot-instructions.md` sẽ được cập nhật ở PRE-PHASE 0. Code hiện tại dùng `--vpp-ocean-*` sẽ được alias, không break.

---

## TỔNG QUAN QUYẾT ĐỊNH THIẾT KẾ

| Hạng mục | Quyết định |
|----------|------------|
| Phong cách tổng thể | Modern Playful |
| Tone màu chủ đạo | Bright Blue / Teal |
| Hệ nền mặc định | Dark mode |
| Bố cục chính | Sidebar trái |
| Bo góc | Lớn (16-24px card, 12px button) |
| Typography | Poppins (heading) + Inter (body) |
| Icon | Filled / Solid (Material Symbols) |
| Animation | Vừa phải (fade-in, hover scale, shimmer) |
| Sub-navigation | Tabs ngang |
| Data table | Card-style rows + Virtual scrolling |
| Form | Floating labels, Order Create dạng Step Wizard |
| Dashboard | Gradient KPI cards + Grid |
| Sidebar | Dark sidebar |
| Login | Fullscreen Hero |
| Empty state | Illustration vui + CTA |
| Error/404 | Playful illustration |
| Toast | Bottom-right rounded |
| User menu | Rich dropdown panel |
| AI toggle | Ẩn toàn bộ component AI khi OFF |
| Responsive | 3 cấp: Mobile / Tablet / Desktop |
| Accessibility | WCAG 2.1 AA |
| Reference | Linear, Vercel |

---

## GHI CHÚ: GPT 5.4 REVIEW RESPONSE

Đã review plan so với `docs/gpt5.4 review`. Kết luận:

**Giữ nguyên direction từ user** (Modern Playful, Bright Blue/Teal, Dark mode, Vietnamese, bo cong lớn). `copilot-instructions.md` có hướng cũ (Ocean Blue #2563EB, enterprise, English-only) — đã bị override bởi 27 câu trả lời thiết kế từ user.

**3 điểm cần sửa trong plan**:
1. **Virtualization**: Tab_History, Tab_AdminApproval, Tab_PagePermission đã dùng `AllowVirtualization` → Phase 9 không tạo mới từ đầu, mà nâng cấp + thêm vào các grid còn thiếu
2. **Order Create wizard**: Hiện có 3 mode (New Order, Copy Previous, Request Additional) qua query params → Wizard phải bắt đầu bằng "Chọn mode tạo đơn", không phải "Chọn kỳ" tự do
3. **Notification migration**: 61 `NotificationService.Notify()` call sites trực tiếp → Phase 11 cần migration strategy 2 bước: wrap service → migrate call sites

## GHI CHÚ: SONNET 4.6 REVIEW RESPONSE

Đã review plan so với `docs/sonnet4.6 review`. Bổ sung 4 điểm:

1. **Tab_Orders thực tế là card-based**: Đã dùng manual card rendering + nested DataGrid có `AllowVirtualization`. Plan sửa thành "enhance style card", không thêm virtualization vào main grid (đã không tồn tại)
2. **Order Create state management**: Đã thêm `OrderCreateContext` ViewModel + CascadingValue + draft auto-save xuyên suốt 3 step + edit mode
3. **Merge 3 toggle components**: ThemeToggle + LanguageToggle + AIStatusToggle gộp thành `HeaderControls.razor`
4. **Reorder**: Phase 7 (Login) lên trước Phase 6 (Dashboard) vì là first impression

## PRE-PHASE 0 — CẬP NHẬT COPILOT INSTRUCTIONS

Trước khi bắt đầu code, cập nhật `.github/copilot-instructions.md`:
```markdown
## UI Design Preferences (UPDATED 2026-05-07)
- Style: Modern Playful with large border-radius (16-24px), smooth animations
- Primary: Bright Blue / Teal (#00BCD4 → #0097A7)
- Dark mode as default, with light mode toggle
- Typography: Poppins (headings) + Inter (body)
- Reference: Linear, Vercel design aesthetics
- Bilingual: Vietnamese primary, English secondary
```

## GHI CHÚ QUAN TRỌNG VỀ NGÔN NGỮ TRONG PLAN

**Các mockup/text trong plan dùng tiếng Việt chỉ để minh họa cho user.** Trong code thực tế, **100% UI text dùng pattern `@Loc["Key"]`** (i18n resource-based), text gốc tiếng Anh, hệ thống i18n tự động render tiếng Việt khi user chọn ngôn ngữ VI. Mỗi phase sẽ có bảng resource keys mới cần thêm vào file `.resx`.

---

## KIẾN TRÚC HIỆN TẠI (ĐIỂM CẦN CẢI THIỆN)

### Vấn đề kiến trúc
1. **LeftSidebar monolithic**: 152 dòng Razor + 186 dòng C# gộp tất cả (nav, header, theme, language, AI toggle, user menu, clock)
2. **app.css 1640 dòng**: Lẫn lộn style cũ từ dự án khác + Radzen fixes + VPP tokens. Không có design system rõ ràng
3. **Radzen theme mặc định**: Material3 chưa được custom sâu, chỉ dùng biến `--rz-primary`
4. **Không có component library riêng**: Không có button, card, badge, input style nhất quán
5. **AI toggle đơn giản**: `glb.IsAIEnabled` ẩn/hiện nhưng không có transition
6. **DataGrid optimization chưa đồng đều**: Tab_History, Tab_AdminApproval, Tab_PagePermission đã có `AllowVirtualization`, nhưng Tab_Orders, Tab_ProductCatalog thì chưa
7. **Font chưa tối ưu**: Montserrat + Open Sans/Poppins đang import từ Google Fonts CDN
8. **Responsive cơ bản**: Chỉ collapse sidebar, chưa có mobile/tablet/desktop rõ ràng
9. **61 NotificationService.Notify() call sites**: Rải rác khắp codebase, chưa có wrapper thống nhất (đã có CustomNotificationService nhưng chỉ dùng 1 phần)

---

## PHASE 1 — DESIGN TOKEN FOUNDATION (Nền tảng thiết kế)

### Mục tiêu
Thiết lập hệ thống CSS custom properties làm nền tảng cho toàn bộ UI, đảm bảo nhất quán và dễ bảo trì.

### File thay đổi
| File | Action | Mô tả |
|------|--------|-------|
| `wwwroot/css/tokens.css` | **CREATE** | CSS variables design system |
| `wwwroot/css/typography.css` | **CREATE** | Font-face, typography scale |
| `wwwroot/app.css` | **REFACTOR** | Import tokens, xóa style cũ không dùng |

### Design Tokens chi tiết
```css
/* tokens.css */
:root {
  /* ═══ PRIMARY PALETTE (Bright Blue/Teal) ═══ */
  --vpp-primary-50:  #e0f7fa;
  --vpp-primary-100: #b2ebf2;
  --vpp-primary-200: #80deea;
  --vpp-primary-300: #4dd0e1;
  --vpp-primary-400: #26c6da;
  --vpp-primary-500: #00bcd4;  /* Teal base */
  --vpp-primary-600: #00acc1;
  --vpp-primary-700: #0097a7;
  --vpp-primary-800: #00838f;
  --vpp-primary-900: #006064;
  
  /* Secondary accent (warm contrast) */
  --vpp-accent-400: #ff6e40;
  --vpp-accent-500: #ff5722;
  
  /* ═══ DARK MODE SURFACES ═══ */
  --vpp-bg-base:       #0a0e17;
  --vpp-bg-elevated:   #111827;
  --vpp-bg-surface:    #1a2332;
  --vpp-bg-card:       #1e293b;
  --vpp-bg-hover:      #263348;
  --vpp-bg-sidebar:    #080c14;
  
  /* ═══ TEXT ═══ */
  --vpp-text-primary:    #f1f5f9;
  --vpp-text-secondary:  #94a3b8;
  --vpp-text-disabled:   #64748b;
  --vpp-text-on-primary: #ffffff;
  
  /* ═══ BORDERS ═══ */
  --vpp-border-subtle:  #1e293b;
  --vpp-border-default: #334155;
  --vpp-border-strong:  #475569;
  
  /* ═══ SHADOWS ═══ */
  --vpp-shadow-sm:  0 1px 2px rgba(0,0,0,0.3);
  --vpp-shadow-md:  0 4px 12px rgba(0,0,0,0.4);
  --vpp-shadow-lg:  0 8px 32px rgba(0,0,0,0.5);
  --vpp-shadow-glow: 0 0 20px rgba(0,188,212,0.15);
  
  /* ═══ BORDER RADIUS ═══ */
  --vpp-radius-sm:   8px;
  --vpp-radius-md:   12px;
  --vpp-radius-lg:   16px;
  --vpp-radius-xl:   24px;
  --vpp-radius-full: 9999px;
  
  /* ═══ SPACING ═══ */
  --vpp-space-xs: 0.25rem;
  --vpp-space-sm: 0.5rem;
  --vpp-space-md: 1rem;
  --vpp-space-lg: 1.5rem;
  --vpp-space-xl: 2rem;
  --vpp-space-2xl: 3rem;
  
  /* ═══ TRANSITIONS ═══ */
  --vpp-transition-fast:   150ms cubic-bezier(0.4, 0, 0.2, 1);
  --vpp-transition-normal: 250ms cubic-bezier(0.4, 0, 0.2, 1);
  --vpp-transition-slow:   400ms cubic-bezier(0.4, 0, 0.2, 1);
  
  /* ═══ TYPOGRAPHY SCALE ═══ */
  --vpp-font-display: 'Poppins', sans-serif;
  --vpp-font-body:    'Inter', sans-serif;
  --vpp-font-mono:    'JetBrains Mono', monospace;
  
  --vpp-text-xs:   0.75rem;
  --vpp-text-sm:   0.875rem;
  --vpp-text-base: 1rem;
  --vpp-text-lg:   1.125rem;
  --vpp-text-xl:   1.25rem;
  --vpp-text-2xl:  1.5rem;
  --vpp-text-3xl:  1.875rem;
  --vpp-text-4xl:  2.25rem;
  
  /* ═══ Z-INDEX SCALE ═══ */
  --vpp-z-sidebar:   40;
  --vpp-z-header:    50;
  --vpp-z-dropdown:  100;
  --vpp-z-modal:     200;
  --vpp-z-toast:     300;
  --vpp-z-tooltip:   400;
}

/* Light mode overrides */
[data-theme="light"] {
  --vpp-bg-base:       #f8fafc;
  --vpp-bg-elevated:   #f1f5f9;
  --vpp-bg-surface:    #ffffff;
  --vpp-bg-card:       #ffffff;
  --vpp-bg-hover:      #e2e8f0;
  --vpp-bg-sidebar:    #0f172a; /* sidebar luôn dark */
  
  --vpp-text-primary:    #0f172a;
  --vpp-text-secondary:  #475569;
  --vpp-text-disabled:   #94a3b8;
  
  --vpp-border-subtle:  #e2e8f0;
  --vpp-border-default: #cbd5e1;
  --vpp-shadow-sm:  0 1px 2px rgba(0,0,0,0.05);
  --vpp-shadow-md:  0 4px 12px rgba(0,0,0,0.08);
}
```

### Typography Setup
```css
/* typography.css */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Poppins:wght@500;600;700;800&display=swap');

body {
  font-family: var(--vpp-font-body);
  font-size: var(--vpp-text-sm);
  line-height: 1.6;
  color: var(--vpp-text-primary);
  background-color: var(--vpp-bg-base);
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

h1, h2, h3, h4, h5, h6 {
  font-family: var(--vpp-font-display);
  font-weight: 600;
  letter-spacing: -0.02em;
}

h1 { font-size: var(--vpp-text-4xl); }
h2 { font-size: var(--vpp-text-3xl); }
h3 { font-size: var(--vpp-text-2xl); }
h4 { font-size: var(--vpp-text-xl); }
```

### Font Migration Map (đã grep verify)
| Font/Icon hiện tại | Load ở đâu | Usage trong code | Action |
|---------------------|-----------|------------------|--------|
| **Montserrat** | App.razor:14 | ❌ Không dùng ở đâu | **Xóa** `<link>`, thay bằng Poppins |
| **Open Sans** | App.razor:16 | ❌ Không dùng ở đâu | **Xóa** `<link>`, thay bằng Inter |
| **Poppins (cũ)** | App.razor:16 | ❌ Chỉ load, không dùng | **Xóa** `<link>` cũ, thay bằng Poppins+Inter mới |
| **Boxicons** | App.razor:13 | ❌ Không dùng (0 `bx-` class) | **Xóa** `<link>` |
| **FontAwesome** | App.razor:38 | ❌ Không dùng (0 `fa-` class) | **Xóa** `<script>` kit |
| **Material Symbols** | App.razor:15 | ✅ Dùng khắp nơi | **Giữ** (Radzen dùng) |

**Kết luận**: Xóa 4 font/icon sets không dùng → giảm ~200KB bandwidth. Chỉ giữ Material Symbols + thêm Poppins + Inter mới.

### Kế hoạch thực hiện Phase 1
1. Tạo `wwwroot/css/tokens.css` — hệ thống `--vpp-*` mới
2. Tạo `wwwroot/css/typography.css` — font Poppins + Inter
3. Sửa `wwwroot/app.css` — import 2 file trên, alias `--vpp-ocean-*` → `--vpp-primary-*`, xóa ~400 dòng CSS cũ
4. Map `--vpp-primary-*` variables sang Radzen `--rz-*` variables qua CSS cascade
5. Update `App.razor` — bỏ Google Fonts cũ (Montserrat, Open Sans), load Poppins + Inter

### Migration path cho `--vpp-ocean-*` tokens
Codebase hiện dùng `--vpp-ocean-primary`, `--vpp-ocean-primary-dark`, `--vpp-ocean-primary-light`, `--vpp-ocean-surface-soft` ở `app.css` và `Page_OrderCreate.razor`. **Không xóa**, mà **alias**:
```css
:root {
  --vpp-ocean-primary: var(--vpp-primary-500);           /* was: var(--rz-primary) */
  --vpp-ocean-primary-dark: var(--vpp-primary-700);      /* was: var(--rz-primary-dark) */
  --vpp-ocean-primary-light: var(--vpp-primary-300);     /* was: var(--rz-primary-light) */
  --vpp-ocean-surface-soft: var(--vpp-bg-hover);         /* was: var(--rz-base-200) */
}
```
→ Các file dùng `--vpp-ocean-*` tiếp tục hoạt động, không break. Sau này sẽ migrate dần sang `--vpp-primary-*`.

### App.css cleanup targets (cụ thể)
Xóa ~400 dòng CSS blocks từ dự án cũ (tiêu thụ — consumption module):

| Lines | Block | Lý do |
|-------|-------|-------|
| 260-318 | `.box`, `.box .square` | Login glassmorphism cũ |
| 320-328 | `@keyframes animate` | Animation cho `.square` cũ |
| 330-398 | `.form`, `.form h2`, `.inputBox` | Form login cũ (đã dùng Radzen) |
| 400-460 | `.divbox`, `.logoppj` | Login UI cũ |
| 463-477 | `.lg1container` | Login container cũ |
| 488-510 | `.reportbutton` | Tiêu thụ button |
| 532-546 | `.un-linkbtn`, `.followupbtn`... | Tiêu thụ module |
| 619-660 | `.background-buy/wfx/pur/...` | Color classes (tất cả đã comment out) |
| 667-694 | `.ppjsidebarmenu.reddot`... | Sidebar badges cũ |

**Giữ lại**: Radzen FormField fixes (border, outlined, floating label), VPP design tokens cuối file, shimmer skeleton, user dropdown, sidebar collapse, dark mode transition.

---

## PHASE 2 — CUSTOM RADZEN THEME (Bright Blue/Teal)

### Mục tiêu
Custom Radzen Material3 theme sang Bright Blue/Teal, đồng bộ với design tokens.

### Cách làm
Radzen Blazor có `ThemeService.SetTheme()` nhận tên theme. Theme được định nghĩa trong file CSS `material3-base.css`. Ta sẽ:

**Phương án A (Khuyến nghị)**: Ghi đè Radzen CSS variables bằng CSS cascade từ `tokens.css`
```css
:root {
  --rz-primary: var(--vpp-primary-500);
  --rz-primary-dark: var(--vpp-primary-700);
  --rz-primary-light: var(--vpp-primary-300);
  --rz-primary-lighter: var(--vpp-primary-100);
  --rz-secondary: var(--vpp-accent-500);
  
  --rz-body-background-color: var(--vpp-bg-base);
  --rz-panel-background-color: var(--vpp-bg-surface);
  --rz-card-background-color: var(--vpp-bg-card);
  
  --rz-border-radius: var(--vpp-radius-md);
  --rz-box-shadow-1: var(--vpp-shadow-sm);
  --rz-box-shadow-2: var(--vpp-shadow-md);
  /* ... override all critical Radzen variables */
}
```

**Phương án B**: Generate Radzen theme mới từ Radzen Blazor Studio hoặc Radzen online theme builder, lưu thành file CSS.

→ **Dùng Phương án A** vì kiểm soát được toàn bộ và nhanh hơn.

### File thay đổi
| File | Action | Mô tả |
|------|--------|-------|
| `wwwroot/css/radzen-overrides.css` | **CREATE** | Override tất cả `--rz-*` variables |
| `wwwroot/app.css` | **EDIT** | Import radzen-overrides.css |
| `Components/App.razor` | **EDIT** | Load CSS theo đúng thứ tự |

---

## PHASE 3 — MAIN LAYOUT RESTRUCTURE

### Mục tiêu
Tái cấu trúc MainLayout để tách biệt header, sidebar, body. Chuẩn bị cho responsive 3 cấp.

### Giải pháp
Tách `LeftSidebar.razor` thành các component nhỏ:

```
Components/Layout/
├── MainLayout.razor          (giữ, chỉnh sửa)
├── LayoutHeader.razor        (NEW - tách từ LeftSidebar)
├── LayoutHeader.razor.cs
├── LayoutSidebar.razor       (NEW - tách từ LeftSidebar)  
├── LayoutSidebar.razor.cs
├── HeaderControls.razor      (NEW - gộp ThemeToggle + LanguageToggle + AIStatusToggle)
├── HeaderControls.razor.cs
├── UserMenu.razor            (NEW - user dropdown riêng)
├── UserMenu.razor.cs
├── LeftSidebar.razor         (DEPRECATED - giữ lại redirect hoặc xóa)
├── LeftSidebar.razor.cs
├── LoginLayout.razor         (giữ, chỉnh style)
├── RightSidebar.razor        (dùng cho AI panel hoặc context details)
└── RightSidebar.razor.cs
```

> **Ghi chú từ sonnet 4.6 review**: ThemeToggle + LanguageToggle + AIStatusToggle mỗi cái chỉ vài dòng HTML + 1 method → gộp thành `HeaderControls.razor` duy nhất thay vì 3 component riêng.

### MainLayout mới
```razor
@* MainLayout.razor *@
<RadzenLayout Style="grid-template-columns: auto 1fr; grid-template-rows: 56px 1fr;">
    <LayoutHeader />        @* Row 1, Col 1-2 *@
    <LayoutSidebar />       @* Row 2, Col 1 *@
    <RadzenBody>            @* Row 2, Col 2 *@
        @Body
    </RadzenBody>
</RadzenLayout>

@* AI ChatBox: chỉ render khi AI enabled *@
@if (glb.IsAIEnabled) {
    <AIChatBox />
}
```

### LayoutHeader
- Fixed top, height 56px
- Logo + Tên app bên trái
- Các toggle (Theme, Language, AI) + Notification + UserMenu bên phải
- Dark background (`var(--vpp-bg-sidebar)`)
- Border-bottom subtle
- Z-index cao hơn content

### Responsive Strategy (3 cấp)
```
Desktop (≥1280px):
  - Sidebar full: 260px, icon + text
  - Content margin-left: 260px

Tablet (768px - 1279px):
  - Sidebar collapsed: 64px, icon only
  - Hover mở rộng tạm thời (flyout)
  - Content margin-left: 64px

Mobile (<768px):
  - Sidebar ẩn hoàn toàn
  - Hamburger menu mở overlay fullscreen
  - Bottom navigation bar cho tác vụ chính (Orders, Library, Settings)
```

---

## PHASE 4 — SIDEBAR REDESIGN

### Mục tiêu
Sidebar dark, hiện đại, với active indicator, icon filled, animation mượt.

### Thiết kế chi tiết

**Màu sắc**:
- Background: `var(--vpp-bg-sidebar)` = `#080c14` (tối hơn nền chính)
- Item hover: `rgba(255,255,255,0.06)` + dịch phải 4px
- Item active: `var(--vpp-primary-500)` background mờ + left border indicator 3px
- Text: `var(--vpp-text-secondary)`, active: `var(--vpp-text-on-primary)`
- Icon: filled, 20px

**Cấu trúc menu**:
```
┌─────────────────────────┐
│ [PPJ Logo]  GTAS VPP   │  ← Logo area, 56px
├─────────────────────────┤
│ 📊 Dashboard        ▸  │  ← Parent item
│   🛒 My Orders         │  ← Child indent 16px
│   📋 History           │
│   📦 Product Catalog   │
│   🏢 Dept Summary      │
│   📄 All Orders        │
│   ✅ Admin Approval    │
├─────────────────────────┤
│ 📚 Library          ▸  │
│   🏷️ Class Definitions │
│   📂 Categories        │
│   ⚙️ Operations        │
│   🚚 Suppliers         │
│   🏛️ Departments       │
├─────────────────────────┤
│ 📈 Reports              │  ← Leaf item
│ ⚙️ Permissions      ▸  │
│ 🤖 AI Management    ▸  │  ← Conditional (glb.IsAIEnabled)
├─────────────────────────┤
│                         │
│      🕐 07-05-2026     │  ← Clock footer
│        14:30           │
└─────────────────────────┘
```

**Animation**:
- Item hover: background fade 150ms + translateX(4px)
- Submenu mở/đóng: max-height transition 250ms ease
- Active indicator: width scale từ 0→100% trong 200ms
- Icon rotate khi expand (▸ → ▾)

**Collapse behavior**:
- Collapsed width: 64px
- Icon centered, tooltip hiện khi hover
- Click icon mở flyout panel nhỏ

### File thay đổi
| File | Action |
|------|--------|
| `Components/Layout/LayoutSidebar.razor` | **CREATE** |
| `Components/Layout/LayoutSidebar.razor.cs` | **CREATE** |
| `Components/Layout/LayoutSidebar.razor.css` | **CREATE** |
| `Components/Layout/LeftSidebar.razor` | **EDIT** (redirect hoặc deprecated) |

### Menu Data Model (cho phép render từ config)
```csharp
public class NavMenuItem
{
    public string Text { get; set; }
    public string Icon { get; set; }
    public string Path { get; set; }
    public string Policy { get; set; }
    public List<NavMenuItem> Children { get; set; }
    public bool IsDivider { get; set; }
}
```

---

## PHASE 5 — HEADER BAR REDESIGN

### Mục tiêu
Header bar tối giản, glassmorphism nhẹ, chứa các toggle và user menu.

### Thiết kế
```
┌──────────────────────────────────────────────────────────────┐
│ ☰  [Logo]  GTAS VPP (SERVER LIVE)     🌐 ⚙️🤖 🔔 👤       │
└──────────────────────────────────────────────────────────────┘
```

- **Hamburger**: Ẩn/hiện sidebar
- **Logo**: PPJ logo 32px
- **App title**: "GTAS VPP (SERVER {name})" — font Poppins 600
- **Language**: Nút tròn VI/EN, active glow xanh
- **AI Toggle**: Icon smart_toy + switch, conditional render
- **Theme**: Icon dark_mode/light_mode
- **Notification**: Bell icon + badge đỏ (số thông báo)
- **User Avatar**: 36px circle, gradient background từ primary color, initials

### User Menu Dropdown (Rich)
```
┌──────────────────────────┐
│  [Avatar]  Nguyễn Văn A │  ← Header
│            Phòng IT      │
├──────────────────────────┤
│  🏢 Phòng IT             │  ← Department
│  📧 user@email.com       │  ← Email
├──────────────────────────┤
│  ⚙️  Settings            │
│  🔑  Change Password     │
├──────────────────────────┤
│  🚪  Logout              │  ← Màu đỏ
└──────────────────────────┘
```

Animation: scale(0.95)→scale(1) + opacity 0→1, duration 150ms

### File thay đổi
| File | Action |
|------|--------|
| `Components/Layout/LayoutHeader.razor` | **CREATE** |
| `Components/Layout/LayoutHeader.razor.cs` | **CREATE** |
| `Components/Layout/HeaderControls.razor` | **CREATE** (gộp Theme + Language + AI toggle) |
| `Components/Layout/HeaderControls.razor.cs` | **CREATE** |
| `Components/Layout/UserMenu.razor` | **CREATE** |

---

## PHASE 6 — DASHBOARD REDESIGN

### Mục tiêu
Dashboard "My Orders" với KPI gradient cards + card-style data grid.

### Thiết kế KPI Cards
```
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│ 📅 Active Period    │  │ ⏰ Period End       │  │ 📋 Total Lines     │  │ 📦 Total Quantity   │
│                     │  │                     │  │                     │  │                     │
│ Tháng 05/2026       │  │ 05/05/2026          │  │ 12                  │  │ 156                 │
│ ↑ 3 so với tháng trc│  │ Còn 3 ngày          │  │ ↑ 20%               │  │ ↓ 5%                │
└─────────────────────┘  └─────────────────────┘  └─────────────────────┘  └─────────────────────┘
```

Mỗi card:
- Background: gradient 135deg từ primary-600 → primary-800
- Icon: to bên trái, opacity 0.2 (chìm xuống background)
- Số liệu: font Poppins 700, size 2xl, màu trắng
- Label: text-xs, opacity 0.8
- Trend indicator: mũi tên ↑↓ + phần trăm, màu success/red
- Hover: scale(1.02) + shadow glow

**Các biến thể màu**:
```css
.kpi-blue   { background: linear-gradient(135deg, #00bcd4, #0097a7); }
.kpi-purple { background: linear-gradient(135deg, #7c3aed, #5b21b6); }
.kpi-emerald{ background: linear-gradient(135deg, #10b981, #059669); }
.kpi-amber  { background: linear-gradient(135deg, #f59e0b, #d97706); }
```

### Chart Section
- Column chart: Monthly orders với gradient fill (primary-500→primary-300)
- Chart card: background `var(--vpp-bg-card)`, border-radius 16px
- Tooltip: dark background, bo tròn, shadow
- Animation: bars grow từ 0 khi vào viewport (IntersectionObserver)

### Card-Style Data Grid
Thay vì bảng truyền thống, mỗi đơn hàng là 1 card:
```
┌──────────────────────────────────────────────┐
│ 📋 Đơn hàng #DH2026042    [Đã duyệt] ✅     │
│ 📅 01/05/2026  │  👤 Nguyễn Văn A           │
│ 📦 5 dòng      │  📊 120 sản phẩm           │
│                              [Xem chi tiết →]│
└──────────────────────────────────────────────┘
```

Status badge dùng RadzenBadge với màu semantic:
- Submitted: blue
- Approved: green
- Rejected: red
- Pending: amber

### Virtual Scrolling cho Data Lớn
Sử dụng `Virtualize` component của Blazor:
```razor
<Virtualize Items="orders" Context="order" ItemSize="120" OverscanCount="5">
    <OrderCard Order="order" />
</Virtualize>
```

Kết hợp với Radzen DataGrid `LoadData` event cho server-side pagination.

### File thay đổi
| File | Action |
|------|--------|
| `Components/Pages/VPPRequest/Tabs/Tab_Orders.razor` | **REWRITE** |
| `Components/Pages/VPPRequest/Tabs/Tab_Orders.razor.cs` | **REWRITE** |
| `Components/Shared/KpiCard.razor` | **CREATE** |
| `Components/Shared/KpiCard.razor.cs` | **CREATE** |
| `Components/Shared/OrderCard.razor` | **CREATE** |
| `Components/Shared/StatusBadge.razor` | **CREATE** |
| `Components/Shared/StatGrid.razor` | **CREATE** |

---

## PHASE 7 — LOGIN PAGE REDESIGN (Fullscreen Hero)

### Mục tiêu
Fullscreen hero page với background animation/particles + glassmorphism login card.

### Thiết kế
```
┌──────────────────────────────────────────────────────┐
│                                                      │
│    ·  ·    ·  ·   ·  (animated particles/dots)       │
│       ·  ·     ·  ·    ·                             │
│                                                      │
│          ┌──────────────────────┐                    │
│          │   [PPJ Logo]         │                    │
│          │                      │                    │
│          │   GTAS VPP           │                    │
│          │   Văn Phòng Phẩm     │                    │
│          │                      │                    │
│          │   👤 [Username    ]  │                    │
│          │   🔑 [Password    ]  │                    │
│          │   🖥️ [Server     ▾]  │                    │
│          │                      │                    │
│          │   [  ĐĂNG NHẬP  ]   │                    │
│          │                      │                    │
│          └──────────────────────┘                    │
│                                                      │
│  ·  ·     ·  ·   ·    ·  ·   ·                       │
└──────────────────────────────────────────────────────┘
```

**Background**:
- Dark gradient: `#0a0e17` → `#111827` → `#1a2332`
- Animated particles: Canvas hoặc CSS dots floating chậm (parallax nhẹ)
- Subtle grid pattern (như Linear/Vercel)

**Login Card**:
- Glassmorphism: `background: rgba(30,41,59,0.7); backdrop-filter: blur(20px)`
- Border: `1px solid rgba(255,255,255,0.1)`
- Border-radius: 24px
- Shadow: `0 25px 50px rgba(0,0,0,0.5)`
- Width: 420px, centered

**Input Style**:
- Floating labels (Material style) nhưng với viền bo tròn lớn
- Icon bên trái mỗi input
- Focus: border glow xanh teal
- Error: border đỏ + shake animation

**Button**:
- Gradient: primary-500 → primary-700
- Border-radius: 12px
- Height: 48px
- Font Poppins 600
- Hover: scale(1.02) + shadow glow
- Loading: spinner trong button

### Animation
- Card xuất hiện: opacity 0→1 + translateY(20px)→0, duration 600ms
- Particles: float ngẫu nhiên, duration 15-25s
- Input focus: border-color transition 200ms

### File thay đổi
| File | Action |
|------|--------|
| `Components/Pages/Authen/LoginPage.razor` | **REWRITE** |
| `Components/Pages/Authen/LoginPage.razor.cs` | **REWRITE** |
| `Components/Pages/Authen/LoginPage.razor.css` | **REWRITE** |
| `Components/Pages/Authen/LoginLayout.razor` | **EDIT** |

---

## PHASE 8 — ORDER CREATE FORM (Step Wizard)

### Mục tiêu
Chuyển form tạo đơn dài (Page_OrderCreate, 198 dòng Razor, 693 dòng C#) thành step-by-step wizard.

### Các bước (ĐÃ CHỈNH SỬA — tôn trọng 3 mode hiện tại)

Hiện tại Page_OrderCreate nhận mode qua query params (`IsAdditionalParam`, `CopyFromParam`):
- **New Order**: Tạo đơn mới trong kỳ hiện tại
- **Copy Previous**: Sao chép đơn từ kỳ trước
- **Additional Order**: Đơn bổ sung sau deadline, cần admin duyệt

Wizard phải bắt đầu bằng mode context, KHÔNG phải "Chọn kỳ" tự do:

```
Step 1: Context & Review  →  Step 2: Chọn/Chỉnh Sản Phẩm  →  Step 3: Review & Submit
┌─────────────────────┐   ┌──────────────────────────┐   ┌─────────────────────┐
│ 📋 Order Mode:      │   │ 🔍 Tìm kiếm sản phẩm    │   │ 📋 Tóm tắt đơn hàng │
│ ○ New Order (mặc đ) │   │ 📋 Danh sách sản phẩm    │   │ • Sp 1: Bút bi x5   │
│                     │   │ 🤖 AI gợi ý (nếu ON)     │   │ • Sp 2: Giấy A4 x2  │
│ Kỳ: Tháng 05/2026   │   │                          │   │                      │
│ Hạn: 05/05/2026     │   │ [Đã chọn: 3 sp]          │   │ Mode: New Order     │
│                     │   │                          │   │                      │
│ Mode: New Order    │   │                          │   │ [Quay lại] [Gửi đơn]│
└─────────────────────┘   └──────────────────────────┘   └─────────────────────┘

Khi Copy Previous: Step 1 hiển thị đơn cũ, Step 2 đã pre-select sản phẩm cũ
Khi Additional: Step 1 hiển thị warning "đã quá hạn, cần admin duyệt"
```

### UX Flow
1. **Step indicator**: Progress bar trên cùng (3 bước), step hiện tại glow
2. **Step 1**: Hiển thị context (mode, kỳ, hạn, warning nếu additional), nút "Tiếp tục"
3. **Step 2**: Product catalog + AI search (nếu ON), quản lý selected items
4. **Step 3**: Review tất cả selected items, tổng quantity, nút Submit
5. **Navigation**: Nút "Tiếp tục" / "Quay lại" ở bottom
6. **Validation**: Validate từng step trước khi cho đi tiếp
7. **Draft auto-save**: localStorage mỗi 8 giây (giữ nguyên)
8. **Mobile**: Mỗi step full-width, swipe để chuyển step

### Radzen Steps Component
```razor
<RadzenSteps @bind-SelectedIndex="currentStep" Change="@OnStepChange">
    <Steps>
        <RadzenStep Text="@Loc["OrderContext"]">
            <Step1 Context="@context" />
        </RadzenStep>
        <RadzenStep Text="@Loc["SelectProducts"]">
            <Step2 ProductList AIEnabled="@glb.IsAIEnabled" />
        </RadzenStep>
        <RadzenStep Text="@Loc["ReviewSubmit"]">
            <Step3 ReviewAndSubmit />
        </RadzenStep>
    </Steps>
</RadzenSteps>
```

### AI Integration trong Wizard
Khi AI ON:
- Step 2 có thêm card AI search với gradient tím/xanh
- Hiển thị suggested products dạng chip
- Click chip để thêm nhanh vào đơn hàng

Khi AI OFF:
- Card AI search hoàn toàn biến mất
- Layout reflow mượt, không để lại khoảng trống

### State Management giữa các Step (THÊM MỚI — từ sonnet 4.6 review)

**Vấn đề**: 3 step components (`Step1`, `Step2`, `Step3`) cần chia sẻ: `selectedItems`, `periodInfo`, `orderMode`, `IsAdditional`, `IsCopyFromPrevious`, `OrderId`.

**Giải pháp**: Dùng **parent ViewModel object** — `OrderCreateContext`:
```csharp
public class OrderCreateContext
{
    public string Mode { get; set; }           // "new" | "copy" | "additional"
    public Guid? EditOrderId { get; set; }     // non-null khi edit
    public Guid? CopyFromId { get; set; }
    public bool IsAdditional { get; set; }
    
    public PeriodInfo? PeriodInfo { get; set; }
    public List<SelectedItem> SelectedItems { get; set; } = new();
    
    // Event notifications cho step change
    public event Action? OnStateChanged;
    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}
```
- Context được tạo trong `Page_OrderCreate.OnInitializedAsync()` dựa trên query params
- Truyền qua `CascadingValue` cho cả 3 step
- Mỗi step có thể đọc/ghi context
- Step change gọi `context.NotifyStateChanged()` để step khác re-render nếu cần

### Edit Mode (THÊM MỚI)
Khi `OrderId != null` (edit đơn tồn tại):
- Step 1: Hiển thị "Editing Order #CODE" + thông tin đơn hiện tại
- Step 2: Pre-load các sản phẩm đã chọn
- Step 3: Hiển thị diff (added/removed items) thay vì full list

### Draft Auto-Save Across Steps
PeriodicTimer 8s chạy ở Page_OrderCreate parent, subscribe context.OnStateChanged:
```csharp
private async Task AutoSaveDraft(OrderCreateContext ctx)
{
    var draft = new { ctx.Mode, ctx.SelectedItems, ctx.PeriodInfo };
    await ProtectedLocalStore.SetAsync("order_draft", draft);
}
```

### File thay đổi
| File | Action |
|------|--------|
| `Components/Pages/VPPRequest/Page_OrderCreate.razor` | **REFACTOR** (extract UI → step components, giữ logic core 693 dòng) |
| `Components/Pages/VPPRequest/Page_OrderCreate.razor.cs` | **REFACTOR** (giữ logic + thêm OrderCreateContext, không rewrite) |
| `Components/Pages/VPPRequest/Page_OrderCreate_Step1.razor` | **CREATE** |
| `Components/Pages/VPPRequest/Page_OrderCreate_Step2.razor` | **CREATE** |
| `Components/Pages/VPPRequest/Page_OrderCreate_Step3.razor` | **CREATE** |

---

## PHASE 9 — DATA TABLE UPGRADE (Card-Style + Virtual Scroll Enhancement)

### Mục tiêu
Card-style rows cho dashboard/list views. Nâng cấp các DataGrid chưa có virtualization. Tận dụng `AllowVirtualization` đã có. **Kèm theo: Full-stack data optimization cho VPP01, VPP02, L04_VPP.**

### Data-heavy Analysis
| Bảng | Data size | Vấn đề |
|------|-----------|--------|
| VPP01_RequestHeader | Hàng chục nghìn | Load tất cả về FE, filter/sort client-side |
| VPP02_RequestDetail | Hàng trăm nghìn | Join VPP01+L04_VPP, load chi tiết từng đơn |
| L04_VPP | Hàng nghìn SP | Category join, full-text search, load toàn bộ catalog |

### BE Optimization

**DB Indexes (kiểm tra + tạo nếu thiếu)**:
```sql
CREATE INDEX IX_VPP01_Period_UserId ON VPP01_RequestHeader(Period, UserId, Status, IsDeleted);
CREATE INDEX IX_VPP01_SubmittedDate ON VPP01_RequestHeader(SubmittedDate, Status) WHERE IsDeleted = 0;
CREATE INDEX IX_VPP02_RequestId ON VPP02_RequestDetail(VPPRequestId) INCLUDE (VPPId, Qty);
CREATE INDEX IX_L04_VPP_Category_Search ON L04_VPP(CategoryId, IsDeleted) INCLUDE (VPPName, VPPCode);
```

**EF Core Query Rules (áp dụng toàn bộ Service layer)**:
- `AsNoTracking()` cho mọi query read-only → giảm 30-40% memory
- Projection `.Select(x => new Dto {...})` — chỉ lấy fields cần, không load full entity
- `AsSplitQuery()` cho query join nhiều bảng → tránh Cartesian explosion
- Pagination: luôn `.Skip().Take()` với `CountAsync()` riêng

**API Response Compression (Program.cs)**:
```csharp
builder.Services.AddResponseCompression(opts => {
    opts.EnableForHttps = true;
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
```

### FE Optimization

**Debounced Search 300ms** (mọi search box trong grid):
```csharp
private CancellationTokenSource? _searchCts;
private async Task DebouncedSearch() {
    _searchCts?.Cancel(); _searchCts = new CancellationTokenSource();
    try { await Task.Delay(300, _searchCts.Token); await LoadData(); }
    catch (TaskCanceledException) { }
}
```

**Tab-Level Cache** (tránh re-fetch khi chuyển tab):
```csharp
private Dictionary<int, object?> _tabCache = new();
// Trước LoadData: if (_tabCache.TryGetValue(tab, out var cached)) return cached;
// Sau LoadData: _tabCache[tab] = data;
```

**Progressive Loading**: LoadData event → chỉ fetch 50 rows/lần, server-side filter+sort

### File thay đổi (FE + BE)

**Backend**:
| File | Action |
|------|--------|
| `gtas_vpp_be/Program.cs` | **EDIT** (thêm ResponseCompression) |
| `gtas_vpp_be.Service/VPPRequestService.cs` | **EDIT** (AsNoTracking, projection, pagination) |
| `gtas_vpp_be.Service/GenericRepository.cs` | **EDIT** (thêm GetPagedAsync) |
| Migration mới | **CREATE** (thêm DB indexes) |

**Frontend**:
| File | Action |
|------|--------|
| `Components/Shared/DataCard.razor` | **CREATE** |
| `Components/Shared/StatusBadge.razor` | **CREATE** |
| `Components/Shared/EmptyState.razor` | **CREATE** |
| `Tabs/Tab_Orders.razor` | **EDIT** (card-style + debounced search) |
| `Tabs/Tab_History.razor` | **EDIT** (enhance style + server-side paging) |
| `Tabs/Tab_ProductCatalog.razor` | **EDIT** (server-side paging + debounced search) |
| `Tabs/Tab_AllOrdersSummary.razor` | **EDIT** (server-side paging) |
| `Components/Lib/Component_ShareGrid.razor` | **EDIT** (style enhancement) |

---

## PHASE 10 — EMPTY, ERROR & LOADING STATES

### Empty State (Illustration + CTA)
```
┌──────────────────────────────────────┐
│                                      │
│         [Illustration: Box rỗng]     │
│                                      │
│       Chưa có đơn hàng nào           │
│   Tạo đơn hàng đầu tiên của bạn      │
│                                      │
│      [  + Tạo Đơn Hàng Mới  ]       │
│                                      │
└──────────────────────────────────────┘
```
> ⚠️ **@Loc[] reminder**: Trong code: `@Loc["NoOrdersYet"]`, `@Loc["CreateFirstOrder"]`. Text tiếng Việt trong plan chỉ để minh họa.

Các empty states khác nhau cho từng context (resource keys trong code):
- Orders: `@Loc["NoOrdersThisPeriod"]` → "Bạn chưa có đơn hàng nào trong kỳ này"
- Product Catalog: `@Loc["CatalogEmpty"]` → "Danh mục sản phẩm trống. Thêm sản phẩm đầu tiên."
- History: `@Loc["NoOrderHistory"]` → "Chưa có lịch sử đơn hàng"
- Library: `@Loc["LibraryEmpty"]` → "Thư viện trống. Bắt đầu thêm dữ liệu."

### Error Page (404/500) — Playful
```
┌──────────────────────────────────────┐
│                                      │
│         [Illustration: Robot lỗi]    │
│                                      │
│            Ôi không! 404             │
│     Trang bạn tìm không tồn tại      │
│     Có vẻ bạn đã lạc đường rồi       │
│                                      │
│      [  ← Về Trang Chủ  ]           │
│                                      │
└──────────────────────────────────────┘
```
- Background: particles animation như login
- Illustration SVG inline (không cần external asset)
- Text playful, tiếng Việt hài hước
- Nút gradient về trang chủ

### Loading States
- **Skeleton shimmer**: Giữ nguyên pattern nhưng cải thiện animation (mượt hơn, gradient đẹp hơn)
- **Page transition**: Fade-in 200ms khi chuyển tab
- **Button loading**: Spinner + disabled, giữ nguyên width
- **Data loading**: Skeleton grid thay thế chính xác vị trí của data sắp load

### File thay đổi
| File | Action |
|------|--------|
| `Components/Shared/EmptyState.razor` | **CREATE** |
| `Components/Pages/Error.razor` | **REWRITE** |
| `Components/Pages/NotFound.razor` | **REWRITE** |
| `Components/Shared/SkeletonGrid.razor` | **EDIT** |
| `Components/Shared/SkeletonStatCards.razor` | **EDIT** |

---

## PHASE 11 — TOAST & NOTIFICATIONS (Có Migration Strategy)

### Hiện trạng
- `CustomNotificationService.cs`: Đã có wrapper (`CustomContentNotification`) nhưng chỉ được dùng 1 phần
- **61 call sites** gọi thẳng `NotificationService.Notify()` rải rác trong: `Page_OrderCreate.razor.cs` (~15 calls), `Tab_AdminApproval.razor.cs`, Library tabs, Permission tabs, AI tabs

### Migration Strategy (2 bước)

**Bước A**: Nâng cấp `CustomNotificationService` thêm convenience methods (`Success`, `Error`, `Warning`, `Info`) - mỗi method wrap `NotificationService.Notify()` với border-radius 12px, animation toastSlideIn.

**Bước B**: Di migrate dần từng file — KHÔNG migrate 1 lần 61 sites (rủi ro cao). Ưu tiên: Page_OrderCreate → Tab_Orders → Library tabs → Permission tabs → AI tabs. Mỗi file migrate xong → build → test → commit.

### Thiết kế Toast
```
                                         ┌─────────────────────────────┐
                                         │ ✅  Thành công!             │
                                         │    Đơn hàng đã được tạo.    │
                                         │                       [✕]  │
                                         └─────────────────────────────┘
                                                   ↑ bottom-right, 16px from edge
```

**Colors**:
- Success: green-500 background, white text
- Error: red-500 background, white text
- Warning: amber-500 background, dark text
- Info: blue-500 background, white text

**Animation**:
- Vào: slideInFromRight 300ms + opacity
- Ra: slideOutToRight 200ms + opacity
- Progress bar: chạy ngang bottom, duration = toast lifetime

### Custom Notification Service
Wrapper quanh RadzenNotification để thêm style:
```csharp
public class CustomNotificationService
{
    public void ShowSuccess(string message, string title = null) { }
    public void ShowError(string message, string title = null) { }
    public void ShowWarning(string message, string title = null) { }
    public void ShowInfo(string message, string title = null) { }
}
```

### File thay đổi
| File | Action |
|------|--------|
| `Services/CustomNotificationService.cs` | **REWRITE** (thêm Success/Error/Warning/Info) |
| `Pages/VPPRequest/Page_OrderCreate.razor.cs` | **EDIT** (migrate ~15 calls) |
| `Pages/VPPRequest/Tabs/Tab_Orders.razor.cs` | **EDIT** (migrate calls) |
| `Pages/Lib/*.razor.cs` | **EDIT** (migrate calls, làm sau) |
| `Pages/Permission/*.razor.cs` | **EDIT** (migrate calls, làm sau) |
| `Components/Shared/DialogProvider.razor` | **EDIT** (style container) |

---

## PHASE 12 — AI SEAMLESS TOGGLE

### Mục tiêu
Khi toggle AI OFF: tất cả component AI ẩn, layout reflow mượt, không để lại khoảng trống.

### Các vị trí AI xuất hiện
1. **Header**: AI toggle switch
2. **Sidebar**: Menu item "AI Management" + sub-items
3. **Dashboard**: FAB chat nổi góc phải
4. **Order Create**: AI search card trong form
5. **AI Page**: `/ai/chat`, `/ai/vpp-chat` routes

### Kỹ thuật ẩn/hiện
```razor
@* Dùng CSS transition + conditional rendering *@
<div class="ai-container @(glb.IsAIEnabled ? "ai-visible" : "ai-hidden")">
    <AIChatBox />
</div>
```

```css
.ai-container {
    transition: opacity var(--vpp-transition-normal),
                transform var(--vpp-transition-normal),
                max-height var(--vpp-transition-normal);
}
.ai-hidden {
    opacity: 0;
    transform: scale(0.8);
    max-height: 0;
    overflow: hidden;
    pointer-events: none;
}
.ai-visible {
    opacity: 1;
    transform: scale(1);
    max-height: 500px;
}
```

### Global State
`glb.IsAIEnabled` được lưu trong `ProtectedLocalStorage`, đồng bộ qua event hoặc cascading parameter.

### File thay đổi
| File | Action |
|------|--------|
| `Components/Layout/MainLayout.razor` | **EDIT** (conditional rendering + transition) |
| `Components/Layout/LayoutSidebar.razor` | **EDIT** (ẩn AI menu items) |
| `Components/Layout/HeaderControls.razor` | **EDIT** (AI toggle đã nằm trong HeaderControls từ Phase 5) |

---

## PHASE 13 — RESPONSIVE (3-TIER)

### Breakpoints
```css
/* Mobile: < 768px */
/* Tablet: 768px - 1279px */
/* Desktop: ≥ 1280px */
```

### Mobile Layout (< 768px)
```
┌───────────────────┐
│ ☰  GTAS VPP   👤 │  ← Header compact
├───────────────────┤
│                   │
│   [Nội dung]     │  ← Full width, no sidebar
│                   │
│                   │
├───────────────────┤
│ 🏠  📋  📚  ⚙️   │  ← Bottom nav bar, 56px
└───────────────────┘
```

- Sidebar: overlay fullscreen khi bấm hamburger
- Bottom nav: 4-5 icon chính
- Cards: full width, stack vertical
- Tables: chuyển thành card list
- Font size: giảm 1-2px

### Tablet Layout (768px - 1279px)
- Sidebar: collapsed (64px icon-only)
- Hover flyout cho submenu
- Content: margin-left 64px
- Cards: 2 columns
- Tables: scroll ngang nếu cần

### Desktop Layout (≥ 1280px)
- Sidebar: full (260px)
- Content: margin-left 260px
- Cards: 3-4 columns
- Tables: full featured

### File thay đổi
| File | Action |
|------|--------|
| `wwwroot/css/responsive.css` | **CREATE** |
| `Components/Layout/MainLayout.razor` | **EDIT** |
| `Components/Layout/LayoutSidebar.razor` | **EDIT** |
| `Components/Layout/MobileBottomNav.razor` | **CREATE** |

---

## PHASE 14 — ACCESSIBILITY (WCAG 2.1 AA)

### Các tiêu chí chính
1. **Color contrast**: Text/background ratio ≥ 4.5:1 (AA)
2. **Keyboard navigation**: Tất cả interactive elements focusable, visible focus ring
3. **Screen reader**: ARIA labels, roles, live regions
4. **Focus trap**: Modal, dialog, sidebar overlay
5. **Skip link**: "Skip to main content" ở đầu trang
6. **Form labels**: Tất cả input có label (dùng aria-label nếu floating)
7. **Error messages**: Kết nối với input qua aria-describedby

### Implementation
```razor
@* Skip link *@
<a href="#main-content" class="skip-link">Skip to main content</a>

@* Focus visible *@
:focus-visible {
    outline: 2px solid var(--vpp-primary-500);
    outline-offset: 2px;
}

@* ARIA on interactive elements *@
<RadzenButton aria-label="Toggle dark mode" ... />
```

### File thay đổi
| File | Action |
|------|--------|
| `Components/App.razor` | **EDIT** (skip link) |
| `wwwroot/css/accessibility.css` | **CREATE** |

---

## PHASE 15 — CHARTS & DATA VISUALIZATION

### Mục tiêu
Chart gradient + animation, phù hợp Modern Playful.

### Chart Designs
1. **Column Chart**: Gradient fill từ top→bottom (primary-500→primary-300), border-radius top 6px
2. **Pie Chart**: Mỗi slice gradient riêng, stroke trắng 2px giữa các slice
3. **Line Chart**: Smooth curve (monotone), gradient area fill bên dưới

### Animation
- Bars/columns: grow từ bottom, stagger 50ms giữa các bar
- Pie: rotate + scale từ 0 khi load
- Line: draw path animation (stroke-dasharray trick)

### Implement qua Radzen Chart
Radzen Chart hỗ trợ các series type và có thể custom qua CSS:
```css
.rz-chart .rz-bar {
    transition: height 0.5s ease;
    border-radius: 6px 6px 0 0;
    background: linear-gradient(to top, var(--vpp-primary-500), var(--vpp-primary-300));
}
```

### File thay đổi
| File | Action |
|------|--------|
| `Components/Shared/ChartCard.razor` | **CREATE** |
| `Components/Pages/VPPRequest/Tabs/Tab_Orders.razor` | **EDIT** |

---

## PHASE 16 — MISC PAGES & POLISH

### Library Page
- Giữ cấu trúc tabs + Component_ShareGrid
- Style lại grid với floating header, striped rows màu tối
- Animation khi inline edit: border glow xanh

### Permission Page
- User management: avatar tròn + thông tin + switch toggle cho active
- Page permission: tree view với checkbox cascade

### Report Page
- Hiện đang placeholder "not configured"
- Thêm illustration "Đang phát triển" với style playful
- Có thể thêm sample chart để preview

### AI Chat Pages
- AIChatBox: làm đẹp bubble chat, typing indicator, animation
- AI VPP Chat: thêm product suggestion cards trong chat

---

## THỨ TỰ THỰC HIỆN (THEO ĐỘ ƯU TIÊN)

```
Phase 1:  Design Token Foundation        ← NỀN TẢNG, làm đầu tiên
Phase 2:  Custom Radzen Theme            ← Phụ thuộc Phase 1
Phase 3:  Main Layout Restructure        ← Phụ thuộc Phase 1,2
Phase 4:  Sidebar Redesign               ← Phụ thuộc Phase 3
Phase 5:  Header Bar Redesign            ← Phụ thuộc Phase 3
Phase 7:  Login Page Redesign            ← Phụ thuộc Phase 1,2 (first impression, làm trước Dashboard)
Phase 6:  Dashboard Redesign             ← Phụ thuộc Phase 1,2,4
Phase 8:  Order Create Wizard            ← Phụ thuộc Phase 1,2
Phase 9:  Data Table Upgrade             ← Phụ thuộc Phase 1
Phase 10: Empty/Error/Loading States     ← Có thể làm song song
Phase 11: Toast & Notifications          ← Có thể làm song song
Phase 12: AI Seamless Toggle             ← Phụ thuộc Phase 3,4
Phase 13: Responsive                     ← Phụ thuộc Phase 3,4,6
Phase 14: Accessibility                  ← Xuyên suốt
Phase 15: Charts                         ← Phụ thuộc Phase 6
Phase 16: Misc Pages & Polish            ← Cuối cùng
```

---

## RADZEN BLAZOR MCP — CHIẾN LƯỢC SỬ DỤNG

**MCP Endpoint**: `https://app.radzen.com/mcp`
**API Key**: Đã cấu hình trong `.vscode/mcp.json`

### Nguyên tắc bắt buộc
1. **TRƯỚC MỖI PHASE**: Query MCP để lấy documentation của component Radzen sẽ dùng
2. **TRONG KHI CODE**: Dùng MCP để kiểm tra properties, events, methods chính xác
3. **SAU KHI CODE**: Build và chạy, nếu lỗi → query MCP tìm solution

### MCP Query Patterns
```
- "RadzenLayout API properties grid-template-areas responsive"
- "RadzenPanelMenu properties DisplayStyle IconAndText"
- "RadzenDataGrid LoadData event virtual scrolling"
- "RadzenSteps component API Change event"
- "RadzenChart ColumnSeries gradient fill customization"
- "RadzenFormField Variant FloatingLabel properties"
- "ThemeService SetTheme method dark mode Material3"
- "RadzenNotification custom style position bottom-right"
```

---

## PER-PHASE MCP REFERENCE

### Phase 1 — Design Token Foundation
| MCP Query | Purpose |
|-----------|---------|
| `Radzen CSS variables rz-primary rz-body-background-color` | Lấy danh sách tất cả `--rz-*` variables để override |
| `Radzen theme customization CSS custom properties` | Cách Radzen dùng CSS variables |
| `Material3 theme variables override Radzen` | Các biến cần thiết cho Material3 |

### Phase 2 — Custom Radzen Theme
| MCP Query | Purpose |
|-----------|---------|
| `ThemeService API SetTheme GetTheme methods` | Cách dùng ThemeService đúng |
| `Radzen appearance customization primary color` | Cách đổi màu primary cho toàn app |
| `Radzen Material3 dark theme CSS variables` | Danh sách biến đặc thù cho dark theme |

### Phase 3 — Main Layout Restructure
| MCP Query | Purpose |
|-----------|---------|
| `RadzenLayout component API grid-template-areas` | Cấu trúc grid của Layout |
| `RadzenHeader RadzenSidebar RadzenBody RadzenFooter` | API từng phần của Layout |
| `RadzenLayout responsive grid-template-columns example` | Responsive layout pattern |

### Phase 4 — Sidebar Redesign
| MCP Query | Purpose |
|-----------|---------|
| `RadzenPanelMenu properties DisplayStyle Icon IconAndText` | Cách hiển thị icon+text và icon-only |
| `RadzenPanelMenuItem properties Path Icon Text` | API của menu item |
| `RadzenSidebar properties Responsive Width collapsed` | Sidebar collapse behavior |
| `RadzenPanelMenu expand collapse animation` | Animation khi expand/collapse |

### Phase 5 — Header Bar Redesign
| MCP Query | Purpose |
|-----------|---------|
| `RadzenHeader properties Style Class` | Custom style cho header |
| `RadzenStack Orientation JustifyContent AlignItems` | Layout trong header |
| `RadzenButton ButtonStyle Light Size Medium Icon` | Button style cho các toggle |
| `RadzenBadge properties Value color` | Badge cho notification |
| `RadzenIcon properties Icon string Material Symbols` | Danh sách icon Material |

### Phase 6 — Dashboard Redesign
| MCP Query | Purpose |
|-----------|---------|
| `RadzenCard properties Style Class shadow` | Card component API |
| `RadzenRow RadzenColumn properties Size SizeSM SizeLG` | Responsive grid cho KPI cards |
| `RadzenChart ColumnSeries properties color fill` | Chart customization |
| `RadzenText TextStyle properties DisplayH5 Overline` | Typography trong card |
| `RadzenProgressBar properties Mode Indeterminate` | Loading indicator |

### Phase 7 — Login Page Redesign
| MCP Query | Purpose |
|-----------|---------|
| `RadzenTemplateForm properties TItem Data Submit` | Form API |
| `RadzenFormField properties Variant Outlined FloatingLabel` | Form field variants |
| `RadzenTextBox Password properties Disabled` | Input components |
| `RadzenDropDown properties Data Value bind-Value` | Server dropdown |
| `AntiforgeryToken Blazor usage` | Security token |

### Phase 8 — Order Create Wizard
| MCP Query | Purpose |
|-----------|---------|
| `RadzenSteps component API SelectedIndex Change` | Steps component properties |
| `RadzenStep properties Text ChildContent` | Step item API |
| `RadzenSteps validation before step change` | Validation giữa các step |
| `RadzenSteps responsive mobile example` | Mobile-friendly steps |

### Phase 9 — Data Table Upgrade
| MCP Query | Purpose |
|-----------|---------|
| `RadzenDataGrid properties LoadData Virtualization` | DataGrid API |
| `RadzenDataGrid custom row template card style` | Custom row rendering |
| `Blazor Virtualize component Items ItemSize OverscanCount` | Virtual scrolling |
| `RadzenDataGrid inline editing properties` | Inline edit cho Library |

### Phase 10 — Empty/Error/Loading States
| MCP Query | Purpose |
|-----------|---------|
| `RadzenAlert properties Style Variant Shade` | Alert component |
| `RadzenSkeleton component properties` | Skeleton loading |
| `RadzenProgressBar properties` | Progress indicators |

### Phase 11 — Toast & Notifications
| MCP Query | Purpose |
|-----------|---------|
| `RadzenNotification component properties Position Duration` | Notification API |
| `NotificationService methods Show Info Success Warning Error` | How to call notifications |
| `RadzenNotification custom template style` | Custom notification appearance |

### Phase 12 — AI Seamless Toggle
| MCP Query | Purpose |
|-----------|---------|
| `RadzenSwitch properties Value Change bind-Value` | Switch component |
| `Radzen conditional rendering if AuthorizeView` | Conditional rendering |
| `CSS transition conditional display Blazor` | Animation khi ẩn/hiện |

### Phase 13 — Responsive
| MCP Query | Purpose |
|-----------|---------|
| `RadzenLayout responsive mobile desktop grid` | Responsive layout |
| `RadzenColumn responsive properties SizeSM SizeMD SizeLG` | Responsive columns |
| `RadzenSidebarToggle responsive mobile` | Mobile hamburger |
| `RadzenDrawer component mobile navigation` | Alternative cho mobile nav |

### Phase 14 — Accessibility
| MCP Query | Purpose |
|-----------|---------|
| `Radzen accessibility ARIA keyboard navigation` | Radzen accessibility support |
| `RadzenButton aria-label properties` | ARIA trên components |
| `RadzenFormField label accessibility screen reader` | Form accessibility |

### Phase 15 — Charts
| MCP Query | Purpose |
|-----------|---------|
| `RadzenChart API series types Column Pie Line` | Chart series types |
| `RadzenColumnSeries properties color gradient fill` | Column customization |
| `RadzenPieSeries properties colors slices` | Pie chart styling |
| `RadzenChart animation properties` | Chart animation |
| `RadzenChartTooltipOptions properties` | Tooltip customization |

### Phase 16 — Misc Pages
| MCP Query | Purpose |
|-----------|---------|
| `RadzenDataGrid inline edit update delete` | CRUD operations |
| `RadzenDialog properties Title Content` | Dialog API |
| `RadzenSplitter properties Panes resizable` | Split panel |
| `RadzenTree component checkbox cascade` | Permission tree view |

---

## RỦI RO & MITIGATION

| Rủi ro | Mức độ | Giải pháp |
|--------|--------|-----------|
| Radzen theme variables không đủ để custom sâu | Medium | Fallback: CSS specificity override hoặc generate theme mới từ Radzen Studio |
| Virtual scroll không tương thích Radzen DataGrid | Medium | Dùng Blazor Virtualize component native |
| Performance với animation quá nhiều | Low | Dùng CSS animation (GPU-accelerated), tránh JS animation |
| Responsive phức tạp với nhiều breakpoint | Medium | Test từng breakpoint, dùng Radzen Column responsive API |
| AI toggle làm vỡ layout | Low | Dùng CSS transition + conditional rendering đã test |
| Conflict với CSS cũ trong app.css | High | Xóa style không dùng, giữ lại Radzen fixes đã test |

---

## METRICS THÀNH CÔNG (ĐÃ BỔ SUNG BUILD VERIFICATION)

**Per-phase checklist (bắt buộc)**:
- [ ] Build: `dotnet build` không lỗi
- [ ] Runtime: `dotnet run` — app chạy được
- [ ] No regression: Các chức năng hiện tại không bị break
- [ ] MCP usage: Đã query Radzen MCP trước khi code

**Global success metrics**:
- [ ] Tất cả design tokens được dùng nhất quán trong toàn bộ app
- [ ] `--vpp-ocean-*` alias hoạt động, không break code cũ
- [ ] Dark mode mặc định hoạt động, toggle light/dark mượt
- [ ] Sidebar collapse/expand animation mượt
- [ ] KPI cards hiển thị gradient + số liệu đúng
- [ ] Login page fullscreen hero với animation
- [ ] Order Create wizard 3 bước hoạt động (tôn trọng 3 mode)
- [ ] AI toggle ẩn/hiện không vỡ layout
- [ ] Responsive: Mobile hoạt động với bottom nav, Tablet với icon sidebar
- [ ] WCAG 2.1 AA: contrast ratio đạt, keyboard nav hoạt động
- [ ] Empty states có illustration + CTA
- [ ] Error pages playful với illustration
- [ ] Toast notification bottom-right, animation mượt
- [ ] Charts gradient đẹp, animation khi render
- [ ] `Page_OrderCreate.cs` giữ nguyên logic cốt lõi (693 dòng), chỉ extract UI vào step components

## GHI CHÚ VỀ PHASE 8 (WIZARD)

**Opus 4.6 khuyến nghị bỏ wizard do thêm clicks và mobile-unfriendly.** Tuy nhiên user đã chọn wizard. Để mitigate:
- Step 1 (Context) chỉ hiển thị info, có nút "Tiếp tục" mặc định → user có thể skip nhanh
- Giữ nguyên toàn bộ logic trong `Page_OrderCreate.razor.cs` (693 dòng), chỉ extract UI render ra step components
- KHÔNG REWRITE toàn bộ file, dùng approach composition: parent giữ state, children render UI fragments
- Desktop: 3 steps rõ ràng. Mobile: step indicator compact, content scroll tự do
