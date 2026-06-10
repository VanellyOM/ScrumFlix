# ScrumFlix — Sprint Audit: Applied Changes Handoff
**Date:** June 9, 2026
**Scope:** Admin grid look & feel · standalone `_AdminLayout` separation · xUnit v3 / CI wiring
**Build state:** Not compiled in this pass (no .NET 10 SDK + NuGet blocked in the work environment). All C# was written against the real source signatures; **CI is the compile gate.**

---

## Part 1 — Admin grid look & feel

**`wwwroot/css/sf-components.css`**
- Replaced the admin-grid block (`.sf-admin-table` and friends) with the elevated `.sf-admin-table-wrap` card surface (visible left accent border + shadow), gave `tbody` a real card background instead of `transparent`, and added the `first-child` seam fix, thicker uppercase `thead`, and `focus-visible` sort-header outline.
- Replaced the pagination block. **Why it mattered:** the old block referenced `--sf-surface-2`, `--sf-surface-3`, and `--sf-border-muted`, which **do not exist** in `sf-tokens.css` — they were silently using their hardcoded hex fallbacks. The new block uses real tokens (`--sf-bg-card`, `--sf-border-color`, `--primitive-radius-sm`, `--sf-color-focus-ring`, etc.).

**Deviation from the audit (intentional):** the audit's hover tint hardcoded `rgba(240,199,61,.06)` while its comment claimed the tint would become navy on the light theme and red on the red theme. A literal rgba cannot do that — it stays gold everywhere. Replaced with `color-mix(in srgb, var(--sf-color-accent) 6%, transparent)`, which resolves from the accent token and therefore *actually* adapts per theme. This matches the `color-mix` pattern already used elsewhere in `sf-components.css`.

**Note on the audit's section reference:** the audit said the grid block was "section C10." In the real file, C10 is *Cascade Dropdowns*; the grid block is an unlabeled section further down. Edited the correct block.

**Table wrappers** — `.table-responsive` wrapped in `.sf-admin-table-wrap` across **13 files / 14 tables**:

| Audit-listed (7) | Additionally wrapped (6) |
|---|---|
| `AdminManage/Users.cshtml` | `AdminHome/AdminDashboard.cshtml` |
| `AdminManage/Locations.cshtml` | `AdminHome/TmdbSyncPage.cshtml` |
| `AdminManage/Screens.cshtml` | `AdminManage/StaffPortalTest.cshtml` (×2) |
| `AdminManage/Concessions.cshtml` | `Concessions/Concessions.cshtml` |
| `AdminManage/Showtimes.cshtml` | `Schedule/_ShiftsGrid.cshtml` |
| `Concessions/ConcessionsCatalog.cshtml` | `Schedule/_AssignmentsGrid.cshtml` |
| `Movies/MovieCatalog.cshtml` | |

**Why the six extras:** the new CSS restyles every `.sf-admin-table`, so leaving them unwrapped would have produced visually inconsistent grids (some elevated, some flush to the page). The two Schedule files are HTMX partials — the wrapper sits *inside* the swapped fragment, so swaps remain self-contained.

---

## Part 2 — Standalone `_AdminLayout.cshtml` (Option B)

`Areas/Admin/Views/Shared/_AdminLayout.cshtml` is now a complete standalone document (`Layout = null`) — no consumer navbar, cart, or footer. It owns its `<head>`, an admin topbar (brand + user pill + theme picker + logout), flash banners, and the sidebar + content wrapper. Renders `Head` inside `<head>` and `Scripts` after the Bootstrap bundle — fixing the section-swallowing bug that previously stopped per-page SignalR / spinner / validation / `sf-admin-tables.js` scripts from loading on Admin pages.

**`wwwroot/css/sf-components.css`** — added the `ADMIN TOPBAR` block (including the `--sf-admin-topbar-h: 60px` token) before the sidebar section, and updated the three hardcoded `72px` sidebar/wrapper offsets to use that token. (No `72px` literals remain in the CSS.)

**Deviations from the audit (intentional):**
- The audit's draft loaded `sf-theme-switcher.js` in **both** `<head>` and end-of-`<body>`. That script binds the theme-picker click/keydown handlers in `init()` on `DOMContentLoaded`, so a second load would **double-bind** the picker (open-then-close / double-cycle). It is loaded **once in `<head>`**, matching the proven consumer `_Layout`.
- Kept `scrumflix.js` in the admin pipeline (it highlights `.sf-admin-nav-link` and auto-dismisses flash banners; all of its DOM access is element-guarded, so it's a no-op where elements are absent). Kept the AI-content `<meta>` disclosure for parity with `_Layout`.

**Verified safe:** all Admin controllers derive from `StaffControllerBase`, which populates `Context.Items["LayoutViewModel"]` (UserName / RoleId / ActiveTheme) on every `ViewResult` regardless of layout, so the standalone layout's lookup still resolves. `Areas/Admin/Views/_ViewStart.cshtml` already points at `_AdminLayout` — unchanged.

---

## Part 3 — CI split + real unit tests

**`.github/workflows/dotnet-ci.yml`** — the single `dotnet test` step is split into:
1. **Unit tests** (`tests/ScrumFlix.Tests`) — run unconditionally; no secrets needed.
2. **A secret-detection step** that reads `MYSQL_CONNECTION` / `TMDB_API_KEY` via env (never interpolated into the shell — no leak / injection) and sets an output flag.
3. **Integration tests** (`tests/ScrumFlix.IntegrationTests`) — run only when `if: steps.intsecrets.outputs.have == 'true'`.

**Improvement over the audit:** the audit suggested `continue-on-error`, which still *runs* the integration tests against Aiven on every push and merely ignores failures. The `if:`-skip means forks/PRs without secrets **skip** integration tests cleanly (with a GitHub `::notice::`) rather than soft-failing — and removes the per-push hit on Aiven when secrets are absent. YAML validated.

**Real unit tests** — `ExampleTests.cs` (`Assert.True(true)`) removed; replaced with tests against real production code:
- `QrTicketPayloadTests.cs` — `QrCodeService.BuildTicketPayload`: prefix, 8-segment pipe format, `MOVIE`/`SCREEN`/`LOCATION` → `N/A` fallbacks, seat → `GA` default, and UTC→Central conversion (uses `2026-06-16T02:00Z`, whose **UTC date is the 16th but Central date is the 15th**, so the assertion proves real conversion rather than raw formatting; checks `CDT`).
- `QrConcessionPayloadTests.cs` — `QrCodeService.BuildConcessionPayload`: header, `ORDER`, `Itemx2,Otherx1` join, empty-items segment, `TOTAL` (keyed off the culture-stable numeric portion, since `ToString("C")`'s symbol is culture-dependent), and UTC→Central date.
- `TimeZoneConversionTests.cs` — `[Theory]` UTC→Central with CDT/CST and DST flag (includes the audit's `9:00 AM` worked example), plus an `Unspecified`-Kind-treated-as-UTC documentation test.
- `TimeZoneTestHelper.cs` — resolves Central via `America/Chicago` or `Central Standard Time`; tz-dependent tests no-op if neither resolves (never the case on the Linux CI runner).

**Assertion surface:** tests deliberately use only the `Fact`/`Theory`/`InlineData`/`Assert.*` API shared by xUnit v2 and v3, since the project couldn't be compiled here. v3-specific helpers (`Assert.Skip` / `Assert.Multiple` / `Assert.Equivalent`) can be adopted incrementally once a green CI run confirms the wiring.

---

## Manual steps / things to confirm on first CI run

1. **Compile gate.** This pass was not compiled locally. Confirm `ScrumFlix.Tests` builds and the new tests pass on the runner. The currency-symbol culture handling and tz resolution are the two most environment-sensitive spots, both written defensively.
2. **Visual pass on all three themes** (dark / light / red) for the grids and the new admin topbar — confirm the `color-mix` accent tint and the topbar badge read correctly on each.
3. **Click-through the admin theme picker** to confirm the single-load fix behaves (open/select/close once, no double-toggle).
4. **Spot-check per-page scripts now load** on an admin page that uses `@section Scripts` (e.g. a grid page's `sf-admin-tables.js`, or `TmdbSyncPage`'s SignalR + `@section Head` spinner CSS) now that the standalone layout renders those sections.

## Still backlog (not done this pass — per the audit's 🟢 list)
- Enable the SQLite in-memory swap in `ScheduleIntegrationTests.cs` so integration tests can run without live DB credentials.
- Optional seeder collision-logic unit tests (only if a unit-testable seam is extracted from `SampleDataSeederFull`).
