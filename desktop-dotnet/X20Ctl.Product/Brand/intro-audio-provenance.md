# Intro audio provenance

original-intro.wav is generated entirely from mathematical oscillators and deterministically seeded noise by tools/build_intro_sound.py. It reads no source recording or sampled sound. The design uses air sweeps, a low impact, inharmonic metallic transients and short stereo reflections. It contains no Steam audio samples.

The previously copied intro.wav has been removed from source and local build outputs, and the app now loads a different filename. That earlier file remains in private Git history; do not distribute builds from commit 538a2e3.

Regenerate: python tools/build_intro_sound.py (NumPy and SciPy required only for regeneration).
