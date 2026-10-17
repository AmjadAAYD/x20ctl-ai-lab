# Matte control and panel correction

User supplied the design direction in attachment 1baccd7b. Preserve commands and the buttons showcase.

- Replace glossy shared styles with semantic matte action, primary, icon, segmented, status, keycap and navigation styles. Study supplied GitHub references; no new library dependencies.
- Remove stretch/sheens/page-translation on selection, retaining a monotonic 210 ms shared navigation tile. Add clearance under chrome/navigation.
- Frame Dashboard state and games, each showcase family, Curves stage/parameter shelf and Device sections with matching navy panels.
- Keep actual model/player data and hardware capability gates. Verify WPF build, affected harnesses and desktop/compact renders.

## Completed

Removed lens stretch, sheen and page-entry translations. Retained a 210 ms shared navigation tile with extra vertical clearance and constant text weights. Added semantic ActionButton, PrimaryButton, IconButton, SegmentOption, SegmentedControl, StatusTag and GamepadKeycap styles; legacy aliases now resolve to matte controls.

Dashboard state and games, showcase families, Curves controller/parameters and Device sections have matching navy surfaces. The showcase retains its face and D-pad geometry. Status text keeps its readable size; short windows scroll. Channel, preset and vibration options use grouped tracks.

Reviewed shadcn/ui, HeroUI, Magic UI, React Bits, Motion Primitives and Radix Themes repositories, plus shadcn/HeroUI button and Radix segmented-control documentation. No new UI dependencies. Motion Primitives tabs documentation endpoint failed; repository was available.

Passed product build, followup UI harness, Zone surfaces, Mouse refinement and general WPF harness. Checks cover all ten animated tab routes, no lens distortion, stable graph selection/editing, mapping/capture/markers, artwork and All vibration preview. Dashboard with 18 fixture games passed at 1500x740 and 1100x520. git diff --check passed. Existing WFO0003 DPI warning remains.

Rendered every Studio route at 1500x900 and 1100x760 in artifacts/refinement-followup-20261016, plus Pro Device/Curves at 1920x1080. Reviewed affected desktop/compact surfaces. Reopened private app PID 32032 and confirmed it remained running. Fixture stores only; connected hardware behavior was not exercised. No publication or hardware configuration write.
