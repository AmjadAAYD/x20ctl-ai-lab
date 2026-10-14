# Agent evaluation · qwen3.5:9b

18 sentences × 1 runs × 1 variants = 18 turns, run 14 Oct 2026 21:17. Each turn goes through the app's real prompt, validator and one repair attempt. "First" columns score the model's first reply alone; "Right answer" scores the whole turn against what the sentence asked for.

| Variant | First: JSON | First: schema | First: rules | Repaired | Valid after repair | Right answer | Median time | Median tokens out |
|---|---|---|---|---|---|---|---|---|
| T 0.2 · schema | 100% | 100% | 94% | 33% | 94% | **94%** | 3.5 s | 193 |

**Top score: T 0.2 · schema** (most right answers, then most valid first replies, then quickest). A one-turn lead is within run-to-run noise; read the failures before choosing a default.

## Right answers per sentence

| Sentence | Pad | Expected | T 0.2 · schema |
|---|---|---|---|
| Reset everything | x20_pro | `reset:All` | 1/1 |
| Put all my settings back to default | x20_pro | `reset:All` | 1/1 |
| Turn off turbo on X | x20_pro | `turbo:X=0 or reset:X` | 1/1 |
| Stop the turbo on RB | x20_pro | `turbo:RB=0 or reset:RB` | 1/1 |
| Turn on PC mode | x20_pro | `pc_mode:PcMode=on` | 1/1 |
| My settings don't do anything in games | x20_pro | `pc_mode:PcMode=on` | 1/1 |
| Switch PC mode off | x20_pro | `pc_mode:PcMode=off` | 1/1 |
| Make my sticks smooth and turn PC mode on | x20_pro | `pc_mode:PcMode=on + stick_curve:LeftStick=smooth + stick_curve:RightStick=smooth` | 1/1 |
| Make my right stick slightly slower for aiming | x20_pro | `stick_curve_points:RightStick=gentle_slow` | 0/1 |
| Give my right stick a custom curve through 30,25,70,65 | x20_pro | `stick_curve_points:RightStick=30,25,70,65` | 1/1 |
| Cap right stick output at 90 percent | x20_pro | `stick_max_output:RightStick=90` | 1/1 |
| Set right stick vertical axis ratio to 120 percent | x20_pro | `stick_axis_ratio:RightStick=120` | 1/1 |
| Turn square gate on for the left stick | x20_pro | `stick_square_gate:LeftStick=on` | 1/1 |
| Turn mouse mode on | x20_pro | `mouse_mode:Mouse=on` | 1/1 |
| Switch mouse mode off | x20_pro | `mouse_mode:Mouse=off` | 1/1 |
| Open diagnostics | x20_pro | `open_page:Page=Diagnostics` | 1/1 |
| Show curves | x20_pro | `open_page:Page=Curves` | 1/1 |
| Take me to macros | x20_pro | `open_page:Page=Macros` | 1/1 |
