# Restore the shared blue/orange intro - 16 October 2026

The user explicitly selected the previously shared blue/orange controller-to-X video as the exact visual target. This supersedes subsequent charcoal/steam/neutral art-direction changes for the intro. Audio remains disabled as separately requested.

Reference: artifacts/intro-steam-inspired/intro-owner-rework.mp4, SHA256 C22CBD1C84A8A38218A0C995C51A90E5D067EEECC318851541EA26A349D92225. Adjacent PNG/preview files were overwritten by later work and are not authoritative reference images. Extract comparison frames from the MP4 itself.

Restored original controller contour reveal, .18/.36/.54 LED timings, PadWidth430, MorphAt2.95, SloganAt5.1, blue/orange blade formation, directional mark lighting, original background and header handoff. Core ProductExperience was reconstructed from HEAD1254a1f plus the exact blade-formation/light changes used for that video. IntroBladeFormation's original progress, trajectory, colours, strokes and seating were restored. Later shell/Dashboard/Agent changes and archived alternative renderer files remain untouched.

HasIntroScore remains false, sound flag defaults false, mute stays hidden and forced sound enabling opens no player. No intro track is packaged. The removed sound control is the intentional visual difference in the lower control strip.

Backed up the replaced ProductExperience.cs, IntroBladeFormation.cs and fixture Program.cs under artifacts/intro-before-preview-restore-20261016-123736. No reset, clean, branch checkout, commit or push.

Tests compare decoded MP4 reference images against current isolated WPF renders at1.90/3.30/5.40 seconds, excluding the bottom sound-control strip. Mean absolute channel differences are .817047/.996721/.868485 on0-255; the reference is lossy H.264. This supports matching those visual states rather than claiming byte-identical frame files or identical rendering on every display. Reduced motion, Skip once, header handoff, silence, retained Agent, Dashboard and navigation checks pass. The existing build warnings are the vibration variable and DPI manifest warning.

Current exported frames: artifacts/intro-preview-restored. Keep the shared reference video unchanged.

Full 213-frame video comparison completed: cropped visual region PSNR56.363502dB average, minimum50.012384dB; reference retains its original audio/mute control and restored export is silent. Comparison log: artifacts/intro-preview-restored/video-comparison.log. Restored app launch survived initial process check. No claim of byte-identical lossy encodes or display-independent pixels.
