# Agent XCTL: 3–5 minute class demo

## 0:00–0:40 — explain the pipeline

Open the Studio's Agent XCTL panel. Say: “Live mode sends text to local Qwen, requests JSON, validates actions, and applies permitted PC-mode changes. The app reports what happened independently of the reply.” Open **RB Settings** on the agent controller, then expand **Lab / Safety**.

Show the single model choice, `qwen3.5:9b`, temperature, editable system prompt and **Reset system prompt**. Qwen is the only chat model offered; no model download is needed for this script. Live Qwen output is nondeterministic and is not required for the presentation.

## 0:40–2:10 — repeatable canned preview

Check **Canned demo · deterministic · preview only**. Point to the app's canned status. Say: “These replies are scripted, not generated. Offline is a separate phrasebook. This demo does not call Ollama or retrieve documents, measure hardware, save settings or control a device.”

1. Send `demo aim`. Show the fixed right-stick curve points `35,20,75,62`, the preview change and **Details → CANNED OUTPUT**. Explain the gentler response below the diagonal.
2. Send `demo trigger`. Show right trigger full output at 40% in the preview.
3. Use **Undo** on that answer to restore the previous preview. It does not write a saved profile. An Undo from a different mode is ignored.
4. Send `demo bad output`. The real validator rejects the 260% dead zone; nothing changes. No retry silently replaces the bad canned output.
5. Send `demo reset`. The preview returns to defaults. Repeating a value already present legitimately reports no changes.

Unscripted sentences return a fixed help reply. A send does not advance a hidden sequence; each named phrase always produces the same JSON. Demo preview survives switching away and back for this panel; `demo reset` resets it.

## 2:10–3:20 — real validator playground

In **Lab / Safety**, show the prefilled bad JSON and click **Validate output**. Show the actual range rejection. Change the action value from `260` to `6`, then validate again: the verdict is valid but nothing is applied. Replace the JSON with `not json` and validate to show a structure rejection. This playground never saves or applies proposals.

## 3:20–4:10 — limits and privacy

Read the short Safety text. Agent chat and lookup stay local. Qwen can hallucinate; validation proves structure, allowed targets and ranges, not correctness of advice or hardware compatibility. Real paddle writes are unsupported. Live mode can save PC-mode settings, so the canned preview is useful when no device changes are intended.

Uncheck canned demo to return to the previous live/phrasebook choice. Live readiness may still be unavailable; no live result is promised. Close **RB Settings** to recover conversation space.

## Verification boundary

Compilation and `AgentDemoTests` cover the code and deterministic validator/applier examples. They do not establish visual layout, hardware behavior, live Qwen quality or latency. No screenshots, review harnesses or stress tests are needed for this script.
