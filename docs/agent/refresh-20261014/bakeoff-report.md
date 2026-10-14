# Agent evaluation · qwen3.5:9b

27 sentences × 1 runs × 5 variants = 135 turns, run 14 Oct 2026 21:15. Each turn goes through the app's real prompt, validator and one repair attempt. "First" columns score the model's first reply alone; "Right answer" scores the whole turn against what the sentence asked for.

| Variant | First: JSON | First: schema | First: rules | Repaired | Valid after repair | Right answer | Median time | Median tokens out |
|---|---|---|---|---|---|---|---|---|
| T 0.2 · schema | 100% | 100% | 89% | 44% | 96% | **89%** | 4.4 s | 229 |
| T 0.8 · schema | 100% | 100% | 89% | 44% | 96% | **96%** | 4.5 s | 244 |
| T 0.2 · bounded schema | 100% | 100% | 81% | 48% | 96% | **96%** | 3.9 s | 228 |
| T 0.8 · bounded schema | 96% | 96% | 85% | 48% | 96% | **85%** | 4.5 s | 261 |
| T 0.2 · no schema | 100% | 19% | 19% | 81% | 52% | **26%** | 4.8 s | 270 |

**Top score: T 0.8 · schema** (most right answers, then most valid first replies, then quickest). A one-turn lead is within run-to-run noise; read the failures before choosing a default.

## Right answers per sentence

| Sentence | Pad | Expected | T 0.2 · schema | T 0.8 · schema | T 0.2 · bounded schema | T 0.8 · bounded schema | T 0.2 · no schema |
|---|---|---|---|---|---|---|---|
| Make my sticks smooth | x20_pro | `stick_curve:LeftStick=smooth + stick_curve:RightStick=smooth` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Make my right stick more precise for sniping | x20_pro | `stick_curve:RightStick=slow` | 0/1 | 0/1 | 0/1 | 1/1 | 0/1 |
| My aim feels too twitchy | x20_pro | `stick_curve:RightStick=slow or stick_curve:RightStick=smooth` | 0/1 | 1/1 | 1/1 | 0/1 | 0/1 |
| My right stick drifts a little | x20_pro | `stick_deadzone:RightStick` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| My left stick moves on its own | x20_pro | `stick_deadzone:LeftStick` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Set my left stick dead zone to 8 | x20_pro | `stick_deadzone:LeftStick=8` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Right stick full speed at 85 | x20_pro | `stick_full_at:RightStick=85` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Small stick movements don't register in my game | x20_pro | `stick_anti_deadzone:LeftStick or stick_anti_deadzone:RightStick` | 1/1 | 1/1 | 1/1 | 0/1 | 0/1 |
| I want faster triggers for shooting | x20_pro | `trigger_full_at:RightTrigger or trigger_deadzone:RightTrigger or trigger_curve:RightTrigger=quick` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Hair triggers for Call of Duty | x20_pro | `trigger_full_at:RightTrigger or trigger_deadzone:RightTrigger` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Slow, progressive triggers for racing | x20_pro | `trigger_curve:RightTrigger=slow or trigger_curve:LeftTrigger=slow` | 1/1 | 1/1 | 1/1 | 0/1 | 0/1 |
| Remove the dead zone on both triggers | x20_pro | `trigger_deadzone:LeftTrigger=0 + trigger_deadzone:RightTrigger=0` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Turbo on X, 12 presses a second | x20_pro | `turbo:X=12` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Turn off turbo on X | x20_pro | `turbo:X=0 or reset:X` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Swap A and B | x20_pro | `remap:A=B + remap:B=A` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Make LB act like RB | x20_pro | `remap:LB=RB` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Put jump on M1 | x20_pro | `paddle:M1=A` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Put jump on M5 | x20_pro | `paddle:M5=A` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Reset my right trigger | x20_pro | `reset:RightTrigger` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Reset everything | x20_pro | `reset:All` | 1/1 | 1/1 | 1/1 | 1/1 | 0/1 |
| Put jump on M5 | x20 | `decline` | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 |
| Set my left stick dead zone to 260 | x20_pro | `decline` | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 |
| Turbo on RB at 50 a second | x20_pro | `decline` | 0/1 | 1/1 | 1/1 | 0/1 | 1/1 |
| Can you change my RGB to red? | x20_pro | `decline` | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 |
| Make the vibration stronger | x20_pro | `decline` | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 |
| What's good and bad about hair triggers? | x20_pro | `answer, no actions` | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 |
| Why would I use an anti-dead zone? | x20_pro | `answer, no actions` | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 |
