# Controller Zone hierarchy and shared material pass - 16 October 2026

Owner critique implemented:
- Player identity is a readable heading (24 px on focused card; 20 grid; 16 compact) rather than a tiny capsule. Assigned/disconnected state is an amber labelled badge; mock connection remains explicitly labelled and green. No real connection is fabricated from model assignment.
- Switch Controller and Enter Studio are compact 38 px shared glass commands, 156/130 px wide. Navigation arrow removed.
- Empty slots offer one explicit + Add controller command instead of a detached corner arrow. Center plus gains a scaled visible surface and its existing pointer event route is reachable; art/text do not intercept the card target. The keyboard-accessible Add command invokes model selection directly.
- Ambiguous multiplication sign replaced by labelled Remove on all assigned cards. Assignment removal and persistence remain; setups are retained.
- Hover changes fill only; keyboard/gamepad focus uses a white edge; expanded selection uses blue surface/edge; assignment uses hardware artwork; connection uses a semantic badge.
- Zone header background and separator removed so the existing environment continues beneath chrome. Body is Player slots under Controller Zone. Footer now displays navigation hints only; low-level/debug elements remain programmatic and hidden.
- Update notice is a utility notification labelled Update available, without the coloured mode-pill treatment or download arrow. Existing download/error/retry routing retained.
- Model picker consumes Zone surface tokens, removes obsolete glossy top overlays, zoom-on-hover and broad glow, uses matching glass Back control, and lays out all eight models in two rows of four.
- Zone artwork retains original pixels and silhouette clip but hides the synthetic dark/light rim overlay. Very faint magenta/cyan reflections seat RGB hardware in the card. This is presentation lighting, not RGB configuration/telemetry.
- Flip controller uses the same Zone.Glass brush/template as the other compact commands.

Verification:
- Product builds with existing unused vibration variable and manifest-DPI warnings.
- Zone checks pass: persistent four slots; assigned-art bounds; compact empty-card text separation; token text contrast >=4.5; ambient/reduced motion; add route; center-plus inherited hit-test enablement; every model picker choice; Remove persistence with other slots preserved.
- Full scoped UI harness passed: Dashboard layouts, manual flip, all ten tabs, battery states, Agent, setup load, artwork persistence and silent intro reference/Skip/handoff checks.
- Isolated renders visually inspected for focused and empty Zone, picker at 1280 width and Dashboard glass Flip command. Render paths: artifacts/zone-surfaces-20261016 and artifacts/intro-stabilization-impact.

No native hardware write or publishing. Source artwork was not regenerated. Isolated UI renders do not establish live hardware or frame-rate performance.
