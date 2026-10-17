# Claude handoff — X20CTL / Agent XCTL — 17 October 2026

## Scope and first actions

Amjad requested a handoff covering **everything added and changed so far, without exception**. This guide describes the current implementation, history, decisions, evidence and limits. Its supporting snapshot contains every tracked pending change and every nonignored untracked file, not merely the files discussed in prose. Read the file inventory and patch too.

**Actual working directory:** `C:\Users\amjad\Desktop\Study\Other\Projects\Personal\x20ctl-ai-lab`

- Branch `ai-agent`; HEAD `1254a1fcbc67553a42a5cfbc91773047bd285460`.
- The environment cwd under `Study\Projects\Personal\x20ctl` is not this lab. Always specify the lab directory.
- Main/public checkout separately under `Study\Other\Projects\Personal\x20ctl`; leave its unrelated intentional changes alone.
- Source is substantially **uncommitted**: 56 tracked modified/deleted paths; snapshot includes 136 individual pending paths, 135 existing files plus one deletion. Git status compresses untracked directories, hence 103 status entries.
- Never reset/discard/clean/checkout over this work. Do not apply the patch to the already-modified checkout.
- This request authorizes the handoff, not new implementation, commits, push, release, model downloads or hardware operations.

## Complete supporting package

Lab folder `artifacts/claude-handoff-20261017/`, also packaged as a ZIP beside it:

| Item | Purpose |
|---|---|
| HANDOFF.md | This current-state guide |
| working-tree.patch | Full-index, binary-capable HEAD→current tracked changes, including deletion |
| staged.patch | Separate staged-state evidence |
| current-files/ | Exact current copies of every changed existing tracked file and nonignored untracked file |
| changed-files-manifest.json | Path, existence/deletion, byte length, SHA256 for each pending file |
| git-status.txt / tracked-change-stat.txt | Frozen pending state |
| tracked-files.txt | Complete tracked path inventory |
| git-log-all.txt | Complete locally available history across refs, dates/authors/messages/file statistics |
| reference-docs/docs/agent/ and docs/plans/ | All available notes, earlier handoffs, evaluation outputs and plans |
| user-references/ | All 33 screenshot references from this conversation plus two earlier pasted briefs |
| reference-availability.json | Original reference paths and availability, 35/35 available |
| snapshot-metadata.json | Workspace, date, HEAD, scope and counts |
| PACKAGE-SHA256.json / PACKAGE-VERIFICATION.txt | Payload integrity and verification result |

Ordinary git diff omits untracked files; **patch alone is insufficient**. This is an evidence handoff, not a standalone clone or portable app. Committed source/history remain in local Git. Ignored build/cache/artifact/local-store contents and full raw chat logs are not included. Original rendered artifacts remain under their paths in this checkout. No claim is made to reconstruct unavailable historical utterances; complete Git-visible pending file coverage is independently recorded.

## Latest completed request

**Remove diagnostic tab** was implemented immediately before this handoff:

- Nine tabs: **Dashboard, Buttons, Curves, Macros, Vibration, PC mode, Device, Mouse, Setups**.
- Diagnostics removed from tab bar, More overflow, LB/RB cycling and PageUp/PageDown indices.
- `DiagnosticsNav` remains a collapsed named RadioButton for existing references/direct help actions; Diagnostics functionality remains.
- Collapsed Diagnostics uses the same exclusive radio group but is absent from navigation/overflow arrays.
- Direct Diagnostics view hides the shared tab indicator instead of leaving an old underline. Returning to a normal tab restores it.
- Device/Open Diagnostics/help/Agent routes remain.
- Updated tests assert nine routes, no Diagnostics cycling/overflow and no stale indicator. Product compilation and followup checks passed; private app reopened, PID31824 was alive after2sec. This is historical process evidence, not proof that PID still runs now.

Immediately preceding request: **“Revert to how it was and keep the darker modern color.”** Earlier long wing geometry/placement restored, dark glossy navy retained. Do not restore bright blue or the subsequently rejected short/head experiment.

## Owner preferences and constraints

- Navy native product, original actual controller artwork; closely follow reference buttons. Drawings illustrate geometry to improve, not scribbles to paste literally.
- Current commands: thick capsules, important light blue, secondary gray. Support warm coral/orange; LB/RB dark glossy navy.
- Both bumper sides must match size, vertical placement and finish. User repeatedly asked sharper/thinner/sleeker/lower; latest revert determines current geometry below.
- No fake hardware/connection/battery percentages/firmware/read-back/calibration/game launch/applied-state claims. Model text is not evidence.
- Agent belongs inside native app. Qwen-only/Ollama for private school project; eventual shipped assistant intended offline-only. No automatic model downloads or model substitution.
- Preserve intentional dirty work. No public-origin push, release tags, main checkout edits or coauthor trailers. Old private-push instructions are not new authorization.
- Stop only exact lab executable, not every X20Ctl process.
- No full-app review/stress harness or desktop screenshots. Scoped WPF component renders/fixtures are established verification.
- No copied Steam audio. Current intro silent; do not revive rejected sound.
- Keep firmware writers gated pending identity, reversible operation, ACK, read-back, persistence and restoration. X20 Pro firmware protocol remains unverified.
- User wants direct execution, proportionate checks, short factual updates, no repeated permission ceremony, no flattery or invented results. For substantial new work, Markdown plan first.

## Architecture and committed work

Native .NET10/WPF Product is `desktop-dotnet/X20Ctl.Product`, assembly X20Ctl, version4.1.0-preview.4. WinForms only supports tray. `X20Ctl.Zone` is domain/state/processing/Agent with unit tests; Desktop supports native integration.

ProductWindow/ProductHost own shell, engine/routing, battery/update flow and Zone/Studio. ZoneScene/DockView own four player assignments. StudioScene owns pages, drafts, model/player context and input ownership. PhotoController is the actual native/layered model renderer. PC mode processes software profiles into virtual Xbox output; it is not controller firmware configuration.

Product build requires `artifacts/product-engine-phase/native-build/Release/x20ctl-engine.exe`; project deliberately fails without it. Do not remove this guard. If needed, build native-core Release target x20ctl-engine. Managed virtual client is Nefarius.ViGEm.Client1.21.256. Driver packages may be absent in dev checkout; do not invent installations.

All committed history is in git-log-all.txt. Notable milestones:

- `7c9e364`, `9ae9c0b`: agent custom/advanced actions, questions/history, actual application/diffs/Undo.
- `a6d2f28`, `baa8476`: shared runtime/graph response, contextual curve labels, reduced chat, source cards removed.
- `ceeb4db`: custom intent protection/offline actions; `e794113`: labelled canned demo and Lab/Safety.
- `c485dab`: game launch feedback and first-use PC setup; `8810665`: explicit offline tuning/nonmutating numeric questions.
- `29cb452`, `bd4fc56`, `935ddfd`: prompt replies/Try it/update notice, settings screen/roundtrip reduction, curve reset corrections.
- `2eb0868`, `6c4f6d4`: scanner separation, scan/entry polish, offline followups.
- `e4897f1`: work-area/taskbar-safe maximized controls/scanner clipping; `a279a45`: required live engine/actual input state.
- `0443123`, `b3052bb`, `29111b4`, `7155315`, `d000e4e`: Dashboard fitting/title space, contour highlights, model macro slots including ProM5/M6.
- `538a2e3`: historical Dashboard/intro used copied reference audio. **Do not distribute that build.**
- `48ce88b`: copied audio removed, oscillator/noise replacement then rejected.
- HEAD `1254a1f`: Dashboard setup panel/X aura; dirty implementation supersedes that UI/intro/audio.

## Agent XCTL and class project

Domain `X20Ctl.Zone/Agent`: AgentSession, proposals/schema/ProposalValidator, AgentApplier, knowledge, instant commands, offline builders, docs retrieval/evaluations. Product Agent UI: AgentPanel/AgentTour/OllamaClient and original controller-shaped presentation.

- Local Qwen structured JSON (`think:false`, schema), strict parsing, type/range/model/action validation, pure application, profile validation, actual diffs, Undo and app-authored status.
- Custom two-knot curves, advanced output/axis/square gate, supported remaps/macros/turbo, Mouse/page commands, drift hooks/explanations/followups.
- PadProcessor.Response/Through shared by runtime and graphs. Never introduce a separate graph curve formula.
- Semantic retries for promises without changes and unrequested preset substitution; keep retry only if it changes something.
- Small-change guard max5 percentage-point raw response movement with rounding tolerance, direction sampled25/50/75%; explicit numeric points take precedence. Does not prove all intent correct.
- Session60 messages, bounded16K model context; applied notes retained. Pending clarification uses short reply; standalone button/numeric question is not an implicit change.
- Offline finite phrasebook/builders support custom points/aim wording/advanced/mouse/pages/followups. Not a universal chatbot.
- Canned demo labelled and in-memory, no real saved-profile/model changes. Lab/Safety: Qwen selector, temperature, editable prompt+Reset, actual-validator playground, limits/privacy. Teacher guide/demo-script retained.
- Historical installed qwen3.5:9b, nomic-embed-text, Ollama0.34.0; **not rechecked during handoff**. Earlier deadline15Nov2026/freeze12Nov are prior project notes, not freshly verified schedule.
- Intermediate right drawer was superseded by explicit restoration of original **controller-shaped Agent**. Current top-left Agent beside brand opens it; Dashboard invite/help bubble removed. Reduced motion forwarded.
- Agent Mouse commands use current player/model takeover, return missing-source errors, cannot claim another player's state.
- Future offline-only shipped direction does not mean remove private school Qwen path now.

Historical evaluation, not rerun for handoff:

- Early Zone286/Desktop24, later Zone403/Desktop24 in handoff-results-20261014.md.
- One-pass27-request variants: T.2/schema24; T.8/schema26; T.2/bounded26; T.8/bounded23; T.2/no-schema7. Fixture drift5/5; advanced17/18. Changed curve-shape scoring not comparable to older preset scoring. DefaultT.2/plain schema retained.
- Known errors: contradictory sniping curve rejected; twitchy aim unnecessary clarification; requested turbo50 incorrectly replaced with valid30. Schema validity is not intent accuracy.
- Generated phrasebook706 unique, kept534/dropped172; **not shipped** because development scores worse: shipped51/60, generated49/60, merged50/60, each10/10 customization. Development is not held-out. OfflineFinal excluded/not rescored. Raw reports archived under refresh-20261014.

## Shared design, Support and time

Design.xaml/Controls.xaml/StudioConsole.xaml, new ZoneSurfaces.xaml, DesignControls/ConsoleKit:

- ActionButton/PrimaryButton/IconButton/SegmentOption/SegmentedControl/StatusTag/GamepadKeycap; old DS/Zone aliases resolve shared controls.
- Main commands min42px, SegoeUI14 SemiBold, capsule padding. Primary #4598F5→#287CE9 edge#78B7FF; secondary#30384C→#272E40 edge#434C63.
- True capsule radius HalfHeightRadius bound to actual height/2. **Fixed999 was wrong in WPF and made stretched ovals.** Physical face buttons remain circular.
- Subtle tonal shading/rim,18% blue hover wash, keyboard focus; white selected segments/dark text; no selection text resizing.
- Dashboard state/games, showcase families, Curves stage/shelf, Device use matching navy panels. No new UI dependency.
- Primary/gray aliases propagated through scanner, PhoneGuide, WriteScan, Tools, Feedback, model picker, macro/setup/PC/management screens, preserving handlers.
- Support approx166x42 warm pink/coral/orange capsule, white heart disk, trailing outlined menu badge/nested rim. Existing action retained. Older30/32px docs superseded.
- New CyclingSupportOutline cycles **rim only** over5sec while loaded, stops unloaded, reduced motion static. No actual shaking position.
- New HeaderClock in Zone/Studio: actual local HH:mm,1sec timer only loaded, real date/timezone tooltip. Not fake telemetry.

## Navigation and current bumpers

New StudioNavigation.cs plus StudioScene XAML/code and bumper styles:

- Plain labels and short white underline;210ms cubic X/width movement. No glass selected pill, scale/stretch/sheens/page-entry shake.
- Cached destinations stop duplicate layout notifications cancelling animation; measured More overflow, no wrapping.
- Nine routes after Diagnostics removal. Old docs/tests saying ten are historical.
- Navigation band86px, margin26,6,26,8. Both wing Buttons148x50 at top offset22. Both span grid to fix LB clipping by76px column.
- Restored path `M 4,15 L 14,3 L 98,3 L 143,48 L 123,48 L 86,16 Z`; fine edge `M 6,14 L 15,4 L 97,4 L 141,47`.
- Left mirrors scaleX−1, right+1, center origin. LB previous on left; RB next on right. Shape-swap experiments did not reverse semantics.
- Left x0/y22; right x=max(0,76+NavigationTrack.Width−86)/y22. Label11 SemiBold, content width40; left margin72,2,0,0, right34,2,0,0.
- Gloss #263F60@0/#192F4D@.22/#10253F@.5/#09192D@1. Hover#355777/#203C5A/#122B43, stroke#476482; fine#96B5D3 opacity.35 thickness.6.
- Mouse/Diagnostics capture hides bumper controls but reserves coordinates. Direct Diagnostics hides underline.

## Zone, assignments and picker

DockView/ZoneScene/ZoneState/ProductHost/ModelSelectorView/new ZoneBackdrop/ZoneSurfaces:

- Four persistent slots, actual art/assignment/enter/focus behavior retained. Navy shared cards/environment; hard polygons/seams/slashes/placeholder glow removed.
- Slow ambient backdrop capped15updates/sec; hidden/minimized/unloaded stop, reduced static. Later navy pass supersedes original charcoal token palette.
- Player titles24focused/20grid/16compact, meaningful disconnected/manual/mock states, no assignment=connection assumption.
- Shared Switch/Add/Enter commands. All8 picker models2rows×4; hover zoom/gloss/glow removed.
- Empty silhouette/dots/+ retained; dot/+ oval background now **Transparent** so blends into card while glyphs/hit targets remain.
- Empty controller plus Add moved down together18–30px, room-bounded; details bottom band retained. Do not move only one component.
- Remove labelled: compact below connection status, focused beside Enter Studio bottom row, hidden empty.
- ZoneState.Clear removes one assignment/collapses removed focus, leaves4slots. ProductHost persists other assignments; failure states session-only. Saved setups/drafts retained, no physical disconnect/write.
- Reduced-motion forwarding fixes stale focused art. Original pixels retained, synthetic edge overlay removed; faint presentation reflections not RGB telemetry.
- Header separator/debug footer simplified; footer navigation hints; update utility notice preserved.
- Zone→Studio artwork flight removed; direct Studio entry and completion callback retained.

## Dashboard, games and setups

- Larger PhotoController in Buttons-style panel. Front default, explicit Flip top-right creates rear on demand. Manual flip supersedes older front-only restriction, no automatic timer.
- Duplicate model/player headings/pills/Configure caption/Agent invite removed. Top identity/status. Ready to play and actual Games/Changes/compatible Setups counters restored during earlier pass.
- One selected-game context: title/store, compatible setup select/load, Edit, Change icon, Play. Scan/manual add/custom-icon persistence preserved.
- Native decode removes forced900px decode, HQ scaling, no fabricated higher-res assets/original replacement.
- **Equal square cover frames**, same visual size as Genshin: width=height=round(coverHeight), UniformToFill intentionally crops portraits. Older native-aspect/uncropped/portrait instructions superseded.
- coverHeight clamp(height*.25,110,190), cover row+130, title min130, horizontal overflow. Add game/folder/Play light blue, Rescan gray.
- Earlier fixes: scan race, fallback artwork, helper-process false positives, honest observed-process launch feedback; delay alone is not launched.
- Dashboard setup choices/count exact model filtered; returning from edits refreshes it; actual working setup/edit state, no unsupported native Apply success.
- SetupsWorkbench lists/counts/active/selected exact model ID. **Never mix X20/X20Pro.** Reject foreign selection/load/native or mixed/foreign legacy imports before mutation; clear stale foreign active/selection.
- Shared store preserves other-model records. Native schema creation still limited to supported model; no relabelling X20 snapshot as Pro. Legacy exact-byte backup/conversion limits retained. PC profiles distinct from native snapshots.

## Buttons, Curves and advanced tools

- Compact You press→Game gets editor, contextual outputs/testing, selected label, modified markers/scoped restore.
- **Keep graphical showcase**: Face/D-pad diamonds, shoulders/sticks/system/special, each navy family panel. Visible before selection, assigns after selection. Do not replace it with text grid.
- Physical capture selects source and blocks navigation/Home. RemapCapture uses native tester-capture context; no button leak.
- PC-only NONE/Disabled target validated by ButtonDraft/PcModeProfile/restored by PcModeDrafts; PadProcessor suppresses buttons/triggers including light feathering. No native NONE firmware command.
- Curves runtime response shared. Stable larger graph, channel/preset mini previews/toolbar, trigger labels, in/out readout/offline clearing.
- Percentage point entry corrected1%/5% steps. Parameters moved to shelf; selecting/editing does not shrink graph. Latest fixture298.88×175.81 stable; older408×240 belongs different layout.
- Advanced one selected stick, accurate range/direct-input/axis labels, graph, copy/Undo, link sticks, raw/bypass disables tuning, measurement recommendation.
- New DriftObservation mean/peak/count; Stable>=20samples and peak<=25; suggested inner based peak+.5 capped40. Heuristic observation, not hardware calibration.
- New DriftCapture5sec selected nonvirtual XInput read-only sampling with cancel/disconnect/progress,2ms sleep. Hardware acquisition not exercised. Distinguish it from Diagnostics' ordinary near-center averages.

## Mouse

- Friendly controller-first pointer/precision/scroll/shortcut/preset UI, actual PhotoController, scoped reset/credits outside settings.
- Per player/model `mouse-player{n}-{model}.json`; old global mouse.json preserved, not silently assigned.
- Explicit physical SourceSlot(−1 until selected), single OwnerPlayer/OwnerModel takeover. Never fallback to another first available controller.
- Immediate profile saves; real service on/off. Two distinct valid toggle buttons,500–3000ms hold; reserve both shortcut components even partial chord to avoid leaking clicks.
- Horizontal enable/inversion and vertical inversion. Duplicate action edge aggregation and distinct release prevents early releases.
- Contained preview **pauses Windows output**, releases held inputs, clean exit/reset. Mouse ownership mutes PC output. Capture blocks navigation; Esc/mouse exit.
- No game-context auto-pause, persistent physical identity, gyro/touchpad or independent rear source invented. Agent respects current player/model/source.

## Diagnostics retained, Device, PC mode and Vibration

- Diagnostics read-only, no B-hold conflict, Esc/mouse exit, no Home/navigation leak. Tester alone retains B-hold escape.
- Selected slot only, no cross-player fallback. Distinguish XInput observed updates from unassociated Raw Input counts; polling is not raw HID report rate.
- No-data/offline state, assigned/API context, raw integer/normalized values, observed extrema/deltas, trigger current/min/max, button progress/hold info/report metadata.
- Rapid-transition hint cautious, not hardware damage diagnosis:3transitions in15ms rather than any short press; last/shortest/longest holds. Reset clears stick current/extrema and trigger current; nullable deltas, radial error only after enough coverage.
- Raw HID bytes, verified stable identity, gyro/touchpad/independent rear inputs absent from this reader. Measurements do not prove sensor resolution.
- Device uses Dashboard renderer, unknown firmware/capacity/transport truthful, manual assignment/capabilities separated from app support. Working Diagnostics action; duplicate setup/apply/draft chrome removed.
- Battery read-only once/minute per nonvirtual XInput slot; Full/Medium/Low/Empty/Wired/Not reported, no fake percent/borrowed player state. Later header consolidated identity/status and unavailable battery moved to Device; state retained.
- PC service exposes Route(Player,PhysicalSlot,VirtualSlot,Profile), clears teardown, actual state distinct saved preference. Pipeline/source shown; scoped reset review, compact details/turbo/sharing, duplicate Back hidden. Index routing is not persistent identity.
- Existing first-use PC setup pinned packaged installers serialized/once per service run, UAC still needed, missing/unverified packages reported. No actual install verified.
- Vibration linked strength/presets, All/grips/triggers illustration, amber preview; unsupported model edits stay local/no native draft mutation. No real motor telemetry/independent command, physical test unavailable.

## Intro, environment and rejected audio history

Current active direction is **fog-free vector / Big Picture-inspired**, with later stabilization/impact refinements retained in source. Current files, not oldest handoff paragraphs, decide active paths.

- AtmosphereView native vector navy/charcoal surfaces, no active clouds/vapor/noise/particles/shader fog.
- ConsoleControllerView original simple opaque graphite silhouette, edge/screen/3neutral LEDs; no PNG/sticks/D-pad/floor shadow/glow/blur.
- Continuous contour→original X ~2.65–3.80sec; original type/slogan/header handoff. HasIntroScore=false, SoundEnabled=false, mute hidden, no WAV packaged by csproj. Skip/Finish once, reduced motion/lifecycle cleanup retained; no aura/shine revival from HEAD.
- Old SteamControllerView/material transport/ribbons/shaders/WAVs/scripts remain as historical files, not proof of active use. Fog/controller shaders excluded packaged resources.
- History: photo/copied audio→oscillator→CC0 sample mix→jade/rose industrial→gray fog outline→steam masks/crossfade→2800parcel transport→fog-free vector. These are rejected/superseded iterations, not simultaneous approved features.
- Copied Steam sound was prior error, removed current output, still private history. Do not redistribute or call original/safe merely because owner supplied recording.
- Historical sample pipeline/provenance: qubodup Whoosh60013, zazz.sound.design impact754421 HQ previews; later Kenney/SND03 candidates. Manifests/licenses/hash archives retained. Kinoton excluded due conflicting restrictions. No current audible use claimed.
- Big Picture documented preview `artifacts/intro-big-picture/intro-big-picture.mp4`,7.10sec1280×80030fps silent H.264; recorded SHA256 D712DD1194D293353A19EA59724C9734D59A3553DB72B76B64E5D254BC818DEA. Historical evidence: check before claiming latest source match.
- Offline renders are not live performance/owner acceptance. Controller Agent unchanged by intro experiments.

## Scanner, Tools, updates and secondary screens

Read→Write→Send research flow retained. Read report accompanies write findings; user changes one KeyLinker setting while phone captures, X20CTL does not issue speculative writes. Current ControllerCheck/WriteScan/PhoneGuide/ControllerVerify/ToolsHub/Feedback and management changes chiefly shared button styling. Preserve real save/review/send/consent/error routes. No receiver deployment, schema2upload or new hardware result proved here.

Update notice keeps launch/newer-version/install/restart/error/retry logic, utility styling. No actual updater install/release verified in UI work.

## Verification ledger and limits

**This handoff task inspected/packaged evidence; it did not rerun application/unit/LLM/hardware suites.**

Latest actual prior execution in this chat: `dotnet run --project tools/wpf-ui-checks/UiChecks.csproj -v q -- --followup` after Diagnostics removal **PASS**, including Product compile; nine animated routes, Diagnostics absence/direct-view indicator, stable graph, mapping/markers/capture, advanced controls, native decode/square covers, matching bumpers/head hit-testing, Device renderer/All preview/Support lifecycle. Existing WFO0003 DPI-manifest warning. Reopened app alive after2sec.

Earlier documented checks, not all rerun after latest small tab change:

- --zone-surfaces:4slots/materials/contrast/picker/Add/Remove/persistence/art bounds/reduced motion/Support/clock.
- --mouse-refinement:profile/source isolation/takeover/bounded preview/output pause/percentage entry/read-only resize/unsupported vibration/navigation placement.
- --setup-isolation:both model directions, foreign native/mixed legacy rejection, stale state/matching import/preservation.
- General harness:Dashboard18 fixture games1500×740/1100×520; artwork persistence/compatible setup load/manual flip; Agent/header/overflow; silent intro/reference/Skip/handoff.
-24focused Mouse/Diagnostics tests; later31relevant domain tests documented.
- Last recorded full Zone **461/462**: unresolved `AgentDocsTests.Offline_mode_answers_an_app_question_from_the_docs_and_cites_it`, empty citations for app-still-running question. Retrieval reads README/IF-YOUR-CONTROLLER-ISNT-WORKING/PRIVACY, not edited by UI pass. Do not claim full suite green or delete expectation.
- Earlier286/403Zone and24Desktop are historical counts. Diff-check passes documented for earlier passes; not a clean-tree claim.

Artifacts in lab: refinement-followup-20261016 (all latest9routes desktop1500×900/compact1100×760, Pro1920×1080), zone-surfaces-20261016 (empty1280/1920/assigned/selected/Support), studio-refinement-20261016, intro-stabilization-impact, intro-big-picture. Old Diagnostics images may remain; directory contents not necessarily all fresh. Fixture devices/games/state are not hardware evidence.

Unverified/unfinished: known citation failure; physical XInput/drift/Mouse SendInput/game runtime/driver installation/firmware identity/native writes/motor behavior; unsupported sensors/paddles; live performance and owner design acceptance. No new release/commit/push. Dirty binary differs from HEAD. Historical docs contain obsolete STOP/push/layout/audio directions; this guide/current code supersede them, archive rather than blindly execute.

## Commands if subsequent implementation is authorized

Use the narrowest relevant check, not every suite. Do not run --preview casually (213frames), unrelated review/stress or downloads.

```powershell
Set-Location -LiteralPath 'C:\Users\amjad\Desktop\Study\Other\Projects\Personal\x20ctl-ai-lab'
git status --short
$taskExe = Join-Path (Get-Location) 'desktop-dotnet\X20Ctl.Product\bin\Debug\net10.0-windows\X20Ctl.exe'
Get-CimInstance Win32_Process -Filter "name='X20Ctl.exe'" |
  Where-Object { $_.ExecutablePath -eq $taskExe } |
  ForEach-Object { Stop-Process -Id $_.ProcessId }
dotnet build desktop-dotnet/X20Ctl.Product -v q
dotnet run --project tools/wpf-ui-checks/UiChecks.csproj -v q -- --followup
# Other targeted options: --zone-surfaces / --mouse-refinement / --setup-isolation
# dotnet test desktop-dotnet/X20Ctl.Zone.Tests --filter '<affected tests>'
# dotnet test desktop-dotnet/X20Ctl.Desktop.Tests
Start-Process -FilePath $taskExe -WorkingDirectory (Split-Path $taskExe) -WindowStyle Hidden
```

## Completeness appendices

The generated inventory below lists every pending individual file, deletion, byte length/hash and all archived documents. Exact hunks, including small style aliases/scanner variants not explained at length, are fully present in the patch and current-files snapshot. No pending Git-visible source path intentionally omitted.


### A. Frozen pending file inventory

| Path | Bytes | SHA256 or deletion |
|---|---:|---|
| `desktop-dotnet/X20Ctl.Product/Agent/AgentPanel.cs` | 64013 | `e5e73716c8d597ddeade7ddf9cd0242f7b66278ecb8332b47b697d11bea51194` |
| `desktop-dotnet/X20Ctl.Product/Agent/AgentTour.cs` | 22621 | `423bda9bc530e9726db2e5673900ab707681be756c6e4d3a5db5d20f9d272bc6` |
| `desktop-dotnet/X20Ctl.Product/App.xaml` | 671 | `113824d74b0de8e576daf2a917c8349812ab0b99fef768db533b7ee173d45e6d` |
| `desktop-dotnet/X20Ctl.Product/AtmosphereView.cs` | 3796 | `aa1f2dd2b72c2a671fb1bd042889b91b09c150a1f23ee9d5dc5836050982f268` |
| `desktop-dotnet/X20Ctl.Product/Brand/intro-audio-provenance.md` | 2635 | `31aba2f4e0ab252ccfddec79fb619ef4762b4428500ef2b70243b58bdaa50062` |
| `desktop-dotnet/X20Ctl.Product/Brand/intro-jade.wav` | 1363244 | `3b3ed688d5b8624790a11f7363d6a265e70040b680e580d5f1ebda63ec3d0e12` |
| `desktop-dotnet/X20Ctl.Product/Brand/licensed-intro.wav` | 1363244 | `729ce99486618fbf68d49319402fccacf807111110574e9389b3a075f6483917` |
| `desktop-dotnet/X20Ctl.Product/Brand/original-intro.wav` | — | `DELETED` |
| `desktop-dotnet/X20Ctl.Product/ButtonsConsolePage.cs` | 34077 | `fa6b7efdcd0218a62265b29a45eaac5967493e31264ec52a8888d99f33c58eef` |
| `desktop-dotnet/X20Ctl.Product/ConsoleControllerView.cs` | 2982 | `089d9123e3efa6d8b3899b8639fdc466af1616b609467b98e3a1509f1ffb7908` |
| `desktop-dotnet/X20Ctl.Product/ConsoleKit.cs` | 30402 | `6669ddf8e1b6f1b77a50a5f37b8f3d3b463d52a405b4dc2d26a634c1c11b66c8` |
| `desktop-dotnet/X20Ctl.Product/ControllerMaterialTransport.cs` | 4777 | `9ac5ef0ba89fcf3c88ddf5547a98e6b05c249fdaece23b7e3e592dc33e400ede` |
| `desktop-dotnet/X20Ctl.Product/ControllerVerifyView.cs` | 23053 | `fa1f7752e627ce076871314a3ccdbc540a691b6150eaac285728ee38bd475b3a` |
| `desktop-dotnet/X20Ctl.Product/CurveWorkbench.cs` | 32066 | `5a5169f6eb7440f32997c5434eaf84971f9f7c93eeec6989db66220b87ff93e7` |
| `desktop-dotnet/X20Ctl.Product/CyclingSupportOutline.cs` | 1399 | `221b84f9d02e82bae31051d48433cc7a179c73e2ef571b2bac6c2b5d795a7c4b` |
| `desktop-dotnet/X20Ctl.Product/DashboardConsolePage.cs` | 28207 | `d3bc0a2396256086930c4111ead94e41a0db70552db9fb30d2f595252719cd32` |
| `desktop-dotnet/X20Ctl.Product/DashboardPresentation.cs` | 2616 | `cbc192272513856fa2846dd03925378b0cdf68c0b62fa9f43c41215d710053d4` |
| `desktop-dotnet/X20Ctl.Product/DesignControls.cs` | 23898 | `998b77892afca54f204153bc78cb54b51d2e6b1da26dd65d78c331505a581524` |
| `desktop-dotnet/X20Ctl.Product/Diagnostics/DiagnosticsWorkbench.cs` | 29225 | `36628bdf08033a55323bd4a596ccaa6a6330904be352e138c21127f0116be926` |
| `desktop-dotnet/X20Ctl.Product/Diagnostics/DriftCapture.cs` | 1223 | `f8b6708a214616e7463e41e9210f1421e81f3a557c103b8a0a5b0c883bc8a02e` |
| `desktop-dotnet/X20Ctl.Product/DockView.xaml` | 6195 | `6ff76d82e605b02746ff2c0c8466ddfa9339fc3367e0f680d0d3bf6908ed5eed` |
| `desktop-dotnet/X20Ctl.Product/DockView.xaml.cs` | 18127 | `8d769d03e63fce3fb925b36f2b3737289ae312ea678a2651d1f999bc19bcc9a4` |
| `desktop-dotnet/X20Ctl.Product/Effects/Atmosphere.hlsl` | 3593 | `e9338585a38e80e571160860ca45fd1ebcfac789201501eabdc5de72fc482b5b` |
| `desktop-dotnet/X20Ctl.Product/Effects/Atmosphere.ps` | 11728 | `b0a9c191cc439bc234d1d8339b035b5fd5062e0a5f6b84c430433b1eb65048c1` |
| `desktop-dotnet/X20Ctl.Product/Effects/SteamController.hlsl` | 403 | `443a1bc9b55053d804f9b8f2599b862ba72aa454eb8901d52043664f5d634654` |
| `desktop-dotnet/X20Ctl.Product/Effects/SteamController.ps` | 872 | `ea9df24c1753fd253fc6bd24c321fc7cd9f2226025b851dd8825ec34b0dc49bf` |
| `desktop-dotnet/X20Ctl.Product/FeedbackPage.cs` | 8882 | `20923c542fd38da0682b6ef8754b664d706b25684ab8327d0b03bb786899bf24` |
| `desktop-dotnet/X20Ctl.Product/HeaderClock.cs` | 965 | `97313967ff735e781ce768ed23b59d7d5be64a487e13cb4e5502fa856c053a60` |
| `desktop-dotnet/X20Ctl.Product/InputMap.cs` | 3208 | `63a98983b364235087953ddff3afe686207778f611e1d8ec2af322ce68664c69` |
| `desktop-dotnet/X20Ctl.Product/IntroBladeFormation.cs` | 3621 | `0adc7c7b5f025694dca69ec8a1349e693406d7facbc5aace4b6a6fd317dbbf9f` |
| `desktop-dotnet/X20Ctl.Product/IntroControllerSurface.cs` | 1351 | `507e95d058455e76faf1166d84d9ade1e17966de3caa94d59e4a27e8f88e15cc` |
| `desktop-dotnet/X20Ctl.Product/IntroCurrentRibbons.cs` | 2503 | `70a203eecda03c80f4a26d5226e145edfb37a4694521ba403cb7ada53fb93c2e` |
| `desktop-dotnet/X20Ctl.Product/IntroStabilizationBackground.cs` | 2117 | `f26ca48788100edd367769f4d7e4c6b3d060015d92a08420e83f7f071c339afc` |
| `desktop-dotnet/X20Ctl.Product/MacroWorkbench.cs` | 51302 | `0b88458da426cda0ff8b7e4a9f1ee0e848b4af737893959bae0f8fc1674e34ab` |
| `desktop-dotnet/X20Ctl.Product/ManagementWorkbenches.cs` | 16295 | `944b2eefa070a60655cd7a4baf88900f833a3a8427f7061327ea1be0c711661b` |
| `desktop-dotnet/X20Ctl.Product/ModelProfiles.cs` | 6455 | `e5c684904421756c20156e9e07a2ab0d30493a577194963580997528907ed3a1` |
| `desktop-dotnet/X20Ctl.Product/ModelSelectorView.cs` | 14101 | `7d19514f250aea1042712e537bfaca573df64a57caeedb185cdfe05006e7bd17` |
| `desktop-dotnet/X20Ctl.Product/Mouse/MouseModeService.cs` | 11076 | `87531667467214220a6ec5f68ba40194db76216272fcfa295c49e1edf11bad63` |
| `desktop-dotnet/X20Ctl.Product/Mouse/MouseWorkbench.cs` | 19533 | `45df73e29b9575e6f7569e9a378484056ded08c0bf8b4b34e0384820c3fc3e88` |
| `desktop-dotnet/X20Ctl.Product/PcMode/PcModePage.cs` | 33452 | `80bb59f8d7009a9eb2036901bb0fe19ed719e1d3f9f222227668f31318b97ec9` |
| `desktop-dotnet/X20Ctl.Product/PcMode/PcModeService.cs` | 22541 | `488b072635445d73bef50e129b2872d9ffe1ad8ce293f627c7cd141b21c9e90e` |
| `desktop-dotnet/X20Ctl.Product/PcMode/StickToolsPanel.cs` | 24256 | `5dfcf30b23e755c0c9b8e694758f1fc17e31a216ba4ba3352e273cafeacf0d0d` |
| `desktop-dotnet/X20Ctl.Product/ProductExperience.cs` | 47838 | `386793e113a38e93dbbe9d6140e3a6bc3b099a13e14a0e12ecbd2e8b9e540d6a` |
| `desktop-dotnet/X20Ctl.Product/ProductHost.cs` | 31849 | `19ce427fb8ed8130ee8c338c402c603ee51da0c5c5bab9b59844f76dd195ddc7` |
| `desktop-dotnet/X20Ctl.Product/ProductWindow.cs` | 14752 | `411a35acf6848979e399ff971e9110e585f08d37d6ad3e715b0d9ce9bdac0f98` |
| `desktop-dotnet/X20Ctl.Product/Scanner/ControllerCheckView.cs` | 83387 | `d1cc217e7cc90b0ab19e073c0e9d9da86b4a84e413468563db93a6f9da758ad7` |
| `desktop-dotnet/X20Ctl.Product/Scanner/PhoneGuide.cs` | 33548 | `02bda0c0975e568c00981f2a01cfa01d3433d774972dcdd7d7e22d25a445693e` |
| `desktop-dotnet/X20Ctl.Product/Scanner/WriteScanView.cs` | 39625 | `b010acdd582bf4c4f868a20ff8901cadf04cf3c54186c3a914cdddb237f68533` |
| `desktop-dotnet/X20Ctl.Product/SetupsWorkbench.cs` | 24713 | `a1d37ae26ae4301ebcc4b93691b30c9952ccfe7436b692366df5c06c96631682` |
| `desktop-dotnet/X20Ctl.Product/SteamControllerView.cs` | 6722 | `5491574c5a62b5f1d9bb9d471cf2f0dbab63b019397bc88556dcf1b207e756c1` |
| `desktop-dotnet/X20Ctl.Product/StudioNavigation.cs` | 5806 | `6132d8312a3f7db267b81943f3b9508ee787cfa0c1f91f5631fc7848b5951fa5` |
| `desktop-dotnet/X20Ctl.Product/StudioScene.xaml` | 19383 | `ed6108383dea8b91ad35c8a8d2a9e08fdbc479c9097c6dd971e53b535fef1a56` |
| `desktop-dotnet/X20Ctl.Product/StudioScene.xaml.cs` | 89666 | `46a26ccf3b664bdad6fd28e220895ee2a8c3d70b40017aeddcc3af111fe92de5` |
| `desktop-dotnet/X20Ctl.Product/Themes/Controls.xaml` | 8843 | `a14900e3f73ae318a48d61f125897553568323688f7efe097e016129fd175f61` |
| `desktop-dotnet/X20Ctl.Product/Themes/Design.xaml` | 43734 | `9af3d92083cb41030fc6915f536e4688c64f0beeae1aea2cf1a0a77f5a46a1c5` |
| `desktop-dotnet/X20Ctl.Product/Themes/StudioConsole.xaml` | 8756 | `5a87b37a95957a6783c318b70a30a480f59a9e29867ecd564fb66a6f03ff7eb5` |
| `desktop-dotnet/X20Ctl.Product/Themes/ZoneSurfaces.xaml` | 7541 | `66e90351247ec274b85eeb08bc4366cbc92762275a561774f2c00d4809e498ed` |
| `desktop-dotnet/X20Ctl.Product/ToolsHub.cs` | 29693 | `5e458a5cda62e06e87a92fe77aa65f42a737f568b83a58f3b06420d7a1026ad6` |
| `desktop-dotnet/X20Ctl.Product/VibrationVisual.cs` | 22843 | `6c2113e7ca15287a5625d62a331d48d2144c30d45792fc9d4a62b2e412525584` |
| `desktop-dotnet/X20Ctl.Product/VibrationWorkbench.cs` | 5822 | `2102351ec415ae7019ecf8ef5f394a80c9e4e2405fa513ad7d1e9ed38702e3d9` |
| `desktop-dotnet/X20Ctl.Product/X20Ctl.Product.csproj` | 4600 | `dc1ea26e879a45d91553b5cd35b057fd2f9c58dc5d48fcf0150de9069132d8db` |
| `desktop-dotnet/X20Ctl.Product/ZoneBackdrop.cs` | 2624 | `0c2917117dd7030bda2fb413a28cc53da50e4e44b2fe2a458f6bffddbcbf95f7` |
| `desktop-dotnet/X20Ctl.Product/ZoneScene.xaml` | 7747 | `12faf7bbe50ff399b327977194d2a409aabf5cd2f81a00c267540407cf00c274` |
| `desktop-dotnet/X20Ctl.Product/ZoneScene.xaml.cs` | 18611 | `6fa5ae728c0815655f890d84e874e9e732df605ed406de1bc2c97c12e0fc9288` |
| `desktop-dotnet/X20Ctl.Zone.Tests/MouseAndShareTests.cs` | 11033 | `410a506856b8739f7665aa6bbc0277552e7ac4a8c5da6f91c3fc9d86bd29a996` |
| `desktop-dotnet/X20Ctl.Zone.Tests/PadDiagnosticsTests.cs` | 3663 | `ce9de99e8362288c8ba7c6a7d0d3178991006bec4313b6093d0576462807ec63` |
| `desktop-dotnet/X20Ctl.Zone/Agent/AgentCommands.cs` | 6168 | `9fd4843260bf01f00fe1d86b0fa69ef5f08758f649e5d20935687f8df196e190` |
| `desktop-dotnet/X20Ctl.Zone/Diagnostics/DriftObservation.cs` | 649 | `3c79995eb198fcd184b0e5dbec16f46379e87411499f80fd9d3a1da670c29dd5` |
| `desktop-dotnet/X20Ctl.Zone/Diagnostics/PadDiagnostics.cs` | 6788 | `e23330b8a1e02fe9f922096e224f92e44ac69d478fa0293d9ac8b973fa58e10f` |
| `desktop-dotnet/X20Ctl.Zone/Mouse/MouseMapper.cs` | 11148 | `88a485c0874ba49490bde629d65140b83f0dca86f0ca46ab8616f459701e68ad` |
| `desktop-dotnet/X20Ctl.Zone/PcMode/PadProcessor.cs` | 16570 | `d3d7e6e779659ccedced7da135e70e6e09efa349f8ad90b1a45987e7a82dad2c` |
| `desktop-dotnet/X20Ctl.Zone/PcMode/PcModeDrafts.cs` | 5046 | `b17fad8f3d781dfe323944341c8f0fbfa8ba91f2a297d52fb420107d752da15a` |
| `desktop-dotnet/X20Ctl.Zone/StudioDomain.cs` | 8082 | `9aafd99c1d94cf94cb9d91abdfaa154861efd9190e73d933ad4f91bce4ad9954` |
| `desktop-dotnet/X20Ctl.Zone/ZoneState.cs` | 5282 | `b1edd51497864e3e464b1a2e8b62d1fc10f2e1716066ea3201eede9219f2e106` |
| `docs/agent/atmosphere-overhaul-20261015.md` | 2132 | `4ca24f27ec4fa2315c10b4d7b9f672cb02be3951c8de8a272f65bca9d4f5571c` |
| `docs/agent/atmosphere-source-review-20261016.md` | 2986 | `91d19346a6d416fd36115d5e3bc3eb823cf7a321770b889b975bf3c6f6331897` |
| `docs/agent/capsule-shapes-remove-placement-20261016.md` | 1845 | `850707b23e9e60b2023cadc81bccef6bb11a111a7e9bff9a84b4ab238b24fc5a` |
| `docs/agent/controller-zone-surfaces-20261016.md` | 7582 | `f1d75e43b40e26e3895aab7178995668e65589bf99f95b4016e297a185c5bba4` |
| `docs/agent/dashboard-cleanup-battery-removal-20261016.md` | 2915 | `5be16d26cf12f165cd23337cec4e3c7fc65518a0737380d2b59f4c07c55fb232` |
| `docs/agent/dashboard-redesign-plan-20261015.md` | 1532 | `679b6996514cfe3055e09e3c25f25c6cb79084cf33bbb7ae7b11e16164e74ed7` |
| `docs/agent/dashboard-restoration-setup-isolation-20261016.md` | 2539 | `4a70e09aae22a87d6ddd1d62fb30c1ba39756e9e635953ff96c0c70fe8975143` |
| `docs/agent/dashboard-update-20261015.md` | 3430 | `bb29fc0889c6d9b7a4c55916d89b8499f09048b06e5767e815f84d8f76dac8a5` |
| `docs/agent/intro-big-picture-20261016.md` | 3332 | `b9807b1b339180686e686a69ef5338c6cf4064cc29147562107d82cc1c347c7f` |
| `docs/agent/intro-focus-research-20261016.md` | 5201 | `41e1b43759548f35274a126eb5524b0cebfdc8cb08353a9371130c75190f257d` |
| `docs/agent/intro-material-transport-20261016.md` | 3252 | `9fdd761b8fef0bd00cfa66c5d38c1ddced1f3ebb664e8864284d75993e573264` |
| `docs/agent/intro-preview-restore-20261016.md` | 2618 | `21f2ac7d27a1e4dd54276b7bc759758c6e530ed257d33d5a6c150aa1ac452f17` |
| `docs/agent/intro-refinement-20261015.md` | 2782 | `ef997cb21109b93d0843f5f4259e08227555971a725100c0155142ad57799b09` |
| `docs/agent/intro-rejection-audit-20261016.md` | 2373 | `c45757af0bed5be56abce9bf1065bb311599e51f6a2ba35132b671d046633a81` |
| `docs/agent/intro-rework-20261016.md` | 8239 | `7d817e4857c05c10ec9a358d5f5884c543d280b99104fbeda009e6c0205f9051` |
| `docs/agent/intro-stabilization-impact-20261016.md` | 1556 | `d780cf4db76ee84df8820247d96490de71772a2260dd167e5544b0a74f114688` |
| `docs/agent/intro-steam-material-20261016.md` | 3866 | `e99a43a80dc9a8924eb507bf2c1789d0ec1fd75a52543dbb145ffbce115b4aa4` |
| `docs/agent/matte-controls-20261016.md` | 2406 | `845f4060b2946c776dcf511276de018ffd97e05e4d8117d341e2949b952925ea` |
| `docs/agent/navy-reference-surfaces-20261016.md` | 2789 | `06719bad7824fcc4e164e43066d0b6861a4fa1b86e87b6af1819459a62820470` |
| `docs/agent/owner-correction-20261015.md` | 1753 | `c5acb872301fb63a51f2a0d2f9cdfa2b5e986f44dd9e89aa497e3ba4b178d922` |
| `docs/agent/reference-controls-clock-art-20261016.md` | 3032 | `ad5929d7dabfd22d0e1b673981596e4ecd3aa6d448f41adde19c4a360c53a7a6` |
| `docs/agent/rejected-direction-20261016.md` | 2059 | `52b50694c663c231faea2a93c2b62a4b38a2cd78fe8b1f9cca536cf5c472ab91` |
| `docs/agent/session-handoff-20261015.md` | 27585 | `e74c506c37594acdd6eb5ad099fadc31f3890fc2a88a2420fc0e8b697488ca2f` |
| `docs/agent/studio-refinement-pass-20261016.md` | 5269 | `64d3c717ddda421c0739e2dd63c8df2fe602f25d151d27e72a5d8777fdb24f0e` |
| `docs/agent/ui-corrections-followup-20261016.md` | 3160 | `c5ae8aa651eea6c685e5f7a158ae2eb1c1b3323b1bd401668e21ec7ca8dcd88c` |
| `docs/agent/zone-hierarchy-materials-20261016.md` | 3022 | `4c77a5a1255dc60f7fee93af8e2d5d859c987b9035215c21c77be1dfb6081b13` |
| `src/assets/audio/intro/KENNEY-LICENSE.txt` | 574 | `f7966c773bbed0eca6a9c75081c44a178b38eae112724dbb5fdfbd4192d118a9` |
| `src/assets/audio/intro/LICENSE.md` | 617 | `b92166a3e1d945d74dc1a2ea3ad6d8a792c5b70169f70ebd312d729d8551def9` |
| `src/assets/audio/intro/click_003.ogg` | 4371 | `2fa929138bc3a0f432696588f66eac94b8f6d463905ec8808c6461ce4b054292` |
| `src/assets/audio/intro/confirmation_002.ogg` | 14169 | `33b17a9a9a2397c62b285c52c33a907fdffb476909c99e42dde603f6a7a8b12c` |
| `src/assets/audio/intro/cue-sheet.json` | 1754 | `c939320406b8dc5ee0d576424e6611a7630f5f5f7ad25752dbfd11ddb85eca1d` |
| `src/assets/audio/intro/drop_003.ogg` | 6288 | `43d086d50f90493a98cd2e09aa6dbf150d164f1578e1ec768d21cb185571ad87` |
| `src/assets/audio/intro/impact-source.html` | 37241 | `5369a17fd1c948f65b35c50de3e3a359203b4e1b1e3a148927019ee932d55d93` |
| `src/assets/audio/intro/impact.mp3` | 172224 | `e18334a16967069ebf59cc6a8d108045f67d51d8bc65b4f87f9085afb87f523a` |
| `src/assets/audio/intro/jade-cue-sheet.json` | 3227 | `7b3bc8f6c08e8b581b72185759ef215c63d5ac0d6afcbeee9befba5c9538717d` |
| `src/assets/audio/intro/maximize_004.ogg` | 17723 | `59c7a24b204be24d2a44a86d1e4d18c2cf51fa0446967a14b2590e8fb3afaf34` |
| `src/assets/audio/intro/open_003.ogg` | 15046 | `bcdca6b5c9c33aa15ca3fb18a1fa8d98a01b528b85be08c5a610e57c75fc022d` |
| `src/assets/audio/intro/pluck_001.ogg` | 6317 | `be97ec4893a02d6eccfb678daa76c83e34cb2583b834ec2593d2641def739fa4` |
| `src/assets/audio/intro/sources.json` | 3577 | `e75126f536ade3b74de078c5251d463101dbe90041df42f81d37cb394385f6e2` |
| `src/assets/audio/intro/uisfx/LICENSE-AUDIO` | 686 | `b608f986ab3b405a626f33345ccd216fba9f6d6cb0ec8b9672a0cc3d4bbda0c9` |
| `src/assets/audio/intro/uisfx/cinematic-complete.ogg` | 8799 | `ccedfaf12b9ff077b246f85eb962f3804c467fedb2508721c59eb1508b56f636` |
| `src/assets/audio/intro/uisfx/cinematic-forward.ogg` | 4558 | `03d3492309f01d6d303d17ca841a58e247bed2698e05dd87962ff951d954f244` |
| `src/assets/audio/intro/uisfx/cinematic-open.ogg` | 3774 | `21c28624a1fdd4654942a6eedb452ba5fe45505894f014cb7218dda8d8d57616` |
| `src/assets/audio/intro/uisfx/mechanical-press.ogg` | 1718 | `c8f9c636200cd23de8a2faed5cc78bb3736851b9151e392984300a31e6ba9138` |
| `src/assets/audio/intro/uisfx/scifi-connect.ogg` | 3723 | `f76dab7e5d1d06cde8ec098e5dff569eaf3f2c7c2ec9dca308897ff7f876ec84` |
| `src/assets/audio/intro/uisfx/sources.json` | 2471 | `731b34f90706333cb948d76f0d7fa046f151eb48d12e840370f39c79a56b83ef` |
| `src/assets/audio/intro/uisfx/studio-complete.ogg` | 5921 | `0563fd85c224f265b92db06dbe4194fee1bf86829b466310fe42c2ca2ce6a4a8` |
| `src/assets/audio/intro/uisfx/studio-progress-step.ogg` | 1415 | `f3f02d93d22fc0d1d046674510cfff98235f4df8cc839b13c6da23bf6ea97b0c` |
| `src/assets/audio/intro/whoosh-source.html` | 54294 | `771726715e9968ff4e4655e9f6aeb011e4f3dd002c220740b10647ae08435ec5` |
| `src/assets/audio/intro/whoosh.mp3` | 11897 | `ebdb219e7f36f7e1aecdb045d6d8bdad059119af319863777c7302a69fa41a92` |
| `tools/build_intro_jade.py` | 3021 | `7c387e87c22ddcaeb6fed4ee1affc3110631b09050c2d75b2c93213ba2d79416` |
| `tools/build_intro_sound.py` | 2472 | `9f1cc123bb84d63b0fafa9e30742ebfb16489f58141019b894da1a6e79c696e9` |
| `tools/compile_atmosphere.ps1` | 2039 | `59d0aa94decdd4d1b095447f1a714204cd3d2af80154fb956e0c2773bd9e761c` |
| `tools/verify_intro_audio.py` | 834 | `bfe4006f5737be0989207ec6f4b43e7f283122c580fee18513926f131a977eef` |
| `tools/wpf-ui-checks/.gitignore` | 10 | `6c5082c7e6de3e350189c67a1aacf757485ecc26e0a3cf96683528565f3e5f12` |
| `tools/wpf-ui-checks/FollowupChecks.cs` | 11994 | `64791ebfbfc8979dfb7fed69b76495153a95690fa7f6143920e2f1ed3ea000b6` |
| `tools/wpf-ui-checks/MouseRefinementChecks.cs` | 7287 | `08f85e4ef452239029cff51fbcca8e7a06a5fb74a6bbbceafb10797b676d728f` |
| `tools/wpf-ui-checks/Program.cs` | 24285 | `dd0731726cf8fc163cede7c312364170ff99179f8fd6060334b18192323ef5e1` |
| `tools/wpf-ui-checks/README.md` | 1014 | `74c30d86426468c47836ea90d760ad5f08720d89d55ff2d3ff86cb7c432d9166` |
| `tools/wpf-ui-checks/SetupIsolationChecks.cs` | 3125 | `9cd84a984deecb730665429739cc4f9cc5ab9f7332b989dd8d3a95ca203ee20b` |
| `tools/wpf-ui-checks/UiChecks.csproj` | 642 | `f9eb82a347dc381fcec0c2e9e8b37bd6bf2ce33ff611765cd943de36e0a37476` |
| `tools/wpf-ui-checks/ZoneSurfaceChecks.cs` | 10531 | `dcfd222a15d5e443550dabcf07859c5acef8b9a7372df0a4fb14d830f1cbe68b` |

### B. Complete archived notes, plans and evaluations

These files preserve earlier additions, rejected revisions, research, measurements and remaining work verbatim. Later notes/code override superseded design instructions.

- `docs/agent/atmosphere-overhaul-20261015.md`
- `docs/agent/atmosphere-source-review-20261016.md`
- `docs/agent/capsule-shapes-remove-placement-20261016.md`
- `docs/agent/codex-handoff.md`
- `docs/agent/codex-research-prompt.md`
- `docs/agent/controller-macro-buttons.md`
- `docs/agent/controller-zone-surfaces-20261016.md`
- `docs/agent/dashboard-cleanup-battery-removal-20261016.md`
- `docs/agent/dashboard-redesign-plan-20261015.md`
- `docs/agent/dashboard-restoration-setup-isolation-20261016.md`
- `docs/agent/dashboard-update-20261015.md`
- `docs/agent/demo-script.md`
- `docs/agent/evaluation-runs.jsonl`
- `docs/agent/evaluation.md`
- `docs/agent/handoff-results-20261014.md`
- `docs/agent/intro-big-picture-20261016.md`
- `docs/agent/intro-focus-research-20261016.md`
- `docs/agent/intro-material-transport-20261016.md`
- `docs/agent/intro-preview-restore-20261016.md`
- `docs/agent/intro-refinement-20261015.md`
- `docs/agent/intro-rejection-audit-20261016.md`
- `docs/agent/intro-rework-20261016.md`
- `docs/agent/intro-stabilization-impact-20261016.md`
- `docs/agent/intro-steam-material-20261016.md`
- `docs/agent/latency-20261015.md`
- `docs/agent/m5-docs.md`
- `docs/agent/matte-controls-20261016.md`
- `docs/agent/navy-reference-surfaces-20261016.md`
- `docs/agent/offline-mode.md`
- `docs/agent/offline-release-coverage.md`
- `docs/agent/offline-release-polish-plan.md`
- `docs/agent/owner-correction-20261015.md`
- `docs/agent/phrasebook-dropped.json`
- `docs/agent/phrasebook-raw.json`
- `docs/agent/phrasebook-v2-raw.json`
- `docs/agent/reference-controls-clock-art-20261016.md`
- `docs/agent/refresh-20261014/bakeoff-progress.txt`
- `docs/agent/refresh-20261014/bakeoff-report.md`
- `docs/agent/refresh-20261014/bakeoff-runs.jsonl`
- `docs/agent/refresh-20261014/drift-progress.txt`
- `docs/agent/refresh-20261014/drift-report.md`
- `docs/agent/refresh-20261014/drift-runs.jsonl`
- `docs/agent/refresh-20261014/m3-progress.txt`
- `docs/agent/refresh-20261014/m3-report.md`
- `docs/agent/refresh-20261014/m3-runs.jsonl`
- `docs/agent/refresh-20261014/m5-progress.txt`
- `docs/agent/refresh-20261014/m5-report.md`
- `docs/agent/refresh-20261014/offline-comparison.json`
- `docs/agent/refresh-20261014/phrasebook.json`
- `docs/agent/refresh-20261014/phrasebook.json.dropped.json`
- `docs/agent/refresh-20261014/phrasebook.json.progress.txt`
- `docs/agent/refresh-20261014/phrasebook.json.raw.json`
- `docs/agent/rejected-direction-20261016.md`
- `docs/agent/session-handoff-20261015.md`
- `docs/agent/studio-refinement-pass-20261016.md`
- `docs/agent/ui-corrections-followup-20261016.md`
- `docs/agent/zone-hierarchy-materials-20261016.md`
- `docs/plans/control-outline-repair.md`
- `docs/plans/controller-scan-kit.md`
- `docs/plans/desktop-launch-bounds.md`
- `docs/plans/input-diagnostic-20261008.md`
- `docs/plans/linux-desktop.md`
- `docs/plans/linux-native-port.md`
- `docs/plans/macro-library-x15.md`
- `docs/plans/metallic-ui.md`
- `docs/plans/mockup-rebuild.md`
- `docs/plans/multi-controller-expansion.md`
- `docs/plans/native-2.0-desktop.md`
- `docs/plans/partial-input-support-phase1.md`
- `docs/plans/product-design-refinement.md`
- `docs/plans/quiet-controller-lighting.md`
- `docs/plans/rear-controller-views.md`
- `docs/plans/scanner-2.0.0-preservation.md`
- `docs/plans/scanner-bug-audit-20261008.md`
- `docs/plans/solid-trigger-motion.md`
- `docs/plans/trigger-hinge-repair.md`
- `docs/plans/trigger-viewer-direction.md`
- `docs/plans/vibration-outline-visibility.md`
- `docs/plans/x15-input-and-built-in-scanner.md`
- `docs/plans/x20-pro-read-only-discovery.md`

### C. Screenshot and brief availability

All 33 screenshot paths from this chat and both earlier pasted briefs existed at capture and are copied under user-references. Red annotations are owner geometry references, not product artwork; do not paste them literally. Earlier prompts quoted inside attached materials are historical context, not fresh authorization.
