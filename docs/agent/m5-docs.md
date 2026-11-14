# M5: answers from the docs

Latest configuration (`AgentDocs.Words = 300, Overlap = 50`, `AgentSession.DocCount = 4`, Markdown tables converted to header-paired rows): **48** passages from README.md, IF-YOUR-CONTROLLER-ISNT-WORKING.md, PRIVACY.md (historical M5 3-run baseline used **51** passages, top **3**, scoring **17/20** keywords, **19/20** embeddings, **63/69** answers).

## Retrieval (latest top 4, single pass)

| Method | Answering passage in the top 4 | Best score, docs answer it (lowest) | Best score, docs don't (highest) |
|---|---|---|---|
| keywords | 19/20 | 0.17 | 0.35 |
| embeddings | 19/20 | 0.62 | 0.73 |

## Answers (qwen3.5:9b, passages found by meaning, cut-off 0.50, latest 1 run vs. historical 3-run baseline)

| Question | Latest (1 run) | Baseline (3 runs) | Latest cited |
|---|---|---|---|
| How do I switch my settings off in the middle of a game? | 1/1 | 2/3 | Studio tour › Studio › PC mode |
| What drivers does PC mode install? | 1/1 | 3/3 | Studio tour › Studio › PC mode |
| Can I use four controllers at once? | 1/1 | 3/3 | Studio tour › Studio › PC mode |
| Can turbo get me banned? | 1/1 | 3/3 | Studio tour › Studio › PC mode |
| Can I give one game its own settings? | 1/1 | 3/3 | Studio tour › Studio › PC mode |
| How do I share my settings with a friend? | 1/1 | 3/3 | Studio tour › Studio › PC mode |
| The tester shows no input, what do I do? | 1/1 | 3/3 | The tester shows no input |
| I closed the app but it's still running | 1/1 | 3/3 | I closed the app, but it is still running |
| Nothing shows up when I scan for my controller | 1/1 | 3/3 | Nothing appears in the scan |
| Where are my profiles saved? | 1/1 | 3/3 | Startup or saved-profile error |
| Does X20CTL send any data about me? | 1/1 | 3/3 | The short version, Local first, What is sent, and only when you choose to |
| Is the X20 Pro supported? | 0/1 | 0/3 | Credits |
| What does the Controller Check do? | 0/1 | 3/3 | Studio tour › Studio › Diagnostics |
| Do I need to install .NET to run it? | 1/1 | 3/3 | Download |
| Is the exe signed? | 0/1 | 2/3 | License |
| A write failed, what now? | 1/1 | 3/3 | A write failed |
| What's in the Tools page? | 1/1 | 3/3 | Studio tour › Tools |
| Does it work on Linux? | 1/1 | 3/3 | Download |
| Does X20CTL flash firmware? | 1/1 | 3/3 | Local first |
| How do I report a bug? | 1/1 | 2/3 | Reset and reporting |
| How much does the X20 Pro cost? | 1/1 | 3/3 |  |
| Does X20CTL work on a PlayStation 5? | 1/1 | 3/3 |  |
| How many hours does the X20's battery last? | 1/1 | 3/3 |  |

**Latest single pass: 20/23 right** (`PC mode` 6/6, `Linux` 1/1, out-of-scope 3/3; baseline 3-run total: **63/69 right**).

## What this shows

- **Retrieval works well across most queries, with one persistent embedding gap:** Both `keywords` and `embeddings` (`nomic-embed-text`) reached **19/20** in the top `4` (up from `17/20` lexical in the baseline top-3 run). Writing Markdown table rows as header-paired sentences (`AgentDocs.AppendTable`) fixed `KeywordSearch` on `"Is the X20 Pro supported?"` (verified in `AgentDocsTests`), but pure dense `nomic-embed-text` cosine similarity still ranks `README.md › Credits` (`0.76`) and shorter sections ahead of the 7-row `Supported controllers` passage.
- **A similarity cut-off still cannot separate answerable from unanswerable questions alone.** Unanswerable queries still scored up to `0.73` (`0.74` baseline) above the lowest answerable score (`0.62`), so the `0.50` threshold drops off-topic hits while explicit refusal relies on the prompt and strict citation checking.
- **Every unanswerable question was refused (`3/3` latest, `9/9` baseline):** price, PS5 support, and battery hours all produced `"I can't confirm that from my docs."` with empty `sources` and `actions`.
- **What the 3 single-pass misses show:** `"Is the X20 Pro supported?"` still missed in dense embeddings (`Credits` retrieved instead of `Supported controllers`), `"Is the exe signed?"` gave the right unsigned-release answer while citing `License` instead of `Download`, and `"What does the Controller Check do?"` drifted to `Diagnostics` in one stochastic pass. Keeping the entire `Download` section intact (`Words = 300`) restored `"Does it work on Linux?"` (`1/1`) and expanded top-4 context fixed `"PC mode"` (`6/6`).

Run with `--agent-m5 DIRECTORY` (or `--agent-qwen DIRECTORY`, `--agent-m3 DIRECTORY`, `--agent-drift DIRECTORY`); the per-turn log is progress.txt.
