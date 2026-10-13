Do a research on how an AI agent chatbot usually actually functions, and compare it with the agent I've built (context below). I want to know what real agents do that mine doesn't, what mine does well, and what I should add next. This is for a university class project (Generative Text AI, due 15 Nov 2026), so explain the concepts clearly and cite sources (papers, official docs, well-known open-source projects).

Research questions:
1. The standard architecture of an AI agent chatbot: the loop (perceive, reason, act, observe), tool or function calling, structured output, memory (short-term and long-term), planning, retrieval (RAG), guardrails and validation, evaluation.
2. How the well-known frameworks and products do it: ReAct, OpenAI function calling and Assistants, Anthropic tool use, LangChain and LangGraph agents, AutoGen, small local models through Ollama. What do they have in common?
3. How agents that control a real app or device usually work: intent recognition vs free-form tool calls, deterministic validation before acting, undo, and confirmation vs acting immediately.
4. Persona and character design for an assistant that "is" a product (it speaks as the controller): how to keep a fun personality without hurting accuracy.
5. Small local models (~9B parameters): their known limits (JSON reliability, hallucination, latency), and the techniques that help (schema-constrained decoding, retries, few-shot prompts, RAG).
6. Evaluating such an agent: what metrics and test sets are standard, and how to keep a fair held-out test set.

Then give me:
- a short comparison table: "typical agent" vs "my agent", component by component;
- the three to five most valuable improvements for my agent, ranked, each with why and how;
- a reading list with links.

Don't change any code. This is research only.

---

CONTEXT: what I've built (Agent XCTL)

The app: X20CTL, a Windows app (C#, WPF, .NET 10) that configures EasySMX game controllers (sticks, triggers, button remaps, turbo, back paddles). Settings reach games through "PC mode", a virtual Xbox pad. The agent lives inside the app, in a private repo.

The model: Qwen 3.5 9B, running locally through Ollama (/api/chat with a JSON schema in "format", think:false). Everything runs on the laptop; nothing goes to the cloud. nomic-embed-text is used for embeddings.

The pipeline, for every message:
1. A system prompt: the persona ("you are the controller, speak in first person"), a fixed list of actions with their ranges, the meaning of every setting, and the current settings.
2. The model answers with one JSON object: speech, actions [{action, target, value}], recap (why, from the settings' meanings), assumptions, sources.
3. A deterministic validator checks the JSON: its structure, the allowed targets for this controller model, the value ranges, and rules (for example, M5 doesn't exist on the X20). If it fails, the model gets one repair attempt with the reason.
4. Valid changes are applied immediately, with Undo (no approval step), saved to PC mode, then shown in a walk-through on the pages where they live, and the changed controls glow.
5. The model's words are shown as its proposal. The status line ("Applied · 2 changes" or "Rejected · nothing changed") comes from the app, never from the model.

Tools and actions:
- Settings: stick and trigger dead zones, curves and "full at", anti-dead zone, remaps, turbo, paddles, reset, and PC mode on or off.
- A read tool: measure_drift. The app reads the real sticks through XInput for a few seconds and feeds the measurement back to the model, which must set the dead zone above the measured peak (the validator enforces this).
- App commands handled before the model: HELP; naming the controller ("That's your controller, what's wrong with it?"); battery level (XInput); change controller; star the repo; donate.

RAG (Help): the README and two guides, chunked by heading (140 words, 30 overlap, 51 passages). Search by meaning (embeddings with a cache, cut-off 0.5), falling back to keywords. The top three passages go into the prompt. The model must cite their numbers or say "I can't confirm that from the docs", and the validator rejects citations it wasn't given.

Offline mode (no LLM): a phrasebook classifier.
- Slots (sticks, triggers, buttons, numbers) are extracted with regex.
- Some requests are ruled out by the sentence's own words.
- TF-IDF over words and word pairs, a 7-nearest-neighbour weighted vote over 142 hand-written examples, plus 2,459 paraphrases written by Qwen and cleaned by rules and a leave-one-out vote.
- It writes the same JSON the model does, so validation and Undo are shared.

Search and ideas: a second search box lists example requests matching the typed words (with prefix matching), and the agent speaks up by itself after the search.

Character: a goofy controller that knows its job. Off-topic questions get "How silly, I'm a controller, not a calculator!", and its eyes (the sticks) turn into "> <". It never calls itself silly. Numbers and status lines stay exact.

UI: the chat window is drawn as a controller. Its sticks are eyes that look and blink; the player LEDs blink blue while it searches or thinks, then green (good), violet (unsure) or red (failed). LB is Offline, RB the settings, A send, B away, X new, Y search. An applied change rumbles the real pad.

Evaluation so far:
- Qwen: 40 sentences × 3 runs; drift 15/15; M3 (PC mode) 24/24.
- Offline: development set 52/60; final held-out test 17/20 (examples only) and 16/20 (shipped), each scored once.
- RAG: retrieval 19/20 by meaning, 17/20 by keywords; answers 63/69, every out-of-docs question refused (9/9).
- 273 unit tests (xUnit).

Known gaps: no multi-turn memory beyond the current conversation; no macro generation yet; no streaming; one repair attempt only; the similarity cut-off can't separate answerable from unanswerable questions (0.64 vs 0.74).
