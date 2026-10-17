# Dashboard cleanup, battery and slot removal - 16 October 2026

Owner screenshots and 4.4-second recording reviewed. Scope implemented:
- Keep only top model pill. Hide header context name and remove Dashboard model/player heading and repeated right-side model pill.
- Remove Dashboard Agent invite bubble and Configure controller caption. Controller remains clickable; top Agent button still opens original controller-shaped Agent.
- Consistent glass tab treatment; hover uses a quiet wash instead of a second outline. Explicit per-Studio exclusive tab group. All ten destinations and responsive overflow retained.
- Top battery section before Agent uses existing once-a-minute read-only XInput polling. Each nonvirtual input slot retains its own reported level, selected by Studio player index. Full/Medium/Low/Empty/Wired or Not reported; no fabricated percentage and no borrowing another slot's battery. Hardware reporting remains dependent on Windows/controller support.
- X button in top-right of each assigned Controller Zone card. Removes that assignment only, restores empty slot, collapses removed focused player and persists remaining assignments. Empty slots hide X. Saved setups and local drafts are retained; no controller disconnect or hardware write.

Verification:
- Product build and scoped WPF checks pass with existing vibration-unused-variable and DPI manifest warnings.
- All ten tab routes checked for exactly one selected destination. Battery display mapping covers coarse levels, wired, unknown and null using fixtures.
- Removal UI event checked through Zone and ProductHost with a temporary assignment store: cleared slot persisted, other assignment retained, four slots retained and X hidden afterward.
- Dashboard artwork/setup/Agent, header status, overflow and silent intro/reference checks remain passing.
- Updated isolated renders inspected: artifacts/intro-stabilization-impact/dashboard-panel-header.png and artifacts/zone-surfaces-20261016/zone-selected-fixture.png. These are component renders, not connected-hardware or live-framerate claims.

## Agent placement and manual Dashboard flip

Owner requested moving Agent XCTL beside the brand in the top-left header and a Flip controller control at the controller panel's top-right. Agent now uses AgentTitleHost in the existing brand row, with its original toggle callback. Dashboard adds a glass Flip controller button that switches between the current model's front and rear PhotoController artwork. Front is default; rear is created on demand. This supersedes the earlier front-only restriction for explicit user-triggered flipping, and introduces no automatic turn or timer.

Build and scoped WPF harness pass, including Agent host placement, manual rear/front state transitions, all ten tabs, battery display and existing setup/artwork/intro checks. Updated full Studio render visually inspected. Changes remain local.
