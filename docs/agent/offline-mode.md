# Offline mode: Agent XCTL without a language model

Offline mode is the agent with no model behind it: an **Offline** switch in the agent's title bar. It also takes over
when Ollama can't run on the PC. Its replies are the same JSON Qwen writes, so the validator, the drift reading, the
walk-through and Undo are shared, and so is "Say HELP".

## How it understands a sentence

1. **Slots are lifted out.** Sticks, triggers, buttons (in the order said), paddles, numbers and curve words are read
   with patterns. "jump" is A, with the guess stated.
2. **The words rule some requests out.** A sentence that names a stick and no trigger can't be a trigger request, and
   the other way round. Switching something off needs a word that says off.
3. **The rest is matched by example.** The sentence becomes TF-IDF weighted words and word pairs, with the slots
   replaced by placeholders (STICK, TRIGGER, BTN, PADDLE, NUM). The 7 closest examples vote, weighted by similarity,
   with the hand-written ones counting double. Below 0.3 similarity it says it didn't catch that.
4. **The winning request builds the reply** from the slots. The recap comes from each setting's meaning
   (`AgentKnowledge`), the same text Qwen is taught from.

Questions ("what's good and bad about…") are answered from the meanings. App questions it can't match fall back to
the closest passage of the docs, found by keywords (M5).

## The phrasebook: Qwen as the teacher

The 26 requests have 142 hand-written examples between them (4 to 9 each). Qwen wrote more ways to say each one, once and ahead of time
(`--agent-phrasebook`). Offline mode never calls the model.

| Version | Prompt | Sentences |
|---|---|---|
| v1 | the request's meaning and its examples; "write 30 more" ×2 | 1487 |
| v2 | v1, plus the similar requests named as off limits, what every sentence must contain (a paddle, two buttons, a number), and no questions; 25 ×3 | 1620 |

Both are kept as Qwen wrote them (`phrasebook-raw.json`, `phrasebook-v2-raw.json`).

### Cleaning

Some of Qwen's sentences belong to another request. For example, "Reset the right stick to the quick profile" was
written for *curves*. `PhrasebookCleaner` works in two passes, with nothing picked by hand:

1. **Rules.** A sentence must not name what contradicts its request, must name what its request needs, and must not
   be a question.
2. **Leave-one-out vote.** Each sentence is matched against all the others. If its neighbours vote for another
   request at least twice as strongly as for its own, it goes.

| | Kept | Dropped by rules | Dropped by the vote |
|---|---|---|---|
| v1 | 1060 | 267 | 160 |
| v2 | 1412 | 108 | 100 |

Every dropped sentence, with the reason, is in `phrasebook-dropped.json`. The agent embeds both cleaned versions
merged: 2459 sentences.

## Results

Three sets of sentences, all for the X20 Pro.

- **Evaluation sentences** are the same 40 as Qwen's. Most are also hand-written examples, so these flatter it.
- **Development (60):** none are examples or phrasebook sentences. 20 were written before the phrasebook and 40
  before it was cleaned. Once their results were looked at, they guided the changes below, so they are no longer an
  independent test.
- **Final test (20):** written after every choice was made, and scored once.

### Development, step by step

| | Examples only | v1 raw | v1 cleaned | v2 raw | v2 cleaned |
|---|---|---|---|---|---|
| nearest example decides | 16/20 * | 15/20 * | | | |
| 7 closest vote | 52/60 | 48/60 | 50/60 | 50/60 | 45/60 |
| + the words rule requests out, every stick/trigger one placeholder | 52/60 | 51/60 | 52/60 | 51/60 | 51/60 |

\* on the first 20 sentences only.

Both cleaned versions merged: **52/60**, the same as the hand-written examples alone. With a tie, the merged
phrasebook was chosen for its wider coverage, before the final test.

### Final test

| | Score |
|---|---|
| Hand-written examples only | 17/20 |
| Examples + cleaned phrasebook (shipped) | **16/20** |

A bug showed up on this test: "help me hit headshots from far away" was taken as a greeting, because the greeting
pattern only looked at the first word. It was fixed after the test was first scored. Before the fix the scores were
17/20 and 15/20.

The evaluation sentences score 39/40 with the shipped phrasebook. "Put all my settings back to default" now asks
"Reset what?", because phrasebook sentences about resetting one control pull it the wrong way.

## What this shows

- **More data didn't help a word matcher.** Doubling or tripling the examples with Qwen's paraphrases left the score
  where the 142 hand-written examples already had it. Cleaning removed the harm the noisy sentences did (48 → 52), but
  added nothing beyond the examples.
- **The real gains came from the matcher:** voting instead of a single nearest example, and letting the sentence's own
  words rule requests out.
- **What would make the paraphrases count is matching by meaning,** for example a small embedding model over the
  phrasebook. That needs a model at run time, which is what offline mode avoids. Qwen, which understands the wording
  itself, scores 96% on the evaluation sentences that offline mode mostly has memorised.
- **Offline mode can still be wrong in ways that change settings.** "make LB send X" became turbo on LB and X: a valid
  change, so it applied. The validator stops impossible settings, not wrong guesses. Undo is the safety net, and the
  status line always says what was applied.
