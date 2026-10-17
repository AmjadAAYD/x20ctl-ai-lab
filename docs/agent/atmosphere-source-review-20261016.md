# Atmosphere source review - 16 October 2026

Read public source files directly after the aggregate GitHub page fetch stalled and was stopped. No repository was cloned into the public checkout. Source snapshots used for inspection are in the temporary xctl-background-references directory. Repo tree identifiers below are tree snapshots, not claimed commit hashes.

- [kun101/LuminaWall](https://github.com/kun101/LuminaWall) - src/presets/deepCosmos.ts; spectralDrift.ts; windowsBloom.ts; solarPlasma.ts; liquidGlass.ts. Warped cloud density, moving interference/ribbons, phase-controlled colour. Tree: 07537c084c8ead83139b241093c50be02bc68132.
- [meerbahadin/gradflow](https://github.com/meerbahadin/gradflow) - src/core/shader.ts: nebulaGradient, auroraGradient, causticsGradient, silkGradient. Main visual reference: warped gas bands, filaments, soft light rolloff and curtain motion. Tree: 756e55df1025b66e3fe3c72215cd249bda2ffc13.
- [enonforetsam/fluid](https://github.com/enonforetsam/fluid) - fluid-core/src/generated/shader.js: fieldFlow and fieldSmoke. Study of curl/domain-warped density; no physical fluid simulation is claimed. Tree: 369b9402c70e5dfdcf62bc647e0e0010f2e14edc.
- [23x2/generative-flow-field](https://github.com/23x2/generative-flow-field) - worker.js: GLSL noise/fbm and trail vertex integration. Study of field-driven movement; no particle trail engine is copied or imported. Tree: ee872419fb5e7eb40c56840c56dc913eeefc855d.
- [nonlineartom/gradient-backdrops](https://github.com/nonlineartom/gradient-backdrops) - references/techniques.md. Visibility-based clocking, synchronous first render, render scale and compositing cost. Tree: ddad93da04280f83f3cf25898a1f1cbdf001385e.
- [tkabalin/WebGL-Fluid-Background](https://github.com/tkabalin/WebGL-Fluid-Background) - webgl-fluid.js. Simulation/resize configuration reviewed; no WebGL runtime or simulation dependency introduced. Tree: ba1f616251029fc63365bea7275ce65f1b4c1c1f.
- [ruucm/shadergradient](https://github.com/ruucm/shadergradient) - packages/shadergradient/src/ShaderGradient/ShaderGradient.tsx. Separate field geometry, lighting and postprocessing roles reviewed. Tree: 68bdd740037a11c639f8b91fd775949c71d718d0.

Implementation is native HLSL with a CPU field fallback, using the reviewed visual techniques. No web UI, Three.js/WebGL engine or external shader source file is bundled. Shader source and matching CPU field are kept together for reproducibility; offline previews explicitly use the CPU path because WPF PS3 effects are not rendered by software RenderTargetBitmap. Main reference: GradFlow Nebula/Aurora, with LuminaWall spectral/ribbon evolution.

Audio source: [UI SFX](https://github.com/romainsimon/uisfx), audio-specific CC0 LICENSE-AUDIO read and retained, seven original OGG files retained with byte hashes. Code remains responsible only for decoding/playback/timing/gain/pan. No assertion that these sources were listened to or subjectively approved is made.
