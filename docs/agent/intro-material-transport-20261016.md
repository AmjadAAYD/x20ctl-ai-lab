# Controller material transport - 16 October 2026

## Owner clarification and scope

The PNG/fade/left-right wording mocked the rejected animation. It was not a new requested approach. Continue autonomously with a replacement, retaining the original simple controller silhouette, screen and three LEDs, neutral graphite/steel atmosphere, silence, original X, slogan and header handoff. No photo, controller controls, floor shadow, blue/orange or green wash. Keep the controller-shaped Agent and front-only Dashboard.

## Replacement mechanism

ControllerMaterialTransport assigns 2,800 persistent material parcels to the two existing controller contour halves. Each has one source, an offstage gathering position, and one destination inside the corresponding original X blade. Destinations are sampled inside the existing geometry, sorted against its aligned boundary so the material retains contour ordering.

A quintic gather brings the material into the controller. A readable hold follows. Between 2.35 and 4.05 seconds staggered, curved trajectories peel the material away and assemble the X. Trails sample each parcel's own previous positions, sharing its mass budget. There is no independent X density mask or target-mask opacity reveal. The existing solid X takes over only after the transported material has reached its destination; this final material-to-solid seating still overlaps opacity from 4.05 seconds.

SteamControllerView splats the population into one bounded 560x400 density texture. SteamController.hlsl only shades/warps that texture. CPU and shader paths use the same authoritative trajectories. Background flow deformation and peripheral wakes receive measured parcel speed via MaterialImpulse. The original screen remains transparent, and LEDs now activate after gathering begins. Existing reduced motion, Skip and header handoff remain.

## Verification

- Both ps_3_0 shaders compile: Atmosphere 11,728 bytes; SteamController 872 bytes.
- Product and isolated WPF fixture compile; existing VibrationVisual unused-variable and high-DPI manifest warnings remain.
- Scoped WPF checks pass: stable parcel population; continuous sampled positions across phase boundaries; exact original-X destinations; intermediate material movement; translucent/evolving material; controller/fog neutrality; readable lower grips; stable reduced motion; Skip exactly once; header handoff; retained controller Agent/Dashboard/navigation.
- Audio packaging/guard verification passes, including forced sound flag: no score is opened or packaged.
- Sixteen keyframe renders were generated. Controller hold, intermediate deformation and X seating inspected. Full preview exported from 213 frames: artifacts/intro-material-transport/intro-transport.mp4, 7.10 seconds, 1280x800/30fps H.264, video stream only. SHA256: 0046AC5AA83A9D5FAFBB8814F07D196B55386265D983F14D479FCDCDE135B988.

These checks establish mechanism and regression behavior, not owner visual acceptance. Preview uses the explicit CPU renderer. WPF tier 2 and both shader constructors were observed; live application GPU performance has not been measured. No full application stress harness, game launch, hardware writes, public checkout edits, commit or push.
