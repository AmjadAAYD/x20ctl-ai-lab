# Owner rejection corrections - 16 October 2026

> SUPERSEDED BY THE OWNER'S NEXT CORRECTION: removing sound and turning everything gray did not meet the request. Rework and improve both. Owner delegated colour with moderate brightness. Current implementation and evidence: intro-rework-20261016.md. Do not treat the historical silence/grayscale notes below as requirements.

The owner rejects the X20 Pro photo in the intro, the blue/orange palette as a brand direction, and the sound score. These are hard constraints, not a request to choose a different pair of colours or a different stock percussion cue.

Removed the intro PhotoController material layer and its reveal timeline. The original controller geometry now animates as one drawn unit: contour, shaded vector silhouette and LEDs share the same settling/release transform. It is not a controller PNG. The controller-shaped Agent and static front-facing Dashboard controller remain functional and unchanged.

Intro contour, X-blade formation, X edge lighting, and both native/CPU background paths now use achromatic values. This removes the rejected hue pair from the intro and animated background; it does not rebrand every functional UI accent or recolour real game/hardware artwork.

The intro has no active score. HasIntroScore is false, playback is guarded even if the development sound flag is enabled, mute is hidden, the Tools sound control is unavailable, and licensed-intro.wav is removed from project output declarations and stale generated output. Existing user sound preferences are preserved. Source files and old previews remain historical rejected work; do not present them as the current design or regenerate/re-enable the score without a new accepted direction.

The current correction is silent and geometry-based. It must not be represented as owner-approved quality or a completed new art direction. Tests check achromatic fields, absence of the controller photo, silence guard, existing geometry/skip/handoff, and retained UI functionality. No commit/push or native writes.
