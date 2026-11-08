# Agent XCTL: teacher guide (IA Générative Textuelle)

Private class project, due 15 November 2026. Branch `ai-agent-qwen` of the private repository `AmjadAAYD/x20ctl-ai-lab`. The public X20CTL download does not contain this agent's local-model code. Repository access must be granted to the teacher.

## What it is

A small assistant inside the real X20CTL Windows app (WPF, .NET 10), drawn as a controller. The user types a sentence ("my aim feels twitchy"). A local model, **Qwen** (`qwen3.5:9b`) through **Ollama**, answers as the controller and proposes changes as JSON. The app then:

1. parses the JSON strictly (unknown fields rejected);
2. validates every action against a fixed list, the controller model and its ranges;
3. applies only what passes, to the PC mode profile, and says what it did from its own records, never from the model's wording;
4. keeps **Undo** available for every applied change.

A rejected proposal changes nothing. The model never writes to the controller itself: the X20 Pro has no verified write protocol, so paddle and firmware changes are declined.

## Build and run

Install the .NET 10 SDK, then:

```powershell
dotnet build desktop-dotnet/X20Ctl.Product -c Release
desktop-dotnet/X20Ctl.Product/bin/Release/net10.0-windows/X20Ctl.exe
```

Live mode needs Ollama with `qwen3.5:9b` (and `nomic-embed-text` for document lookup). The app never downloads models, uses no API key and calls no hosted service. Everything stays on the PC. Without Ollama, **Offline** (embedded phrasebook plus the same validator) and the **canned demo** still work.

## Features

- Slow, visible walk-through of each change on the page where it lives (Curves, Buttons).
- A stated meaning for every setting (`X20Ctl.Zone/Agent/AgentKnowledge.cs`), used in the prompt and the cards.
- Questions are asked back only when the agent cannot act without the answer.
- Reads before it writes: "my stick drifts" measures the real drift first, and the validator refuses a dead zone at or below the measured peak.
- Help answers cite README and recovery-guide passages and refuse when the docs do not cover the question (`m5-docs.md`).
- **Saved setups:** "save this as X", "load X", "my setups", "delete the X setup".
- **Undo history:** "undo", "undo everything", and the settings page lists the changes; going back undoes the chosen step and every later one. The history is cleared when canned demo mode toggles so demo settings can never be undone into the real file.
- **Live stick:** a dot on the Current and Proposed curve cards follows the real stick through XInput (not yet checked with a real pad).

## Documents

- [Demo script, 3 to 5 minutes](demo-script.md)
- [Evaluation: Qwen bake-off, settings chosen](evaluation.md) (measured 12 October 2026, before grammar, setups and history were added)
- [Offline mode](offline-mode.md)
- [Document lookup (RAG)](m5-docs.md)

## Checks

```powershell
dotnet build desktop-dotnet/X20Ctl.sln
dotnet test desktop-dotnet/X20Ctl.Zone.Tests --no-restore
```

## Limits

Qwen can hallucinate: valid structure and ranges do not prove the advice is right. Live mode can save PC mode settings. Hardware paddle writes and firmware configuration are unsupported. Live Qwen quality and latency depend on the laptop and are not guaranteed for the presentation; use the canned demo for a repeatable run.
