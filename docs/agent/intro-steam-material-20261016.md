# Beginning rebuilt as steam - 16 October 2026

Owner rejects the preceding contrast/timing pass as insufficient and explicitly asks to rework the entire beginning, change its background and give the controller the same steam effect.

This supersedes the previous drawn-contour treatment. Preserve the original controller silhouette, screen and three white LEDs; no sticks, buttons, photograph, colours, floor shadow or sound. Preserve original X, slogan, seven-second duration, Skip/reduced-motion and handoff. Controller-shaped Agent and Dashboard hardware artwork are outside this change.

## New opening

`SteamControllerView.cs` uses the original controller and mark geometry to build signed-distance fields in memory. They are vector-derived masks, not a supplied controller PNG or photograph. A new native `SteamController.hlsl` material, with a matching CPU path, shapes turbulent translucent vapor around that silhouette. Its edge rolls, varies in density and sheds wisps; no animated solid outline is painted over it.

The vapor rises into the controller shape, holds with the screen/LEDs, then disperses along opposing diagonal currents as the original X condenses from the same material. This replaces the old vector tracing, generic tilt/scale entry, point-interpolated blade morph and crossing filament overlay. There is no circular intermediate or explosion. The existing solid X hold/type/header handoff remain after the vapor resolves.

The intro background is a new scene in `Atmosphere.hlsl` and `AtmosphereField.SteamBanks`: rising lower banks, a lighter upper current, curled narrow wisps and a calm subject region. It shares the noise/turbulence family and time direction with the controller material. This is procedural steam-like rendering, not fluid-physics simulation. The app shell retains its prior renderer by setting IntroScene=0.

Both shaders compile with `tools/compile_atmosphere.ps1` (default Atmosphere, `-Shader SteamController` for the new material). PS3 is used when supported; isolated/offline review uses matching CPU rendering. Mask generation is cached at construction. The material stops rendering after its 4.55s dissolve and is hidden under reduced motion. No extra rendering clock or background service is introduced.

## Review and verification

Current artifacts: `artifacts/intro-steam-material`. The initial scoped pass builds and passes field motion/neutrality, actual translucent material change over time, lower-grip visibility, silence, reduced motion, Skip/handoff, Agent and Dashboard/navigation checks. The first compile failure was a duplicate test-local name, corrected to silhouetteField. Still review found a seam where the two release directions used a hard sign switch; replaced it with a smooth spatial blend before final export.

Final preview: `intro-steam.mp4`, silent, CPU-rendered review rather than a live GPU recording. Source WAVs remain inactive. No native writes, public checkout changes, commit or push. Owner subjective acceptance and live GPU performance remain unverified.

Final verification: Atmosphere compiles to 11,404 bytes; SteamController to 4,828 bytes. Both shader constructor paths succeed on this WPF tier-2 host. Product and scoped checks pass with the existing vibration-variable and DPI warnings. The isolated steam controller has 26,897 translucent pixels and 12,067 pixels change between 1.9s and 2.15s; lower-grip separation from the background is 186/183 levels. Intro backdrop motion changes 51,049 pixels; reduced motion stays still. Sound packaging, Skip/handoff, Agent, Dashboard and tab checks pass. Still review confirms the release seam is gone. Export: 213 frames, 1280x800/30fps, 7.1 seconds, video only; SHA-256 `8D847DECB391D15FFA13987E5BA7B6F7C456CF8D7C4951D77DFC6A2E982F171D`. These checks do not establish owner visual acceptance or live GPU frame rate.
