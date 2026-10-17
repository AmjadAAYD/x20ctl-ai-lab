# Intro rework, 16 October 2026

> SUPERSEDED BEGINNING: owner rejected the focus-only pass. Current opening is the vapor material / new steam-bank scene documented in intro-steam-material-20261016.md. Controller silhouette, screen, three LEDs and silence retained; old drawn-outline/tracing/scale-entry descriptions below are historical.

> LATEST FOCUS REFINEMENT: owner says fog overwhelms the controller and asks for researched impact. See intro-focus-research-20261016.md and `artifacts/intro-focus-review/intro-focus.mp4`. The simple controller/screen/three LEDs remain silent and neutral. Fog clears/slows locally, full contour is readable earlier, then anticipation, accelerated crossing and controlled X seating establish focus. Build/scoped checks pass; subjective acceptance pending.

> CURRENT OWNER CORRECTION (supersedes sound/foreground notes below): restore the simple intro controller, keep only its screen and three dot LEDs, remove sticks/D-pad/face buttons/grip details/green tint, let fog pass through its interior, and remove the floor shadow. Owner now explicitly asks to REMOVE sound and will handle it separately. Original outline geometry/motion and controller-shaped Agent remain; the intro's coloured blade/X light is neutralised too. No intro WAV is packaged, mute and Tools sound switch hidden, stored preferences preserved. Current preview: `artifacts/intro-jade-review/intro-fog-outline.mp4`, silent. Historical audio sources and mixes stay as inactive local work.

Current correction verified: product builds with the existing vibration/DPI warnings. Scoped WPF checks pass, including pixel comparisons against bare fog at both former sticks, D-pad, face buttons and floor-shadow area; neutral controller region; silence guard even with the sound flag enabled; reduced motion, Skip/handoff and retained Agent/Dashboard/navigation checks. First colour assertion incorrectly included the shared Skip label; inspection located all 20 out-of-range pixels in that label, so the check was corrected to the controller/fog region. Audio packaging check passes. `intro-fog-outline.mp4` is 7.1 seconds, 1280x800/30fps, video stream only. Reviewed the 1.9s and 2.4s outline/screen/LED renders. CPU-rendered evidence; no live GPU benchmark or owner acceptance claimed.

> LATEST OWNER STEERING: "Modern Steam Background, dont give me colorful background." Background and current filaments now use restrained graphite/steel, with moving sheets and depth retained. Jade/rose background washes are superseded. This correction concerns the background; the current foreground controller, audio, Agent, and handoff remain. A neutral-background regression check caps RGB channel spread at 8/255. Earlier colour descriptions and measurements below record the previous revision.

Neutral-background verification: shader compiles to 8,084 bytes; isolated field luma averages 46.9/255, maximum RGB channel spread 6/255, 62,406 intro pixels change and 23,170 fixed-phase shell pixels change. Reduced motion remains still. Current neutral preview is `artifacts/intro-jade-review/intro-graphite.mp4`; earlier `intro-review.mp4` records the superseded colourful background. This is a CPU-rendered preview, not a live GPU performance measurement.

Latest owner direction supersedes the silent/grayscale correction: rejection means improve the feature, not delete it. Keep sound. Keep colour without the rejected blue/orange split. Owner delegated colour choice and requested moderate brightness. No controller PNG in the intro, no rear flip, no controller-to-ball/explosion, preserve the controller-shaped Agent.

Later steering: use Steam as the background quality reference. [Valve's Library overview](https://store.steampowered.com/libraryupdate?l=english) and [Steamworks library assets](https://partner.steamgames.com/doc/store/assets/libraryassets?l=english) inspected for restrained depth, quiet regions and foreground separation. Field revised from the first uniformly green render to wider, smoother desaturated sheets over coloured graphite; jade/rose remain in reflected light and foreground details. Existing geometric seams softened. This is an interpretation of the reference, not a claim to reproduce Steam's renderer or subjective quality.

## Current implementation

- Drawn front-facing controller surface with bevelled controls, grip seams and moving reflected light, all clipped to the original controller outline. Three LEDs share the controller's entry transform. No hardware photo or raster artwork is inserted into the intro.
- Jade light, rose reflections and a pearl X face. Both native shader and coherent software fallback have moving folded sheets, crests and domain-warped density. The app uses a slower, dimmer version for text contrast. Field brightness is measured in the isolated fixture; this is a tuning guard, not a visual-quality rating.
- Controller contours shear into two blade laminae. Two continuous filament sheets open with that motion and evolve across the second half, including the logo hold and slogan. Original X silhouette, slogan and header handoff remain.
- New SND03 industrial edit, `Brand/intro-jade.wav`, replaces the previous mixes. Three low material LED dots, surface texture and stereo contour/landing cues. Sound toggle and mute work again. No new oscillator or generic success jingle. Credits and source-specific terms are in Brand/intro-audio-provenance.md; originals remain outside the repo because SND's audio terms prohibit standalone raw redistribution.

## Sources actually inspected this continuation

New owner list `f24ddea9-b03b-4f4f-bdf7-0c1ea2520a70/Pasted text.txt` read in full. Inspected SND README/sprite maps and three kits, site's audio terms and SND03 source WAVs; THISUX README/generator provenance; React Bits Aurora/Silk/Threads renderer source. Prior LuminaWall/GradFlow study is retained in atmosphere-source-review-20261016.md. These are technique/asset references, not a change of application framework or imports of their UI.

React Bits tree snapshot: `7381f14b15e8f9e00b01ce5b6ac4416f7ee360c4`; soundkit tree snapshot: `1e1556d7835828a325cf0f4b6cdd68d463164132`. Snapshots are tree identifiers, not claimed release commits. SND03 archive SHA-256: `3b4a342ac16d1dbdf1f73e4f9e5a119033cae98d31f2f55a8a8de8942461e7d3`.

## Review boundary

Isolated WPF renders use the explicit CPU field, not a desktop screenshot or hardware session. Current artifacts go under `artifacts/intro-jade-review`, separate from rejected historical previews. Tests cover field motion/exposure, reduced motion, no photo, packaged score, mute events, Skip once, handoff and the previously restored Agent/Dashboard interactions. No native writes, public-checkout changes, commits or pushes. Visual/listening owner acceptance and live GPU frame-rate measurement are not established by the tests.

## Verification on the final revision

- `tools/compile_atmosphere.ps1`: PASS, ps_3_0 resource 8,380 bytes, SHA-256 `c54f590abb237d8d2b2c81771bdf3b2c0a8e4da09639440a6fa63f98b9dda66a`.
- `dotnet run --project tools/wpf-ui-checks/UiChecks.csproj -v q -- --preview`: PASS. Product compiles; existing vibration-unused-variable and DPI-manifest warnings remain. Final field average luma 52.4/255; 62,608 changed intro pixels and 24,774 fixed-phase shell pixels; reduced-motion stable. Mute events, packaged track, 16 inspection frames and 213 video frames, Skip/handoff, Dashboard layout at both sizes, setup/artwork behavior, original controller Agent and tab overflow pass.
- `python tools/verify_intro_audio.py`: PASS. Source score and both build outputs identical; stereo 48 kHz / 16 bit, 7.1 seconds, peak -6.30 dBFS, no clipping and settled tail. No device playback or subjective listening test claimed.
- Visual review of controller, intermediate blade and X/slogan renders. Corrected a centre seam caused by treating each open controller half as a closed polygon before combining the silhouette.
- `git diff --check`: PASS, only line-ending notices.
- `artifacts/intro-jade-review/intro-review.mp4`: 1280x800, 30 fps, current score muxed at the app's .65 playback volume; isolated component render, not a live desktop recording. Previous preview filenames are historical.
