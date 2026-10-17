# UI corrections — 16 October 2026 follow-up

Preserve the current app, controller-specific setup boundaries, original artwork and hardware-write gates.

1. Reproduce curve-size changes and interrupted tab motion in the WPF harness.
2. Keep a fixed graph/readout allocation while editing. Keep one persistent tab lens alive across repeated route/layout notifications, including rapid switching and overflow.
3. Place Remove at compact-card top right and below the controller in focused view. Remove Zone-to-Studio artwork flight.
4. Reuse Dashboard PhotoController rendering on Device/Mouse; retain bitmap quality and original assets.
5. Add an All vibration preview region (both grips and both triggers where present).
6. Compact the Buttons editor: physical input → output, contextual output categories, preserve the graphical buttons showcase (latest user correction), alongside the compact editor, modified markers, selected mapping label, scoped restore and contextual Test mapping. Keep model/PC capability gates.
7. Refine header into identity/Agent plus one device-status control; move unavailable battery into device details and keep OS controls quiet.
8. Revise advanced stick tools around one selected stick with accurate range/direct-input/axis terminology, output graph, copy/undo and explicit measurement recommendations.
9. Verify affected behavior and renders at desktop/compact sizes; reopen the private lab app. No publication or hardware writes.


## Implemented and verified

- Preserved the Face and D-pad diamonds plus shoulders, sticks, system and special outputs. The showcase remains visible before source selection and assigns outputs after selection; compact editor retains You press / Game gets and contextual testing.
- Fixed shared tab lens cancellation on repeated layout updates; all ten routes tested with animation enabled. LB/RB preserve tab coordinates on input-capture pages.
- Curve plot remains 408 x 240 before and after point selection/editing. Point editor lives in the shelf instead of shrinking the chart.
- Device and Mouse reuse the Dashboard PhotoController renderer. Added All vibration preview for model-provided grip/trigger regions.
- Compact Remove stays upper-right; focused Remove sits below artwork. Empty Add stays below its silhouette. Removed Zone-to-Dashboard controller flight.
- Combined device identity/state in header, quiet OS chrome, advanced stick selection/copy/Undo/link/bypass and measured drift review. PC-only Disabled mappings suppress output without adding a native hardware command.

Validation: followup WPF harness passed, including ten animated routes, graph stability, mapping/markers/capture, advanced stick controls and All preview. Zone surfaces and Mouse refinement harnesses passed. Buttons rendered and inspected at 1500x900 and 1100x760. Relevant domain tests previously passed (31 tests). git diff --check passed. Build retains existing WFO0003 DPI warning.

Physical controller input, measured drift acquisition and hardware behavior were not exercised with connected hardware. Native writer gates remain in place. No release or publication.
