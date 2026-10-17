# Dashboard restoration and setup model isolation - 16 October 2026

Owner requests: enlarge the Dashboard controller; replace rectangular tab hover/moving underline with a glass pill; restore earlier square game tiles and bottom selected-game actions; restore Ready to play with Games/Changes/Setups metrics. Follow-up: never mix X20 and X20 Pro setups.

Changes:
- Dashboard controller preview enlarged with a centered 1.18 scale and revised artwork margins. Existing Buttons-style panel, controller Agent and front-only artwork retained.
- Top tabs use translucent rounded glass selected pills and border-only hover/focus feedback. Moving underline hidden and its animation removed. Overflow destinations remain operational.
- Restored square game tiles showing uncropped images. Selected-game title/store metadata and compatible setup selector, Load setup, Edit, Change icon and Play restored to one bottom row.
- Ready to play headline, assigned controller/player pill and three counters restored. Counts use scanned games, local draft changes and the assigned model's saved setups. Actual Offline/Live input remains in the top bar; offline explanatory text remains explicit.
- SetupsWorkbench lists, counts and selects only exact model IDs. Stale foreign active/selected identities cleared. Cross-model selection and native/legacy imports rejected before mutation. Shared storage retains existing records for all models; no profiles deleted or relabelled. Existing LoadSetup model check retained.
- Dashboard setup choices/counts use the same model filter. Returning from setup edits recreates Dashboard to refresh these values.

Validation:
- Product builds with the existing unused vibration-variable and manifest-DPI warnings.
- Scoped WPF harness passes Dashboard layout at 1500x740 and 1100x520, game artwork persistence, compatible setup load, Agent behavior, header state updates, tab overflow and silent intro/reference behavior.
- New --setup-isolation fixture passes both directions (X20/X20 Pro), foreign native and mixed/foreign legacy rejection without library modification, stale active-state clearance, matching import and preservation of other models.
- Component renders reviewed under artifacts/intro-stabilization-impact. These are isolated renders, not claims of hardware connection or live performance.

Native setup creation remains restricted to the models supported by the existing snapshot schema; this change does not pretend that X20 snapshots are X20 Pro snapshots or enable unsupported hardware writes.
