# Reference controls, clock and game art

Latest screenshots supersede previous pill/nav styling. Preserve commands and setup/model isolation.

1. Match flat 36px action capsules, Segoe UI 14px, white selected options and blue primary actions. Preserve the button showcase geometry.
2. Detailed Support: warm gradient, heart disk, outlined trailing menu badge, nested capsule rim. Keep existing support action.
3. Plain top tabs with short white underline; angular LB/RB geometry from user sketch, preserving input-capture ownership and responsive overflow.
4. Live local HH:mm clock in Zone and Studio headers, timer active only while loaded.
5. Decode artwork at native size, high-quality scaling and native-aspect cover frames. No fake higher-resolution assets.
6. Build, verify navigation/geometry/clock/artwork and render desktop/compact evidence; reopen private app.

Latest steering: exchange bumper silhouettes while retaining LB on the left (previous) and RB on the right (next). Filled, softened pill heads and short shoulder extensions replace the literal sketch outlines.

Verification: product compiled; Zone geometry/commands, native artwork resolution/aspect regression, local HH:mm clock and all ten animated tab routes passed. General WPF harness passed including populated Dashboard at desktop/compact sizes; Mouse refinement passed. New screenshots in artifacts/refinement-followup-20261016. No connected hardware test or publication.

Color/liveness follow-up: secondary commands now use a navy/cobalt tonal fill with a soft edge; primary actions use a brighter blue gradient. Hover wash increased from 6% white to 18% blue and keyboard focus is clearer. Bumpers and option groups share the updated palette; white selected options, shapes, sizing and typography remain. Product compilation, followup and Zone harnesses passed. Inspected Dashboard render; no connected hardware test.

Latest request: sharpen bumper corners; use thick 42px command capsules, light blue primary actions and gray secondary actions. Equal square cover frames match Genshin, using UniformToFill crop while keeping source files untouched. Support rim cycles colors every five seconds while loaded; reduced motion stays static and unloading stops animation. This supersedes native-aspect tile sizing.

Final wing correction: previous LB was implicitly clipped by its 76px Grid column. Both controls now span the navigation grid, with explicit positions above the end tabs. Slimmed wing silhouette from 180x54 to 164x40, reduced the navigation band to 86px, sharpened tips, added a fine edge accent and centered the 12px labels. Hit-testing the LB head and all ten navigation routes passed. Square-cover, thick-button and Support animation lifetime checks also passed.

Latest size adjustment: both wings are now 140x32, with shorter outer tails and a 6px downward offset. Followup harness passed, including LB head hit-testing and all ten tab routes. Build and diff checks passed; private app reopened for review.
