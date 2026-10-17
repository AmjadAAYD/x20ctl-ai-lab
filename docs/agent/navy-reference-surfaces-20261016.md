# Reference-based navy surfaces - 16 October 2026

Owner reference: ApexSenseBridge screenshot supplied in chat. Scope: bring X20CTL's backgrounds, panels, buttons and game shelf into that visual family, preserving routes and controller Agent.

Implemented:
- Shared native navy environment, broad blue light at upper right and darker lower edge. No fog, facets, particle layers or new dependencies. Stops when hidden/minimized; reduced motion remains static.
- Controller Zone navy cards/chrome, pill controls, blue focus state and coral Support action using the existing callback.
- Studio and secondary screens use the same environment; Studio propagates its reduced-motion setting.
- Dashboard rounded navy selected-game panel, rounded primary/secondary controls and portrait game covers with single-line titles. Covers scale down in short windows to retain room for the controller stage.
- Existing controller-shaped Agent, front-only Dashboard preview, silent intro and original transformation are retained.

Verification:
- Product and WPF harness build completed. Existing unused vibration variable and manifest DPI warnings remain.
- Zone surface checks pass: four persistent slots, routes, assigned-art fixtures, bounds, motion/reduced motion and card text token contrast >=4.5:1.
- Full scoped UI harness passes: Dashboard 18-game overflow cases at 1500x740 and 1100x520; artwork persistence; setup filtering/loading; original controller Agent; responsive navigation; silent intro, reference keyframes, Skip and handoff.
- Isolated renders inspected under artifacts/zone-surfaces-20261016 and artifacts/intro-stabilization-impact. These are component renders, not hardware or runtime performance evidence.

This is a styling adaptation of the screenshot, not a pixel-identical reproduction of another product's content or layout. Changes are local; no commit, publish or hardware writes.

## Dashboard controller panel and header status follow-up

The owner requested the same background block as the Buttons visualizer and Offline beside the top model pill. Dashboard now wraps its existing controller stage with Kit.Panel, sharing the exact radius, fill and border implementation. The duplicate Dashboard connection chip was removed; Studio owns a single top-bar StatusChip immediately after its model pill. SetGameplayConnected updates it from Offline to Live input and back using the existing actual gameplay input signal. Controller Agent, front-only preview and all other controls remain.

Scoped UI checks pass, including explicit assertions of header placement and both connection transitions. Dashboard panel render and full Studio render saved under artifacts/intro-stabilization-impact. Product build succeeds with existing warnings. No hardware writes or release.
