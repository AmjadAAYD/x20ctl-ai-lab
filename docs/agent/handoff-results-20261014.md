# Handoff completion and measurements — 14 October 2026

Worktree: `x20ctl-ai-lab`, branch `ai-agent`. Main repo and the cancelled vibration fork were left untouched. No release or tag was created.

## Implemented

- Regression coverage for point parsing, shared curve interpolation and all presets, custom/advanced actions, diagnostics, promises, both existing semantic retries and follow-up memory.
- A quantitative guard for an explicitly small adjustment: the raw curve response may move by at most five percentage points (0.5 point tolerance for rounded percentage knots). Direction checks reject curves that contradict a precision/faster request at 25%, 50%, and 75% push. These checks use the shared `PadProcessor.Through`, not a second graph formula. Explicitly supplied numeric points take precedence. No claim that these checks prove every sentence correct.
- Removed copied curve-point examples from the Qwen prompt. The bounded experimental schema now accepts numbers through 150 (axis ratios) and free text for custom points, page names and on/off; the validator still checks action-specific types and ranges.
- Offline custom aim curves, mouse mode and page routing; explicit points, max output, vertical ratio and square gate; small changes start from the current curve. A pending clarification can use the next short answer, while a fresh standalone button/number is not a command. Numeric questions explain settings without changing them.
- Separate labelled canned demo, isolated in-memory preview, no model or saved-profile changes. Collapsible Lab/Safety: Qwen-only selector, temperature, editable system prompt + Reset, real-validator playground and limits. Teacher setup guide and 3–5 minute script in `demo-script.md`.
- Existing Dashboard photo/background, game scanning/manual add, and launch update checking retained. Fixed rescan races, cover fallback, helper-process false positives and window-sized game zoom. No time delay alone counts as a game launch. Fixed popup input handling and repeated install action.
- First non-neutral PC-mode use can invoke existing pinned installers quietly, once per service run, serialized with manual setup. Windows UAC remains necessary. Missing/unverified packaged installers are reported rather than fetched or launched.

## Verification

- `dotnet test desktop-dotnet/X20Ctl.Zone.Tests`: **403 passed, 0 failed**.
- `dotnet test desktop-dotnet/X20Ctl.Desktop.Tests`: **24 passed, 0 failed**.
- `dotnet build desktop-dotnet/X20Ctl.Product`: **PASS**, 0 errors, 6 existing warnings (unused vibration variable, unused invitation field and DPI-manifest warning, repeated by WPF compilation).
- No screen capture, review/stress harness, model download, controller operation or driver installation was performed. Visual appearance and real hardware remain unverified.

## Refreshed evaluation

Exploratory **one pass**, not the older three-repeat study. `X20CTL_AGENT_EVAL_REPEATS=1` ran all five variants plus drift, expanded M3/customization, docs and phrasebook generation sequentially. Default harness behavior remains three repeats. An early 18-turn run exposed contradictory sniping points and was stopped; it is excluded. The final bakeoff contains 135 turns, drift 5, M3/customization 18, and docs 23. Settings were not selected from this small sample.

Scoring now accepts custom curves that actually have the requested slow/quick/smooth response, rather than requiring a preset label. Thus the new scores are **not directly comparable** to the old preset-only scores. “Repaired” in the archived raw reports means any multi-call turn, including drift's expected follow-up; future report headings say “Multi-call”. The current default remains temperature 0.2 with the plain schema.

| Variant | Right requests |
|---|---|
| T 0.2 · schema | 24/27 |
| T 0.8 · schema | 26/27 |
| T 0.2 · bounded schema | 26/27 |
| T 0.8 · bounded schema | 23/27 |
| T 0.2 · no schema | 7/27 |

- Drift: **5/5**, with fixed fixture readings, not real-controller measurements.
- PC mode + advanced customization: **17/18**. Qwen repeatedly made a “slight” curve change too large; the guard blocked it.
- Document answers: see [current docs report](refresh-20261014/m5-report.md). The final pass uses the final README copied into `Help/`.

Default-variant misses: Make my right stick more precise for sniping, My aim feels too twitchy, Turbo on RB at 50 a second. The sniping curve contradicted the requested response and was rejected. The twitchy request ended in unnecessary clarification after a numeric-type rejection. Turbo 50 was wrongly replaced with 30: the replacement is within range, showing that a valid action can still misinterpret intent. This remains a known limitation; the app's applied/rejected line is authoritative and Undo is available.

The refreshed phrasebook generated 706 unique sentences, kept 534 and dropped 172. It was **not shipped** because it reduced development accuracy. New actions work through the builders/parsers and hand-written seeds. `OfflineFinal` was excluded from generation and not rescored; the 60-sentence development set is already known and is not an independent test. The 10 customization scenarios are regression examples, not a held-out accuracy claim.

| Offline data | Development | Customization |
|---|---|---|
| shipped | 51/60 | 10/10 |
| generated | 49/60 | 10/10 |
| merged | 50/60 | 10/10 |

Raw outputs, focused reports, phrasebook and comparison JSON are in [refresh-20261014](refresh-20261014/). They record the real measured results, including misses.

## Deferred and limitations

- Driver packages are absent in this development checkout. Quiet setup code is present; actual install/reboot/driver behavior was not tested. Release CI already downloads the pinned packages, but no release was run.
- No speculative firmware writes, Pro reverse engineering, paddle writes, stick-spin Easter egg, Linux port or public changes.
- Rocket League artwork, launcher variants, popup timing, zoom and taskbar cutoff need owner inspection. The taskbar issue was not reproduced; it is not claimed fixed. Existing work-area window sizing was retained.
- The model choice is intentionally only Qwen. Canned demo is scripted; offline is a finite phrasebook; neither is a pretrained universal chatbot. Session context is bounded to 60 messages and a 16K-token model context.

## Please look at

1. Open Agent XCTL and expand/collapse Lab/Safety: check contrast and whether it leaves sufficient chat space.
2. Enable canned preview, run the four scripted phrases, inspect the canned labels, Details, rejection and Undo; verify your real profile is unaffected.
3. In Offline, try mouse mode, diagnostics, custom point text and “slightly slower”; check that the curve page and applied values agree.
4. Rescan/add a game/folder, inspect Rocket League artwork, and Play: check full-window zoom and honest process status.
5. Check Dashboard versus Buttons artwork, the newer-version popup after intro, and controls near the taskbar at your normal DPI.
6. On a machine with the verified driver packages, test first-use PC mode and UAC; do not infer firmware compatibility from it.

## Implementation commits

- `8810665` Support explicit offline tuning and preserve numeric questions
- `c485dab` Fix game launch feedback and first-use PC mode setup
- `e794113` Add isolated canned demo and collapsible agent teaching lab
- `ceeb4db` Protect custom curve intent and extend offline controller actions
