# Codex handoff: X20CTL + Agent XCTL

Paste everything below the line into Codex as the task. Written 14 Oct 2026 by Claude Code for Amjad, who is away until about 21 Oct.

---

You are continuing work on **X20CTL**, a Windows WPF (.NET 10) controller app, and on **Agent XCTL**, a local-LLM agent inside it that Amjad is submitting as a class project (*IA Générative Textuelle*). **Due 15 Nov 2026; freeze 12 Nov.** Work autonomously; Amjad does not want check-ins. Batch your report at the end.

## Where things are
- Work in the lab worktree `C:\Users\amjad\Desktop\Study\Projects\Personal\x20ctl-ai-lab\`, branch `ai-agent`. Solution folder: `desktop-dotnet\`.
- Push only with `git push -q private HEAD:ai-agent`.
- The main repo `C:\Users\amjad\Desktop\Study\Projects\Personal\x20ctl` (branch `codex/native-2.0-recovery`) has Amjad's uncommitted vibration work and an untracked `App.xaml.cs`. **Do not touch it.**
- Last commit: `baa8476`. Read `docs/agent/` and `docs/plans/` first.

## Hard rules
1. Commits have **no co-author trailer**. Sole authorship; the message ends at its last paragraph.
2. **Never push to the public `origin`.** Never push `v*` tags (they trigger `release.yml`).
3. **No screen captures.** Review harnesses render visuals only. Amjad also said "stop stress testing": do not run `--review-*` harnesses. Offline image checks in Python are fine.
4. **Never download models.** Ollama has `qwen3.5:9b` and `nomic-embed-text`; use only those. Qwen only (Gemma was dropped).
5. **Don't kill Amjad's own X20Ctl from the main repo.** Stop only processes whose path contains `x20ctl-ai-lab`.
6. UI copy is terse: say it and stop, no asides or reasons. Use they/them for people. Don't write "the official app".
7. **Don't claim a check passed unless you ran it.** Quote real PASS/FAIL.
8. The status line in the agent window comes from the app, never from the model.

## Build, test, run
```
cd desktop-dotnet
dotnet build X20Ctl.Product
dotnet test X20Ctl.Zone.Tests      # 286 passing at baa8476
dotnet test X20Ctl.Desktop.Tests   # 24 passing
```
Stop the lab app before building, or the DLLs lock (MSB3027):
```
Get-Process X20Ctl | Where-Object { $_.Path -like '*x20ctl-ai-lab*' } | Stop-Process -Force
Start-Process 'C:\Users\amjad\Desktop\Study\Projects\Personal\x20ctl-ai-lab\desktop-dotnet\X20Ctl.Product\bin\Debug\net10.0-windows\X20Ctl.exe'
```
Gotchas: `Geometry.Parse` returns a frozen geometry, so `Clone()` before setting `Transform`. Source files mix CRLF and LF and some have a BOM; keep each file's own style when patching. Bash heredocs with nested quotes break; write patch scripts to files.

## Architecture in one screen
- `X20Ctl.Zone/Agent/`: no UI, unit-tested. `AgentSession` (prompt, schema, history, retries), `AgentProposal` (vocabulary, `ProposalValidator`), `AgentApplier`, `AgentKnowledge` (meaning of every setting), `AgentCommands` (instant app commands such as battery, diagnostics), `AgentOffline` (phrasebook, no model).
- `X20Ctl.Product/Agent/`: `AgentPanel` (controller-shaped window: eyes, LEDs, ideas, chat), `AgentTour` (walk-through graphs), `AgentInvite` (help bubble), `OllamaClient`.
- Curves: `PadProcessor.Response` is what games get. `PadProcessor.Through` is the monotone line through the two curve points. The Curves page (`CurveWorkbench`) and the walk-through both draw it, so the graph, the agent's words and games agree. Do not reintroduce a second curve formula.
- Flow: sentence → system prompt → Ollama (`format` = JSON schema, `think:false`) → strict parse → validator → applier → `PcModeProfile.Validate()` → saved to PC mode. Two app-side retries in `AgentSession.Ask`: a reply that promises a change but makes none, and a curve preset the owner never named (must become custom points). A retry is kept only if it changes something.
- The agent acts without asking unless it truly cannot (which button, what a macro does). It asks one `question` back. It remembers 60 messages, including `[applied: …]` notes.

## What to do, in order
**A. Protect the demo (do first)**
1. Add unit tests in `X20Ctl.Zone.Tests` for: `CurvePoints.TryParse`, `PadProcessor.Through` (monotone, endpoints, shape of each preset), `stick_curve_points`, `stick_max_output`, `stick_axis_ratio`, `stick_square_gate`, `mouse_mode`, `open_page`, `AgentCommands` Diagnostics routing, `AgentSession.Promises`, and both retries (use a fake `IAgentModel`).
2. Teach `AgentOffline` (the phrasebook) the new actions: custom curve wording ("slower for aiming", "more precise", "twitchy"), mouse mode on/off, open diagnostics/curves/macros. Keep the held-out sets honest: `OfflineFinal` was scored once, so do not tune to it.
3. Re-run the evaluation harnesses (`--agent-bakeoff`, `--agent-drift`, `--agent-m3`, `--agent-m5`, `--agent-phrasebook`). The prompt, schema and action list changed, so the old numbers are stale. Write the new table to `docs/agent/`. These are agent harnesses, not review captures, and need Ollama running.
4. Qwen copies the example curve points from the prompt verbatim (e.g. `35,20,75,62`). Reduce that: vary or remove the examples, describe shapes in words, add a check, and measure with the bakeoff.

**B. Class deliverables**
5. Demo mode: canned replies, clearly labelled as such, so the demo survives Ollama failing.
6. README section for the teacher, and a 3–5 minute demo script in `docs/agent/`.
7. Lab and Safety: confirm it exists (model picker, temperature, editable system prompt with Reset, "feed a bad output to the real validator", short safety section on hallucination, validation, local-only privacy and limits). Build what is missing.

**C. Queued UI requests (build, no hardware needed)**
8. Dashboard background like the Buttons page.
9. Automatic, silent ViGEmBus install for PC mode.
10. Update popup at launch: check every launch, show it after the intro only when a newer version exists. Today it is opt-in and writes to the feedback line.
11. Your-games strip: wider scan, "Add a game / folder", Play button zoom to the screen ratio, "In game · <name>" then back, real art (Rocket League), and buttons cut off above the taskbar.

**D. Verify without looking**
12. You cannot check visuals by screen. Where a change is visual, write down in your report exactly what Amjad should look at, and do not claim it looks right.

## Do NOT do (needs hardware or Amjad)
- Anything that writes to a real pad, or the X20 Pro reverse-engineering track (the pad arrives 26 Oct – 2 Nov). The "eyes spin" easter egg waits for the real pad.
- Paddle (M1–M6) changes: PC mode cannot target them.
- Linux/Avalonia port, X05/X05 Pro/Dune read-write, release tagging, anything on the public repo.
- Deciding what to do with Amjad's uncommitted work in the main repo.

## Report format
When done: commits (hashes), test counts with real output, the new evaluation table, what you built, what you skipped and why, and a short "please look at" list for Amjad.
