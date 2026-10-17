> SUPERSEDED: owner rejected this photo/palette/score iteration, then clarified that colour and sound must be reworked rather than removed. Current implementation is documented in intro-rework-20261016.md: drawn front face, jade/rose reflected light, Steam-inspired layered background, and a new active score. The following records rejected history.

# Intro overhaul - 16 October 2026

Direct owner request overrides the attached background-only suggestion where it asks for a complete controller-section overhaul. Exact controller contour, X geometry, typography and slogan remain; controller motion/reveal and the atmosphere are revised.

The controller is larger, enters with a brief settling tilt/translation, and gains a full material reveal using the app's existing X20 Pro art beneath its original contour. LED resolution, recognition, material withdrawal and contour release lead into the two-blade X formation. The X hold and slogan retain the moving fullscreen field instead of reverting to black. Skip/mute/reduced motion and the existing header handoff remain.

AtmosphereView.cs shares one native domain-warped cloud/filament field between intro and app shell. Effects/Atmosphere.hlsl compiles to ps_3_0. The main technique reference is GradFlow Nebula/Aurora, with LuminaWall spectral/ribbon evolution. Source study is documented in atmosphere-overhaul-20261015.md. This is not a fluid-physics simulation or a bundled web engine. Native implementation requires no WebView/browser dependency.

The intro receives deterministic time from its own clock. The shell uses a slower/dimmer field behind its retained geometric seams. Hidden/unloaded/minimized shell backgrounds detach their rendering handler. Reduced motion displays a stable field. PS3-capable hardware uses the shader; software-only/unsupported rendering uses a bounded-resolution coherent field. Tuning constants: AtmosphereView IntroStrength/ShellStrength/ShellSpeed, shader palette/warp/filament coefficients, and field equivalents. Recompile after HLSL changes with tools/compile_atmosphere.ps1.

The active sound now uses finished UI SFX production OGGs unchanged apart from decode/gain/pan/cue placement. No procedural substitute/DSP/pitch/time effects are generated. See Brand/intro-audio-provenance.md and uisfx source/license manifest.

Verification: shader compilation; WPF tier2/PS3 construction on this host; deterministic software render changes in58196 sampled pixels between .5s and4.5s, stable reduced motion and stopped clock; isolated controller/X frames, Skip exactly once, header handoff, scoped UI checks; exact source-to-mix PCM reconstruction and packaged bytes. Offline video is a review artifact, not a live GPU frame-rate benchmark. Visual/listening acceptance remains pending.
