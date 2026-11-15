# Agent XCTL: teacher guide (IA Générative Textuelle)

Public class project (**Group G17:** Amjad AAYD, Saad ZINE, Salma RAHMANI). Current AI Lab work is on branch `lab/qwen-v0.022` of [`AmjadAAYD/x20ctl-ai-lab`](https://github.com/AmjadAAYD/x20ctl-ai-lab) (`x20ctl-lab-qwen-main`). The standard X20CTL download does not contain the local-model / AI Lab code unless built from this repository.

## What it is

A small assistant inside the real X20CTL Windows app (WPF, .NET 10), drawn as a controller. The user types a sentence (*"my aim feels twitchy"*, *"can u set the rgbs to light blue"*). The app resolves unambiguous commands locally (`AgentDirect`), or queries a local model (**Qwen** `qwen3.5:9b` through **Ollama**) with strict JSON Schema output, or runs in **Offline mode** via an embedded phrasebook (**35** intents, **3,081** cleaned sentences). Every action is checked before applying:

1. parses the JSON strictly (unknown fields rejected);
2. validates every action against `AgentVocabulary` (**22** actions, **9** pages), the controller model (`catalog.json`), and numeric ranges (`ProposalValidator.Check`);
3. applies only what passes (PC mode profile, mouse mode, app page navigation, RGB color/effect preview), and reports what actually changed from app state rather than the model's prose;
4. keeps transactional **Undo** available on each change card and in **History**.

A rejected proposal changes nothing. The model never writes unverified controller firmware or rear paddle memory (`M1`–`M6` hardware writes are declined).

## Build and run

Install the **.NET 10 SDK**, then:

```powershell
dotnet build desktop-dotnet/Product.sln --no-restore
desktop-dotnet\X20Ctl.Product\bin\Debug\net10.0-windows10.0.19041.0\X20Ctl.exe
```

Live mode requires **Ollama** with `qwen3.5:9b` (and `nomic-embed-text` for document lookup). The app never downloads models, uses no API key, and calls no external LLM service. Without Ollama, **Offline mode** (embedded phrasebook + keyword search) and **Canned demo** inside **RB Settings → Lab / Safety** still work.

## Features

- Slow, visible walk-through of each change on the page where it lives (Curves, Buttons).
- A stated meaning for every setting (`X20Ctl.Zone/Agent/AgentKnowledge.cs`), injected into the prompt and cards.
- Clarifying questions asked only when a required detail is missing, cleanly resetting if the user asks a new question or switches request (`AgentClarification.IsNewRequest`).
- Reads before it writes: *"my stick drifts"* measures live stick drift via XInput first, and `ProposalValidator` refuses a dead zone at or below the measured peak.
- RGB lighting & effects (`AgentColors`, `AgentLightingEffects`) plus dynamic avatar colors synced to the requested LED color.
- Help answers cite `README.md`, `IF-YOUR-CONTROLLER-ISNT-WORKING.md`, and `PRIVACY.md` passages (`AgentDocs`) and decline out-of-scope questions (`m5-docs.md`).
- **Saved setups & Undo history:** *"save this as X"*, *"load X"*, *"my setups"*, *"undo"*, *"undo everything"*. Canned demo edits stay isolated in demo state.
- **AI Lab (`Lab / Safety`) & Incident Reporting (`Report issue`):** Open **RB Settings** to switch **Offline mode**, click **Report issue** (opens pre-filled `FeedbackPage` backed by `FeedbackClient` / `FB-...` receipts), or expand **`Lab / Safety`** to inspect/reset the system prompt, tune Qwen temperature, toggle **Canned demo**, or test raw JSON in the *Bad-output playground*.

## Documents

- [Rapport d'ingénierie PFE (FR)](rapport-pfe-chatbot.md)
- [7-Step GenAI Automation TP & Tutorial (EN)](tp-genai-automation.md)
- [Demo script, 3 to 5 minutes](demo-script.md)
- [Evaluation: Qwen bake-off, settings chosen](evaluation.md) (`405` M2 turns baseline; latest single-pass `qwen3.5:9b` checks: `--agent-m3` **18/18**, `--agent-drift` **5/5**, `--agent-qwen` **11/12**)
- [Offline mode](offline-mode.md) (`35` intents, `3,081` cleaned phrases, held-out & messy scores)
- [Document lookup (RAG)](m5-docs.md) (`48` passages, top `4`, latest single pass `20/23`, historical 3-run `63/69`)

## Checks

```powershell
dotnet build desktop-dotnet/Product.sln --no-restore
dotnet test desktop-dotnet/X20Ctl.Zone.Tests/X20Ctl.Zone.Tests.csproj --no-build
```

Latest verified test run: **`1,125 / 1,125` passed** (`0` failed, `0` skipped) after all edits.

## Limits

Qwen can hallucinate: valid JSON structure, allowed targets, and numeric ranges do not prove gameplay advice is optimal. Live mode can modify local PC mode settings; **Canned demo** never writes persistent files. Hardware paddle writes and firmware flashing are unsupported. Live Qwen quality and latency depend on host hardware (GPU/RAM). For a repeatable offline/deterministic demonstration without Ollama, use **Offline mode** or **Canned demo**. Note: if a separate elevated `USBPcapCMD` capture session was started manually during USB diagnostics, stop it directly in that elevated PowerShell window (`Ctrl+C`).
