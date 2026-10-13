# M5: answers from the docs

51 passages from README.md, IF-YOUR-CONTROLLER-ISNT-WORKING.md, PRIVACY.md.

## Retrieval

| Method | Answering passage in the top 3 | Best score, docs answer it (lowest) | Best score, docs don't (highest) |
|---|---|---|---|
| keywords | 17/20 | 0.15 | 0.28 |
| embeddings | 19/20 | 0.64 | 0.74 |

## Answers (qwen3.5:9b, passages found by meaning, cut-off 0.50, 3 runs)

| Question | Right | Cited |
|---|---|---|
| How do I switch my settings off in the middle of a game? | 2/3 | Tools › PC mode |
| What drivers does PC mode install? | 3/3 | Tools › PC mode |
| Can I use four controllers at once? | 3/3 | Tools › PC mode |
| Can turbo get me banned? | 3/3 | Tools › PC mode |
| Can I give one game its own settings? | 3/3 | Tools › PC mode |
| How do I share my settings with a friend? | 3/3 | Tools › PC mode |
| The tester shows no input, what do I do? | 3/3 | The tester shows no input |
| I closed the app but it's still running | 3/3 | I closed the app, but it is still running |
| Nothing shows up when I scan for my controller | 3/3 | Nothing appears in the scan, Tools › Controller Check |
| Where are my profiles saved? | 3/3 | Startup or saved-profile error |
| Does X20CTL send any data about me? | 3/3 | The short version, What is sent, and only when you choose to, Local first |
| Is the X20 Pro supported? | 0/3 |  |
| What does the Controller Check do? | 3/3 | Tools › Controller Check |
| Do I need to install .NET to run it? | 3/3 | Download |
| Is the exe signed? | 2/3 | Download |
| A write failed, what now? | 3/3 | A write failed |
| What's in the Tools page? | 3/3 | Tools |
| Does it work on Linux? | 3/3 | Download |
| Does X20CTL flash firmware? | 3/3 | Local first |
| How do I report a bug? | 2/3 | Reset and reporting |
| How much does the X20 Pro cost? | 3/3 |  |
| Does X20CTL work on a PlayStation 5? | 3/3 |  |
| How many hours does the X20's battery last? | 3/3 |  |

**63/69 right.**

## What this shows

- **Retrieval by meaning beats retrieval by words:** 19/20 against 17/20 for having an answering passage among the three
  offered. The keyword search missed questions worded unlike the docs ("Nothing shows up when I scan" against the
  heading "Nothing appears in the scan").
- **A similarity cut-off can't tell answerable questions from unanswerable ones here.** The best passage for a question
  the docs don't answer scored up to 0.74 by meaning, above the lowest score for one they do answer (0.64). So the cut-off
  (0.5) only drops passages that are plainly off topic, and admitting "I can't confirm that" is left to the model, which is
  told to.
- **Every unanswerable question was refused, 9/9:** price, PS5 support, battery hours. No made-up facts, and no made-up
  citations: the validator refuses any source number that wasn't offered, and none was needed.
- **The 6 misses are all too cautious, none invented:** the model had the right passage and said the docs don't confirm
  it, or (for the X20 Pro) didn't have it. The X20 Pro question failed 3/3 because retrieval missed: the supported-controllers
  passage wasn't among the three offered (the privacy introduction, What's new and Local first were), and the model rightly
  wouldn't answer from those. It is the one retrieval miss by meaning. A likely cause, not yet tested: the table is
  flattened to a run of words, which embeds poorly; writing each table row as a sentence would test it.

Run with `--agent-m5 DIRECTORY`; the per-turn log is progress.txt.
