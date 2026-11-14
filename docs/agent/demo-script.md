# Agent XCTL: 3–5 minute class demo

## 0:00–0:40 — explain the pipeline

Open the Studio's **Agent XCTL** panel. Say: “Live mode sends text to local Qwen (`qwen3.5:9b`), requests schema-constrained JSON, validates actions against controller limits, and applies permitted PC-mode changes. The app reports what happened independently of the model's reply.” Open **RB Settings** on the agent controller, then expand **Lab / Safety**.

Show the single local model choice (`qwen3.5:9b`), temperature slider (`0.2` default), editable system prompt with **Reset system prompt**, and **Report agent issue / Report issue** feedback integration (`FeedbackClient`). Live Qwen output is nondeterministic and is not required for this presentation.

## 0:40–2:10 — repeatable canned preview

Check **Canned demo · deterministic · preview only**. Point to the app's canned status. Say: “These replies are scripted, not generated. Offline is a separate 35-intent phrasebook (`3,081` cleaned phrases). This demo does not call Ollama, retrieve documents, measure hardware, save settings, or touch a physical controller.”

1. Send `demo aim`. Show the fixed right-stick curve points `35,20,75,62`, the preview change, and **Details → CANNED OUTPUT**. Explain the gentler precision response below the diagonal.
2. Send `demo trigger`. Show right trigger full output at 40% in the preview.
3. Use **Undo** on that answer to restore the previous preview. It does not write a saved profile; an Undo from a different mode cannot pollute the real profile.
4. Send `demo bad output`. `ProposalValidator` rejects the 260% dead zone; nothing changes.
5. Send `demo reset`. The preview returns to defaults. Repeating a value already present reports no changes.

## 2:10–3:05 — real validator playground & issue reporting

In **Lab / Safety**, show the prefilled bad JSON and click **Validate output**. Show the real range rejection. Change the action value from `260` to `6`, then validate again: the verdict is valid but nothing is applied. Replace the JSON with `not json` to show structure rejection. Click **Report issue** (either in **Lab / Safety** or in the action buttons row beside **History** and **Setups**) to demonstrate the pre-filled feedback channel.

## 3:05–3:50 — optional Offline & RGB check

Uncheck **Canned demo**, switch **Offline mode On**, return to chat, and show:
- `"can u set the rgbs to light blue"` (fast local color parsing + controller LED avatar update)
- `"change the lights"` → `"what does rainbow do?"` (clarification question cleanly yielding to an explanation without applying an unintended effect)
- Open **History** or click **Undo** to revert cleanly.

## 3:50–4:30 — limits and privacy

Read the **Lab / Safety** privacy summary: agent prompts, replies, and `48`-passage RAG lookup (`DocCount = 4`) stay on this PC. Qwen can hallucinate; validation checks structure, allowed targets, and ranges, not gameplay optimality or hardware compatibility. Physical rear paddle memory writes and firmware flashing remain unsupported. Close **RB Settings** when returning to the chat view.

## Verification boundary

Automated `dotnet test desktop-dotnet/X20Ctl.Zone.Tests` (**1,125 / 1,125 passed**, covering `AgentDemoTests`, `AgentOfflineTests`, `AgentOfflineCustomizationTests`, `AgentCustomizationTests`, `AgentDocsTests`, `AgentGrammarTests`, `AgentCommandsTests`, `OfflineReleaseTests`) verify logic, normalization, and validation rules; WPF UI screenshots were checked via `--review-agent-look` (`8-settings-changes.png`). Live `qwen3.5:9b` quality depends on host hardware (`evaluation.md` + latest single-pass CLI suites `--agent-m3` `18/18`, `--agent-drift` `5/5`, `--agent-qwen` `11/12`, `--agent-m5` `20/23`), so use **Canned demo** or **Offline mode** for repeatable grading demonstrations.
