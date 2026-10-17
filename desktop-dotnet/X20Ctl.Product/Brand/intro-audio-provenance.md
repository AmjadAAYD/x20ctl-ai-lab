# Intro audio status - 16 October 2026

> CURRENT: the owner explicitly requested removal of the sound effect and will manage audio separately. HasIntroScore=false; no intro WAV is packaged; mute and the Tools sound switch are hidden. Stored preferences are preserved. Sources/mixer files below are inactive historical work, not the current product sound.

The owner's latest correction requires reworking sound, not removing it. Active score: `intro-jade.wav`, stereo 48 kHz / 16 bit, 7.1 seconds. Sound and mute controls are available again; stored preferences, reduced-motion silence, development-fixture silence and Skip fade remain.

Source: [SND](https://snd.dev/), SND03 industrial kit, obtained from https://snd.dev/assets/sounds/SND03_industrial.zip. SND's site identifies collaborating sound designers; these are finished source assets, not X20CTL-generated oscillators. SND01 sine and SND02 piano are not used. The new THISUX reference was inspected but its Python-generated kit was not selected.

**SND's assets are not CC0/MIT audio.** The site permits use in personal/commercial products under its own terms, prohibits standalone unprocessed redistribution and unprocessed use as a trademark sound logo. Only the edited score is included here. Raw ZIP/WAV sources stay outside the repository in the temporary reference folder. This startup edit is not a registered sound trademark. Copyright remains with the credited designers/Dentsu. Read current terms at snd.dev before redistributing any raw material. The JS package's MIT label must not be used as an audio license.

Processing: decode to stereo PCM, timing, six-ms edge fades, gain, moving pan envelope and 16-bit export. No synthesis, pitch/time alteration, filtering, saturation or added reverb. Three low material dots follow the LEDs; low moving texture follows the surface reveal; opposing transition textures follow the blades; a restrained seat and handoff finish the sequence. There are no cinematic impact or completion-jingle samples.

Reproducible mix: `tools/build_intro_jade.py --sources <extracted SND03_industrial folder> --ffmpeg <ffmpeg.exe>`. `src/assets/audio/intro/jade-cue-sheet.json` records exact source WAV hashes, cue times, gains, pans, master gain, metrics and output SHA-256. `tools/verify_intro_audio.py` checks packaged byte identity, stereo, LED timing, duration, headroom and settled tail. Checks do not establish subjective listening approval.

Earlier mixes, sources, scripts and videos are retained as rejected history. `licensed-intro.wav` is not the active track and is not packaged. Do not restore the old score over this one.
