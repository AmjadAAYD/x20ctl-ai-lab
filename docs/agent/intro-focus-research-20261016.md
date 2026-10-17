# Intro focus and impact research - 16 October 2026

Owner: "Make it impactful, the fog is overwhelming the controller, try to find a solution, do a research on how to make the best animation intro."

Keep the accepted simple controller outline, screen and three white LEDs; neutral fog; no sticks, buttons, body fill or floor shadow; no sound. Preserve original X/type/slogan, Skip, reduced motion, seven-second duration and handoff. Scope is staging and timing, not another controller redesign.

## Research and application

1. [Microsoft Fluent motion](https://fluent2.microsoft.design/motion): choreography should establish visual hierarchy; timing/easing should reflect travel and give the viewer time to read the result. Application: show the full controller earlier, give it a readable hold, then accelerate the transformation and decelerate its finish. Ambient motion belongs behind that hierarchy. UI transition durations are not treated as a universal duration for a brand intro.
2. [Animation Mentor - anticipation, Chris Hurtt](https://www.animationmentor.com/blog/anticipation-the-12-basic-principles-of-animation/): a preparatory movement helps viewers expect and understand the main action; anticipation can be subtle. Application: a small 2.5% contraction before the existing contour release, followed by an accelerating expansion into its existing starting scale. No camera shake, flash, explosion or bouncing loop.
3. [Riot - VALORANT shaders and gameplay clarity](https://www.riotgames.com/en/news/valorant-shaders-and-gameplay-clarity): environment/subject rendering should preserve readability while balancing style and performance. Application: quiet and thin the fog where the foreground subject lives, leaving peripheral fog present and moving. This is an inference for an intro, not a claim that Riot prescribes these exact numbers.
4. Riot's [published VFX value-guide search excerpt](https://nexus.leagueoflegends.com/wp-content/uploads/2017/10/VFX_Styleguide_final_public_hidpjqwx7lqyx0pjj3ss.pdf) discusses value contrast and focal areas. The indexed excerpt was readable but opening the PDF redirected to the League home page; no full-PDF review is claimed. The direct Riot shader article above is the accessible primary source used for implementation.

There is no universal "best intro" formula. Here, the code showed a specific conflict: bright moving fog occupied the subject's space while the controller's lower opacity mask fell to 105/255, further multiplied by a .78 contour opacity. The full contour did not finish until about 2.45s, just as anticipation began. This left little opportunity to read a clear controller.

## Implemented treatment

- Intro-only subject-aware fog clearing. Local fog speed and density fall smoothly across the controller's region; the outer field retains its existing exposure and motion. A brief stronger clearance accompanies the blade crossing. Both HLSL and CPU paths use the same calculation; the shell's focus input is zero.
- Transparent controller body remains: the fog seen inside it is the same field as outside. The lower opacity mask now ends at 200/255; the neutral contour is slightly brighter/thicker. No floor shadow or new filled body.
- Full contour completes around 1.30s, then holds before the 2.45s anticipation. Small contraction 2.45-2.63s, accelerating release 2.63-2.95s, then slow peel / fast crossing / controlled blade seating. One 2.5% seating motion on the shared X transform completes the transformation. No geometry, colour, sound, duration or handoff change.
- Regression checks compare the focal region against the same fog without focal clearing, preserve peripheral exposure, check lower-grip separation, and retain interior-fog/no-shadow, neutral-colour, silence, reduced-motion and handoff checks.

Current review artifacts: `artifacts/intro-focus-review`. These use the CPU field for isolated WPF rendering, not a live GPU benchmark. Exact timing/clearance values are design choices to review, not measured audience preference or owner acceptance.

## Verification

- Shader compiled successfully, 8,408 bytes. Product builds; existing vibration-variable and DPI-manifest warnings remain.
- Scoped WPF checks pass. Compared with the same field without subject focus, focal fog average luminance falls from 49.9 to 29.5/255 (about 41%); peripheral exposure retains at least 95%. Lower-grip peak separation from fog is 172 and 169/255. Both former sticks, D-pad, face-button area and old floor-shadow region still show bare fog; controller region remains neutral.
- Field motion, reduced-motion stability/clock stop, silent playback guard, 16 still renders, 213 video frames, Skip exactly once, header handoff and retained Dashboard/Agent/navigation checks pass. Silence packaging and diff whitespace checks pass.
- Reviewed old/new controller stills at 1.9s and the new blade-crossing frame at 3.5s. Export is `artifacts/intro-focus-review/intro-focus.mp4`: 7.1s, 1280x800, 30fps, video only. SHA-256 `2ABC4E9E75BDF2113C3C33F4217483F2FB0CB10FD2B3C2CCA8A14CB565C1FBFB`.
- Live GPU frame-rate and owner subjective acceptance remain unverified. No commit, push, native writes or sound introduced.
