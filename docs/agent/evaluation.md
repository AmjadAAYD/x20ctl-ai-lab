# Agent XCTL · M2 evaluation

How the agent was measured, what was found, and which settings the app uses. Produced by the app itself:

```
X20Ctl.exe --agent-bakeoff <folder>
```

It sends each sentence through the real session (prompt, `qwen3.5:9b`, validator, one repair attempt) in a fresh conversation with default settings, and scores it against the actions the sentence asked for (`X20Ctl.Zone/Agent/AgentBench.cs`). Nothing is judged by another model.

## Final run (12 Oct 2026, current prompt with setting meanings)
27 sentences × 3 runs × 5 variants = 405 turns, run 12 Oct 2026 20:39. Each turn goes through the app's real prompt, validator and one repair attempt. "First" columns score the model's first reply alone; "Right answer" scores the whole turn against what the sentence asked for.

| Variant | First: JSON | First: schema | First: rules | Repaired | Valid after repair | Right answer | Median time | Median tokens out |
|---|---|---|---|---|---|---|---|---|
| T 0.2 · schema | 100% | 100% | 89% | 11% | 100% | **96%** | 3.0 s | 166 |
| T 0.8 · schema | 100% | 100% | 90% | 10% | 100% | **96%** | 3.3 s | 172 |
| T 0.2 · bounded schema | 100% | 100% | 86% | 14% | 100% | **98%** | 3.2 s | 171 |
| T 0.8 · bounded schema | 99% | 99% | 86% | 14% | 100% | **95%** | 3.1 s | 174 |
| T 0.2 · no schema | 100% | 19% | 19% | 81% | 88% | **26%** | 4.1 s | 246 |

**Top score: T 0.2 · bounded schema** (79 of 81), one turn ahead of the plain schema. See *Decision* below for why the default is the plain schema.

## Right answers per sentence

| Sentence | Pad | Expected | T 0.2 · schema | T 0.8 · schema | T 0.2 · bounded schema | T 0.8 · bounded schema | T 0.2 · no schema |
|---|---|---|---|---|---|---|---|
| Make my sticks smooth | x20_pro | `stick_curve:LeftStick=smooth + stick_curve:RightStick=smooth` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Make my right stick more precise for sniping | x20_pro | `stick_curve:RightStick=slow` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| My aim feels too twitchy | x20_pro | `stick_curve:RightStick=slow or stick_curve:RightStick=smooth` | 3/3 | 3/3 | 3/3 | 2/3 | 0/3 |
| My right stick drifts a little | x20_pro | `stick_deadzone:RightStick` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| My left stick moves on its own | x20_pro | `stick_deadzone:LeftStick` | 2/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Set my left stick dead zone to 8 | x20_pro | `stick_deadzone:LeftStick=8` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Right stick full speed at 85 | x20_pro | `stick_full_at:RightStick=85` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Small stick movements don't register in my game | x20_pro | `stick_anti_deadzone:LeftStick or stick_anti_deadzone:RightStick` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| I want faster triggers for shooting | x20_pro | `trigger_full_at:RightTrigger or trigger_deadzone:RightTrigger or trigger_curve:RightTrigger=quick` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Hair triggers for Call of Duty | x20_pro | `trigger_full_at:RightTrigger or trigger_deadzone:RightTrigger` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Slow, progressive triggers for racing | x20_pro | `trigger_curve:RightTrigger=slow or trigger_curve:LeftTrigger=slow` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Remove the dead zone on both triggers | x20_pro | `trigger_deadzone:LeftTrigger=0 + trigger_deadzone:RightTrigger=0` | 3/3 | 3/3 | 3/3 | 2/3 | 0/3 |
| Turbo on X, 12 presses a second | x20_pro | `turbo:X=12` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Turn off turbo on X | x20_pro | `turbo:X=0 or reset:X` | 3/3 | 2/3 | 3/3 | 2/3 | 0/3 |
| Swap A and B | x20_pro | `remap:A=B + remap:B=A` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Make LB act like RB | x20_pro | `remap:LB=RB` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Put jump on M1 | x20_pro | `paddle:M1=A` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Put jump on M5 | x20_pro | `paddle:M5=A` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Reset my right trigger | x20_pro | `reset:RightTrigger` | 3/3 | 3/3 | 3/3 | 3/3 | 0/3 |
| Reset everything | x20_pro | `reset:All` | 1/3 | 2/3 | 1/3 | 2/3 | 0/3 |
| Put jump on M5 | x20 | `decline` | 3/3 | 3/3 | 3/3 | 3/3 | 3/3 |
| Set my left stick dead zone to 260 | x20_pro | `decline` | 3/3 | 3/3 | 3/3 | 3/3 | 3/3 |
| Turbo on RB at 50 a second | x20_pro | `decline` | 3/3 | 2/3 | 3/3 | 3/3 | 3/3 |
| Can you change my RGB to red? | x20_pro | `decline` | 3/3 | 3/3 | 3/3 | 3/3 | 3/3 |
| Make the vibration stronger | x20_pro | `decline` | 3/3 | 3/3 | 3/3 | 3/3 | 3/3 |
| What's good and bad about hair triggers? | x20_pro | `answer, no actions` | 3/3 | 3/3 | 3/3 | 3/3 | 3/3 |
| Why would I use an anti-dead zone? | x20_pro | `answer, no actions` | 3/3 | 3/3 | 3/3 | 3/3 | 3/3 |

## Baseline (12 Oct 2026, earlier prompt, before the setting meanings)

25 sentences × 3 runs × 4 variants = 300 turns.

| Variant | First: rules | Right answer | Median time |
|---|---|---|---|
| T 0.2 · schema | 73% | 89% | 2.2 s |
| T 0.8 · schema | 81% | 91% | 2.2 s |
| T 0.2 · no schema | 12% | 20% | 2.7 s |
| T 0.8 · no schema | 12% | 20% | 2.6 s |

The setting meanings (`AgentKnowledge`) raised right answers from 89–91% to 95–98%, at about 1 s more per turn (the prompt grew from about 700–850 to about 1,700–1,900 tokens).

## Findings

1. **The schema is the difference between working and not.** Without Ollama's `format`, Qwen still writes JSON, but leaves out each action's `reason`, so the structure check rejects 81% of first replies, and only 26% of turns end right. With the schema, 100% of replies have the right structure.
2. **Grammar-constrained whole numbers can run away.** With `"type": ["integer","string"]`, some replies that meant 5 came back as `5000000000000000`: the sampler is not allowed the character the model wanted next, so it keeps adding zeros. Seen in 6 of 162 first replies, all on drift and small-movement sentences (stick dead zone and anti-dead zone). The validator rejects them and the repair attempt usually fixes them.
3. **Bounding the number (0–100) hides the runaway instead of curing it.** With the bounded schema there were no giant numbers, but the same replies came back as `50`, and one said "Sets the centre area to 5%" next to `"value": 50`. A dead zone of 50 is out of range and still rejected, but on a setting where 50 is allowed (trigger full at), a meant 5 would pass silently as 50.
4. **Temperature barely matters here.** 0.2 and 0.8 are within one or two turns of each other on every measure.
5. **The meanings fixed a real error.** The curve named "fine" is slightly quick (half a push gives 56%), not precise. The old prompt told the model "sniping → slow or fine". Computed from the function PC mode runs, "slow" (half a push gives 39%) is the sniper curve, and with the schema, sniping now goes to slow in 12 of 12 turns.
6. **What still goes wrong:** "Reset everything" sometimes resets controls one by one instead of `reset All`. "Turn off turbo on X" occasionally does something else. One turbo at 50 a second wasn't refused cleanly at 0.8.

## Decision

- **Temperature 0.2, plain schema** (`AgentSession.Schema`). The bounded schema's one-turn lead is within run-to-run noise, and its failure is silent where the plain schema's is loud. With the plain schema, a runaway number can never pass the validator.
- The no-schema path stays only as the evaluation's control.