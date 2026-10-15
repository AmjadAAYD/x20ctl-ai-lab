# Intro refinement — 15 October 2026, second pass

Preserves the exact LeftHalf, LeftGrip, Arch and Mark(Point) source geometry.

## Motion

7.1 seconds: separated blue/violet contour fragments, progressive white controller reveal, 10% anticipation expansion, curling arc transformation (2.95–4.10 s), original X assembly with restored glow, coloured accent settle, slogan and header handoff from 6.0 s. One visual draws the 240 particles. Reduced motion, mute and Skip remain.

## Sound

The local preview uses the effect extracted from the user-supplied Steam recording, starting at source 2.25 s, aligned to the reveal/morph. This replaces the rejected synthesized music. See Brand/intro-audio-provenance.md. Regenerate with tools/build_intro_sound.py --reference <decoded-recording.wav>. Standard-library Python only. WAV: 48 kHz stereo, 16-bit, 7.1 s, peak -14.41 dBFS, silent final 100 ms.

## Research

Valve startup guidance: https://partner.steamgames.com/doc/marketing/pointsshopitems?l=english
Motion continuity: https://m1.material.io/motion/choreography.html

## Verification

Product build passes with six pre-existing warnings. Original controller strings and entire X geometry function match HEAD. Sixteen isolated WPF frames, reduced-motion construction, Skip exactly once and header handoff pass. WAV format, duration, peak and silent tail pass. These checks open no app window and capture no desktop. Live audiovisual quality remains for user review.
