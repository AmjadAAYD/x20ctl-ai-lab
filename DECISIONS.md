# Architecture & Design Decisions Log

Format: one line per decision (what, why)

- 2026-11-02: Replace hardcoded 1000 HZ / 0.08 ms / 0.85 ms / NOMINAL fabricated benchmarks in TideTester.cs with calm truthful placeholders ("—", "0 / 17", "Waiting for input") to obey Rule 2.1 & 5.11.
- 2026-11-02: Completed Tide review suite verification (0 failures, 0 binding errors, 658 unit tests passed) and committed on gemini/theme-repair-20261029 (a655635) to satisfy 09_AUTONOMOUS_MODE start condition.
- 2026-11-02: Created branch feature/themes-hollow-nocturne-sunrise and dedicated worktree worktree-hollow-nocturne-sunrise based on commit a655635 per 09_AUTONOMOUS_MODE instructions.
- 2026-11-02: Extrapolated all Hollow theme pages (Dashboard, Buttons, Curves, Macros, Vibration, PC Mode, Device, Mouse, Setups, Tester, Diagnostics, RGB, Screen, Tools Hub, Themes, About, Feedback, Scans, Notices) from refs/hollow.png and 03_HOLLOW_SPEC.txt using procedural gothic vector filigree and ivory-silver palettes.
- 2026-11-02: Generated Hollow Brand assets (logo.png 752x523 with cleaned transparent emblem, spec.json, silent 1920x1080 intro.mp4 with parting mist animation) using Python and bundled ffmpeg.
- 2026-11-02: Wired Hollow into ThemeCatalog, StudioScene, ZoneScene, DockView, ToolsHub, FeedbackPage, ModelSelectorView, ProductHost, StickToolsPanel, AgentPanel, Scanner views, and ThemeReview harness.
- 2026-11-03: Resolved workbench API bindings for Hollow (Vibration strength slider, Curves bezier draft, Macro slots/record, Mouse actions, Setups actions) and achieved 0 build errors.
- 2026-11-03: Generated Nocturne Brand assets (logo.png 752x523 with cleaned transparent emblem and slanted NOCTURNE wordmark, spec.json, silent 1920x1080 60fps intro.mp4 with diagonal electric blue/white shards) using Python and bundled ffmpeg.
- 2026-11-03: Extrapolated all Nocturne theme pages (Dashboard, Buttons, Curves, Macros, Vibration, PC Mode, Device, Mouse, Setups, Tester, Diagnostics, RGB, Screen, Zone, Tools Hub, Themes, About, Feedback, Scans, Notices) from refs/nocturne.png and 04_NOCTURNE_SPEC.txt using procedural electric blue fields, -12° slanted white cards, and heavy italic typography.
- 2026-11-03: Wired Nocturne into ThemeCatalog, StudioScene, ZoneScene, DockView, ToolsHub, FeedbackPage, ModelSelectorView, ProductHost, StickToolsPanel, AgentPanel, Scanner views, and ThemeReview harness with 0 compilation errors.
- 2026-11-03: Generated Sunrise Brand assets (logo.png 752x523 with cleaned transparent emblem and concentric sun ring, spec.json, silent 1920x1080 60fps intro.mp4 with retro TV CRT power-on and halftone ripple animation) using Python and bundled ffmpeg.
- 2026-11-03: Extrapolated all Sunrise theme pages (Dashboard, Buttons, Curves, Macros, Vibration, PC Mode, Device, Mouse, Setups, Tester, Diagnostics, RGB, Screen, Zone, Tools Hub, Themes, About, Feedback, Scans, Notices) from refs/sunrise.png and 05_SUNRISE_SPEC.txt using sunshine yellow base, procedural halftone dots, cream sticker cards, and retro TV bezels.
- 2026-11-03: Wired Sunrise into ThemeCatalog, StudioScene, ZoneScene, DockView, ToolsHub, FeedbackPage, ModelSelectorView, ProductHost, StickToolsPanel, AgentPanel, Scanner views, and ThemeReview harness with 0 compilation errors.
- 2026-11-03: Converted NocturneKit.NocturneButton to native WPF Button and normalized notice button tags to notice:no / notice:yes to pass ThemeReview accessibility assertions.
- 2026-11-03: Normalized ControllerArt model lookup in HollowKit, NocturneKit, and SunriseKit to safely match substring keys (x20, x15, x10, x05, d10, dune) and always return a valid FrameworkElement to prevent null reference exceptions during off-screen rendering.
- 2026-11-03: Successfully executed full review harness on Hollow (exit code 0, 29 shots, 0 binding errors, notice-regression passed, mapping-regression passed).
- 2026-11-03: Successfully executed full review harness on Nocturne (exit code 0, 29 shots, 0 binding errors, notice-regression passed, mapping-regression passed).
- 2026-11-03: Successfully executed full review harness on Sunrise (exit code 0, 29 shots, 0 binding errors, notice-regression passed, mapping-regression passed).
- 2026-11-03: Verified full test suite (658/658 unit tests passing, 0 failed, 0 skipped) across Product.sln.
- 2026-11-03: Rebased worktree feature/themes-hollow-nocturne-sunrise onto tip commit 736ec92 (codex/diytool-screen-20261101) to integrate real ApplyToController, Device telemetry, LightingWindow, ScreenWorkbench, and FeatureNavigation.
- 2026-11-03: Wired real ApplyToController on Buttons, Curves, and Macros across Hollow, Nocturne, and Sunrise when ControllerWrites is true, and added LoadFromController on Macros.
- 2026-11-03: Removed fake Apply to controller buttons from PC mode and Vibration across all three themes, preserving native app behavior.
- 2026-11-03: Replaced hardcoded telemetry (1000 Hz, 84% battery, 4.12V, 1.1ms jitter/latency, v1.34/v2.14 firmware, 'Profile synchronized') with real Device bindings and truthful placeholders across footers, Device, Diagnostics, and Zone chips.
- 2026-11-03: Replaced crude vector box controller art on Sunrise with real high-resolution controller image on cream pedestal with concentric sun rings in SunriseKit.ControllerArt.
- 2026-11-03: Rebuilt Hollow cavern backdrop with layered vector stalactites/arches/pillars/mist and created HollowPrimaryApplyButton with wide dark card and 4-point diamond ornaments matching refs/hollow.png.
- 2026-11-03: Fixed left-edge slanted card clipping on Nocturne Buttons ('FACE / A', 'YOU PRESS:', 'GAME RECEIVES:') by disabling ClipToBounds and adding dynamic slant padding in NocturneKit.NocturneWhiteCard.
- 2026-11-03: Fixed Sunrise Buttons TV panel layout and Apply bar overflow by balancing TV card padding and applying responsive width in SunriseKit.CreateTvScreenCard and SunriseButtons.cs.
- 2026-11-03: Wired real Device.RefreshDetailsAsync() and real 2-step FactoryReset(say) confirmation flow, registering Device.Watch across Hollow, Nocturne, and Sunrise.
- 2026-11-03: Re-executed full test suite (823/823 passed, 0 failed, 0 skipped) across Product.sln.
- 2026-11-03: Re-executed windowless off-screen review harness for Hollow, Nocturne, and Sunrise (all 29/29 shots rendered, 0 binding errors, notice and mapping regressions passed).
- 2026-11-03: Rewrote REPORT-hollow.md, REPORT-nocturne.md, and REPORT-sunrise.md with honest accounting of what was mocked before and what is real now.
