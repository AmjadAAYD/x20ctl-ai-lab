# Controller Zone surface correction - 16 October 2026

Scope: preserve four-player layout, card-to-focused-player transitions, actual controller art, all top controls/update/install routes, footer hints and model assignment. Fix material hierarchy only. Restored intro/silence/impact and controller Agent are outside this pass.

## Primary gaming-software comparison before implementation

- [Razer Synapse4](https://www.razer.com/synapse-4): modernized device configuration and contextual tips; apply a clear device/state/action hierarchy. Its supplied support links redirect through a pre-page check, so their contents were not verified.
- [Logitech G HUB](https://www.logitechg.com/en-gb/software/guides/g-hub-basics): connected devices occupy the Home center; selecting hardware opens its settings. Retain large actual controller art and one whole-card target.
- SteelSeries [107](https://techblog.steelseries.com/2026/03/24/GG-notes-107.0.0.html), [111](https://techblog.steelseries.com/2026/05/20/GG-notes-111.0.0.html), [112](https://techblog.steelseries.com/2026/06/02/GG-notes-112.0.0.html), [120](https://techblog.steelseries.com/2026/09/22/GG-notes-120.0.0.html): device status on Home, relocated secondary access, consistency work and contextual mouse-state tooltips. Apply quiet resting tiles and intentional state emphasis. Do not copy or invent their telemetry.
- [Corsair iCUE](https://www.corsair.com/us/en/explorer/diy-builder/blogs/corsair-icue-everything-you-need-to-know/): Home exposes profiles/devices and a dedicated Dashboard handles sensors. Keep capabilities in their existing screens.
- ASUS [NUC6](https://www.asus.com/support/faq/1055594/) and [Gaming6](https://www.asus.com/support/faq/1043747/): device/system information and operating modes organize Home; use shared dark surfaces around functional content.
- [NZXT CAM](https://nzxt.com/pages/cam): monitoring and hardware control remain the organizing categories; restrained shared panels support the hardware.
- Dell's supplied AWCC dashboard page failed twice. Flydigi's supplied document redirects to a generic help center. These were attempted but their detailed tile/action guidance was not verified. No further retries or inferred visual claims.

Material checks: [Fluent material](https://fluent2.microsoft.design/material), [color](https://fluent2.microsoft.design/color), [elevation](https://fluent2.microsoft.design/elevation), [layout](https://fluent2.microsoft.design/layout) and [Mica layering](https://learn.microsoft.com/en-us/windows/apps/design/style/mica). Solid content surfaces and a subordinate base layer support hierarchy. This implementation is native WPF material styling, not a claim to enable OS Mica/Acrylic. Apple Materials returned a JS shell and its Markdown link failed; full content unavailable.

## Current implementation audit

ZoneScene uses FacetBackdrop/AtmosphereView, whose large high-contrast polygons cross the window. DockView already provides four reusable player views, but each card adds gloss bands, a diagonal panel/seam and slash decoration. Empty art stacks white outlines, multiple glows and blinking dots. Update, Support, Tools, overview and arrows use unrelated styles. Hover and keyboard focus both set the same white card border, causing separate targets to appear identically selected. Selection uses a second fill and a large blue glow.

## Shared tokens defined before implementation

| Token | Value/role |
|---|---|
| Backdrop | #10151C, lowest-contrast environment |
| ChromeSurface | #171E28, same neutral family |
| ContentSurface | #232C38 to #1D2530, opaque shared card |
| ContentSurfaceHover | #293442 to #222C39 |
| ContentSurfaceSelected | #2A3647 to #222E3D |
| BorderDefault | #3A4656 |
| BorderHover | #65778E |
| BorderSelected | #6CA8E8 |
| TextPrimary | #EDF2F8 |
| TextSecondary | #A9B6C8 |
| AccentPrimary | #5B9FE7, primary command/focus/selection |
| SemanticSuccess | #78C39D |
| SemanticWarning | #D9B675 |
| SemanticError | #D78286 |

Use Zone-prefixed WPF resources and one button base with Primary/Secondary/Utility/Icon/Status variants. Scope styles to Controller Zone/DockView/update notice. Replace only the Zone's background instance with a soft animated environment, retaining other pages' existing backdrop and the restored intro. Keep dots and silhouette recognizable but remove glow and excess contrast. Retain actual device art, selection/hover/focus cues, all layout and actions.

## Verification plan

Isolated Zone renders at two sizes, all-empty and assigned-model fixtures (clearly labelled), default/selected states and utility controls. Verify four persistent docks and actions, shared materials, no placeholder glow, content/background separation, ambient movement/reduced motion and unchanged layout bounds. Build and smallest affected checks; no full-app stress/review harness or hardware writes. Launch private app. Rendered own components are the captured review result; do not claim a desktop screenshot or visual approval.
## Result and review

Implemented ZoneSurfaces.xaml with the listed reusable tokens and named Primary/Secondary/Utility/Icon/Status button variants. All four existing DockView instances use the same opaque gradient material; hover and selected states use related surfaces, keyboard focus/selection use an accent edge and pointer hover uses a neutral edge. No card zoom, repeated diagonal/gloss artwork or placeholder glow remains. Header/footer/status/brand tile and update notice use the same neutral family. Standard controller A/B glyph colours remain input legends, not separate structural themes.

ZoneBackdrop replaces only the Zone backdrop instance. Soft cool/warm illumination moves slowly at a maximum15 updates/sec; it contains no hard polygons and stops when hidden/minimized/unloaded. Reduced motion is forwarded by ZoneScene. Other pages, restored intro/impact/silence and Agent remain unchanged by this pass.

Inspection caught stale artwork in reduced-motion focused-player layouts; DockView had followed only the OS setting and ignored its Zone's reduced setting. Forwarding the existing setting fixes that handoff. Compact-card inspection also caught artwork touching the title region; its size/placement now fit above the same text band, with the four-card layout unchanged. Explicit brand resource resolution keeps the mark present in isolated renders.

Verification: `dotnet run --project tools/wpf-ui-checks/UiChecks.csproj -v q -- --zone-surfaces` PASS. Product compiles with the two pre-existing warning categories (unused vibration variable and DPI manifest). Tests cover shared materials/button inheritance, four slots, quiet placeholders, Tools/Support/select routes, assigned artwork and primary availability, default/selected states, artwork bounds and compact text separation, ambient animation versus reduced motion, card text-token contrast >=4.5:1. The initial shared-material test failed against the old implementation before migration and passed after. Git diff --check passes.

Captured/inspected isolated renders: artifacts/zone-surfaces-20261016/zone-empty-1920.png, zone-empty-1280.png, zone-assigned-fixture.png, zone-selected-fixture.png. Assigned models in these renders are fixtures and state Not connected; no hardware connection is claimed. Actual pointer/keyboard hover is implemented through existing handlers; screenshot fixtures exercise resting and selected states, not a live desktop hover session. No full-app stress harness, actual updater installation, hardware writes, public checkout edits, commit or push.
