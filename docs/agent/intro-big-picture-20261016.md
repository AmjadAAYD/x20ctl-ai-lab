# Big Picture direction - 16 October 2026

Owner explicitly asks to remove fog and the controller effect and use Steam Big Picture as the sole visual reference. This supersedes the material-transport/fog revision. Keep the controller itself, its screen and three LEDs, original X, silent playback, Skip, reduced motion, and real header handoff. Leave controller Agent and front-only Dashboard behavior intact.

Implementation: replace the shared cloud renderer with inexpensive charcoal vector surfaces and broad, restrained diagonal planes; replace the vapor controller with a plain vector silhouette and a controlled contour-to-X transition. No particles, vapor texture, glow, blur, floor shadow, photo or colorful wash. Use existing WPF stack and geometry. Render the sequence and run the affected isolated checks; do not equate passing checks with visual acceptance.

Reference: Valve's updated Big Picture announcement, https://store.steampowered.com/news/posts/?feed=steam_blog&enddate=1668711140 (27 October 2022), and its linked controller-first home-screen image. This is an X20CTL interpretation, not a copy of Steam assets or a claim to use Valve's renderer.

## Result and evidence

- AtmosphereView is now a WPF vector backdrop: a charcoal vertical gradient and two broad, faint diagonal surfaces. The shell uses the same backdrop; FacetBackdrop no longer adds the old faceted/star-field overlay. No pixel noise, cloud renderer, fog shader or density texture remains in the active path. Both fog/controller shaders are removed from packaged project resources. Archived rejected source files remain local but are not instantiated by the intro.
- ConsoleControllerView draws the original silhouette with an opaque graphite face, thin pale edge, small screen and three neutral LEDs. No glow, blur, floor shadow, raster controller image or particles. Its contour interpolates continuously into the original X between 2.65 and 3.80 seconds. The solid X seats without the previous aura or shine effect. Existing slogan and header flight remain.
- Product and scoped WPF fixture compile. Existing unused-variable and high-DPI manifest warnings remain. Scoped checks pass for background neutrality/movement/reduced motion, opaque controller/no active vapor renderer, sampled contour continuity, forced sound-off guard, Skip exactly once, handoff completion, retained Agent, Dashboard layout/artwork/setup chooser and navigation overflow. Audio packaging check and git diff --check pass.
- Initial opaque-body assertion sampled a coordinate in a render with inherited layout placement. The full preview showed a solid body; the corrected assertion measures the opaque area of the isolated body render rather than assuming its pixel offset. The rerun passed.
- Reviewed controller hold (1.90s), transition (2.90s/3.50s), and finished X/slogan (4.80s). Exported all 213 frames to artifacts/intro-big-picture/intro-big-picture.mp4: 7.10 seconds, 1280x800, 30fps, silent H.264. Complete video decode passed. SHA256 D712DD1194D293353A19EA59724C9734D59A3553DB72B76B64E5D254BC818DEA.

Evidence is from isolated offline WPF renders. Live application performance and owner visual acceptance are not claimed. No game launch, full-application stress harness, native hardware writes, public checkout edits, commit or push.
