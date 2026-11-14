# Offline mode: Agent XCTL without a language model

Offline mode is the agent with no model behind it: an **Offline** switch in the agent's Settings view (**RB Settings**). It also takes over automatically when Ollama cannot run on the PC. Its replies use the same JSON schema Qwen writes, so `ProposalValidator`, live drift measurement, page walk-throughs, Saved Setups, History, and **Undo** are shared cleanly.

## How it understands a sentence

1. **Direct fast-paths (`AgentDirect.Resolve`) & slot extraction (`OfflineAgent.Slots`).** Normalized via `AgentGrammar.Plain` and `AgentSpelling`. Sticks, triggers, buttons (in spoken order), rear paddles (`M1`–`M6`), numbers, curve presets, colors (`AgentColors`), and lighting effects (`AgentLightingEffects`) are parsed first. All direct proposals pass through `ProposalValidator.Check`.
2. **Exclusion rules & question guards.** Contradicting slots (e.g., naming a stick on a trigger intent) are ruled out; standalone informational questions (*"what does rainbow do?"*) are intercepted by `OfflineAgent.IsQuestion` across `AgentClarification`, `AgentColors`, `AgentLightingEffects`, and `OfflineAgent.Chat` so they never execute hardware changes or glue onto prior clarification prompts.
3. **TF-IDF example voting.** Remaining requests are vectorized as weighted unigrams/bigrams with slot tokens replaced (`STICK`, `TRIGGER`, `BTN`, `PADDLE`, `NUM`). The `7` nearest neighbors vote (hand-written seeds carry weight `2.0`). Below `0.3` similarity (`OfflineAgent.Threshold`), the agent declines safely and offers examples.
4. **Action building & explanation.** The winning intent builds actions from extracted slots and explains why from `AgentKnowledge`. Unmatched questions fall back to `KeywordSearch` over the help docs (`M5`).

## The phrasebook: Qwen as the teacher

### Historical baseline (26 intents: v1 + v2)

Originally built across `26` intents (`142` hand-written examples). Qwen wrote paraphrases ahead of time (`--agent-phrasebook`):

| Version | Prompt | Raw Sentences | Kept after `PhrasebookCleaner` |
|---|---|---|---|
| v1 | Meaning + examples; "write 30 more" ×2 | 1,487 | 1,060 (267 rules, 160 vote) |
| v2 | v1 + neighbor exclusions + required slots + no questions; 25 ×3 | 1,620 | 1,412 (108 rules, 100 vote) |

Merged cleaned baseline (`v1 + v2`): **2,459** sentences (`52/60` on development held-out; `16/20` final test vs. `17/20` examples-only).

### Current expansion & cleanup (35 intents — AI Lab update)

Offline coverage was expanded from **26 → 35 intents** (**189** hand-written seeds) to match `AgentVocabulary`:
- Added 9 intents: `stick_max_output_set`, `stick_axis_ratio_set`, `stick_square_gate_on`, `stick_square_gate_off`, `mouse_mode_on`, `mouse_mode_off`, `open_page`, `led_color_set`, `led_effect_set`.
- Strengthened `PhrasebookCleaner` rules to filter `greeting` control-word noise (`sticks`, `triggers`, `buttons`, `paddles`, `d-pad`, `reset`, `game modes`, `sign in`), exclude non-LED `"rainbow six/6/r6"` false positives, reject buttons inside `trigger_*`, and require valid color/effect extraction on `led_*` intents.
- Ran incremental generation (`--agent-phrasebook desktop-dotnet\X20Ctl.Zone\Agent\phrasebook.json --missing-only`) using local **`qwen3.5:9b`** only for the 9 missing intents while explicitly excluding all held-out benchmarks (`OfflineHeldOut`, `OfflineFinal`, `OfflineMessy`, `AgentBench.Cases`), followed by full two-pass `PhrasebookCleaner.Clean`:
  - **3,081 kept** sentences across **35 intents** in `desktop-dotnet/X20Ctl.Zone/Agent/phrasebook.json`
  - **551 dropped** noisy/cross-intent sentences logged in [phrasebook.dropped.json](phrasebook.dropped.json)

## Current verified evaluation results (`X20Ctl.Zone.Tests`)

Executed after the final code edits (`1,125 / 1,125` tests passing):

| Benchmark | Result | Notes |
|---|---|---|
| `bench` (`Offline_mode_answers_the_evaluation_sentences`) | **40/40 (100%)** | Canonical evaluation sentences (up from historical `39/40`) |
| `examples only` (`Phrasebooks_compared_on_held_out_wording`) | **59/60** | Hand-written seeds alone with upgraded normalization/rules |
| `both cleaned` (`Phrasebooks_compared_on_held_out_wording`) | **59/60** | Expanded & cleaned 35-intent phrasebook (**+7** vs. historical `52/60`) |
| `held out` (`Offline_mode_handles_wording_it_has_never_seen`) | **57/60 (95.0%)** | Unseen wording (3 borderline misses on game/curve phrasing after adding 9 new intents) |
| `messy` (`Offline_mode_reads_messy_typing`) | **130/131 (99.2%)** | Typos, SMS abbreviations, broken grammar, French/Spanish/German phrases |

## What this shows

- **Cleaner data + normalization beats raw volume:** Cleaning `greeting` leakage (`22` noisy entries dropped) and upgrading `AgentGrammar` / `OfflineSupport` raised the cleaned phrasebook score from `52/60` to `59/60` even after growing from `26` to `35` intents.
- **Classifier vs. meaning:** TF-IDF remains bounded by surface tokens on rare borderline complaints (`57/60` held-out), whereas live `qwen3.5:9b` understands novel phrasing directly (**96 %** on M2).
- **Safety invariant still holds:** `ProposalValidator` prevents impossible values (`"set the right stick full speed to 40"` cleanly returns 0 actions), and transactional **Undo** backs every valid change.
