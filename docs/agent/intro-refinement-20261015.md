# Intro refinement — 15 October 2026

Scope: keep the exact controller path constants and the original Mark(Point) X from the existing intro. Use the supplied Steam recording for pacing, continuous transformation and sound-envelope inspiration. No Steam audio samples or logo geometry are used.

## Sequence

- 0.18–0.54 s: the original three LEDs wake softly.
- 0.35–1.95 s: blue leading strokes, restrained orange trailing light, then white contour recognition.
- 1.95–2.65 s: recognition hold and a 6% anticipation expansion.
- 2.65–3.65 s: the two controller contour halves flow into the original X. Their interpolation starts at the expanded controller so the handoff does not snap backward in size. A shallow curved trajectory vanishes at both endpoints.
- 2.65–4.65 s: one deterministic release of 150 small particles. One visual draws the whole burst; no per-particle UI elements or timers.
- 3.60–4.15 s: the two original X pieces fill progressively, with reduced bloom.
- 3.85–4.90 s: a masked light sweep settles across the X.
- 4.60–5.55 s: the existing slogan appears under the steady X.
- 5.55–6.65 s: the existing handoff carries the X into the real Zone header.

Separate heartbeat pulses and LED pings have been removed from the animated sequence and new score. Skip, mute preference, reduced motion and audio position synchronization remain. Audio resources are closed on completion or interruption.

## Sound

The committed WAV is used directly by the app; Python is not required to play it. Offline regeneration uses NumPy and SciPy in tools/build_intro_sound.py. The score is original procedural stereo synthesis: quiet filtered texture and a warm tonal bed, a first reveal swell, a stronger low morph swell, a restrained upper resolution, stereo room decay, and a silent ending.

48 kHz, 16-bit stereo, 6.65 s. Sample peak approximately -11.70 dBFS. The final 100 ms are silent. Repeated generation produced identical SHA-256: bb40a12e5de32c5e0ec535aab446acd41019ed36a0687457c18aa722af54e70e.

## Validation

- Product build passed with six existing warnings (unused vibration variable, unassigned agentInvite field, DPI manifest warning, repeated by WPF compilation).
- The LeftHalf, LeftGrip and Arch controller strings and the entire Mark(Point) function match the preceding commit byte-for-byte at source-text level.
- Fourteen isolated WPF component frames rendered successfully at fixed times; inspected controller trace, morph, final X and slogan. This opens no app window and captures no desktop.
- Normal and reduced-motion construction passed. Calling Finish twice emits completion once.
- An isolated IIntroHandoff fixture completed the header transition with finite values and one begin/end.
- WAV format, duration, bounded peak, silent tail and deterministic regeneration passed.

No full-app stress/review harness was run. Live playback smoothness, perceived sound quality and audiovisual synchronization still require user review on the actual machine.
