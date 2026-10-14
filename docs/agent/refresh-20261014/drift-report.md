# Agent evaluation · qwen3.5:9b

5 sentences × 1 runs × 1 variants = 5 turns, run 14 Oct 2026 21:16. Each turn goes through the app's real prompt, validator and one repair attempt. "First" columns score the model's first reply alone; "Right answer" scores the whole turn against what the sentence asked for.

| Variant | First: JSON | First: schema | First: rules | Repaired | Valid after repair | Right answer | Median time | Median tokens out |
|---|---|---|---|---|---|---|---|---|
| T 0.2 · schema | 100% | 100% | 100% | 100% | 100% | **100%** | 7.4 s | 342 |

**Top score: T 0.2 · schema** (most right answers, then most valid first replies, then quickest). A one-turn lead is within run-to-run noise; read the failures before choosing a default.

## Right answers per sentence

| Sentence | Pad | Expected | T 0.2 · schema |
|---|---|---|---|
| My right stick drifts a little | x20_pro | `stick_deadzone:RightStick` | 1/1 |
| My left stick moves on its own | x20_pro | `stick_deadzone:LeftStick` | 1/1 |
| Fix my stick drift | x20_pro | `stick_deadzone:LeftStick + stick_deadzone:RightStick` | 1/1 |
| My camera keeps slowly turning by itself | x20_pro | `stick_deadzone:RightStick` | 1/1 |
| Check my sticks for drift | x20_pro | `stick_deadzone:LeftStick + stick_deadzone:RightStick` | 1/1 |
