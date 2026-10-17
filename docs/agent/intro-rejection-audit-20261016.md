# Rejected steam opening - 16 October 2026

Owner rejected intro-steam.mp4 as low quality and requested a redo. Clarification resolved: PNG/fade/left-right described the rejected shortcut sarcastically; it was not an instruction. No controller photo or new PNG asset is authorized by that wording. See intro-material-transport-20261016.md for the replacement.

## What the rejected mask implementation did

SteamControllerView and SteamController.hlsl independently sample the controller and X distance masks. Controller density is multiplied by `1-Smooth(2.60,3.45,t)` while X density is multiplied by a separate reveal beginning at 2.95s. This is a crossfade with spatial distortion and a directional wipe. The same matter is not transported from the controller to the X. The earlier description that the controller steam disperses into the X overstated that continuity.

The background is a separate procedural density field. Although it shares a noise function with the subject, it does not react to or generate the foreground material. Translucent pixels, temporal differences, shader construction and successful builds establish implementation behavior; they do not establish an impactful or accepted design.

## Rebuild requirements independent of the clarification

- Preserve the original controller silhouette, screen and three LEDs, neutral palette and silence unless the owner explicitly changes those requirements.
- Avoid further brightness/opacity retuning as a substitute for the requested rebuild.
- Show motion continuity in intermediate frames. If the direction is continuous transformation, use persistent material samples or a deforming surface whose positions can be traced from controller to X; do not introduce an independent X-mask fade and call it transformation.
- Make the background visibly related to the foreground motion while keeping the controller readable. A shared noise function alone does not provide that relationship.
- Review a short visual sequence before claiming subjective improvement. Keep compilation/behavior checks separate from art-direction acceptance.

No application edits were made during the initial rejection audit. The subsequent material-transport rebuild supersedes that candidate. Previous candidates remain rejected, not approved. No public checkout changes, commits, pushes or native writes.
