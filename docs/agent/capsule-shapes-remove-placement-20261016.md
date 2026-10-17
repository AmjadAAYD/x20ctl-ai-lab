# Capsule shape and Remove placement

Latest correction: copy Support reference shape/proportions/style, not its pink/orange color. Preserve current colors.

- Use full capsule rounding in shared button/selection templates; preserve command handlers and color values. Keep physical face buttons circular.
- Compact Remove below model connection status; focused Remove in bottom command row immediately before Enter Studio. Empty slots still hide Remove.
- Verify Zone removal/persistence/geometry and Studio routes/renders; reopen private lab.

Implemented and verified: true capsule radius binds to half the rendered height. Fixed-radius 999 was wrong in WPF and produced an oval; removed it from the edited templates and showcase buttons. Support uses compact 32 px height, 13 px text and 14 px side padding. Existing background/color values remain unchanged. Compact Remove is below status; expanded Remove is immediately before Enter Studio in the same row.

Zone harness passed, including actual Support corner-radius/color checks, both Remove positions, removal persistence, other-player preservation and hidden action on empty slots. Followup Studio harness passed all ten animated routes and affected editor checks. Product compilation passed with the existing DPI warning. git diff --check passed. Inspected zone-selected-fixture.png; reopened the private app. No connected-hardware verification or publication.

Follow-up: added full-face diagonal tonal shading, a soft upper edge and lower shading to shared ActionButton template. Removed Support hard pale outline; kept base palette. Tightened Support to 30 px height, 12 px text/icon, 6 px icon gap and 12 px side padding. Zone build/harness passed including material/edge and true-capsule geometry checks. Preview: artifacts/zone-surfaces-20261016/support-detail.png.
