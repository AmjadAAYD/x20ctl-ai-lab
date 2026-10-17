# Dashboard and Agent owner correction - 15 October 2026

The owner explicitly rejected the generic Agent drawer, idle rear turn, flat Dashboard and circular/explosive intro. Their direct request supersedes drawer/short-intro suggestions in the supplied research text.

## Earlier-session comparison

Compared HEAD 1254a1f, its Studio/Agent/Dashboard implementations and the earlier session handoff. The controller Agent had a body silhouette, eye sticks, LEDs, LB/RB mode/settings, D-pad, A/B/X/Y actions, an idea/search column and chat. The drawer displaced these controls and hid the idea column. The simplified Dashboard also removed selected-game artwork, the controller invitation and useful empty-setup guidance. These are restored. KPI cards, repeated editor shortcuts and contradictory Ready/offline statuses remain removed.

## Current behavior

- Dashboard controller is static front artwork only. No rear artwork, turn timer or idle sway exists in DashboardControllerPreview. Editor back inspection elsewhere is retained.
- Controller stage restores its Agent invitation and Configure affordance. Agent opens as the original controller-shaped overlay with its physical controls, eyes, LEDs, ideas, chat and settings. It preserves sessions/drafts and follows host resizing. Drawer implementation is removed; reduced-motion and hide/unload cleanup remain.
- Working setup and actual PC-mode context share the right hero area with selected-game full artwork, title/store/last played and Play/Artwork actions. PC mode has an enabled link to its real page. Native writers remain gated; no disabled native Apply button competes as the Dashboard CTA.
- Games use one full-art rail, a persistent selected outline and at most2px hover/focus lift. No tile zoom, glow stack, card sound or staggered page-load animation.
- Empty setup state explains the absence of compatible setups and offers Create setup. The picker/load callback remains for matching-model saved snapshots. This is not an automatic per-game association feature.
- Top tabs/overflow and quiet utilities remain. No invented battery, transport, firmware or synchronized state.

## Verification

`dotnet run --project tools/wpf-ui-checks/UiChecks.csproj -v q`

Checks front-only preview, 18-game layout/title bounds at1500x740 and1100x520, artwork copy/rename/failure preservation, compatible setup load, restored controller Agent/settings/session/offline reply/new chat/reduced motion, tab overflow and hidden PC mode destination, Dashboard restoration and Agent resize. Isolated own components are rendered; no Window or full-app stress/review harness is used. Optional local installed-game scan is read-only and renders actual local artwork with an explicitly offline input state; games are not launched.

Frames/images: artifacts/intro-steam-inspired/dashboard-current.png and agent-controller.png. Live acceptance remains the owner's review.

## Background continuation - 16 October
FacetBackdrop now uses the native AtmosphereView coherent field at ShellStrength/ShellSpeed, with retained low-opacity geometric seams. The .3 whole-layer attenuation and redundant endless facet translations were removed. New field clocks stop on hidden/unloaded/minimized windows; reduced motion is static. The Dashboard layout and front-only controller are retained. See atmosphere-source-review-20261016.md and intro-refinement-20261015.md for source study and limits.
