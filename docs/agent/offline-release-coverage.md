# Offline assistant coverage and release direction

## Intended product boundary

The owner explicitly decided on 15 October 2026 that Qwen/Ollama mode is for the school project; the intended later shipping assistant is offline-only. This class-project build still includes Qwen. No public release, model removal or public-repository merge was performed.

## What changed

- Embedded phrasebook: 2,459 to 2,547 phrases, with 88 authored additions across existing intent groups. Added from capability meanings, not from the frozen final evaluation set.
- Local knowledge covers connection trouble, profiles/saves, ViGEmBus/HidHide and double input, privacy/model requirements, circularity error, latency/polling distinctions, advanced stick tools, console limitations and honest macro/paddle limitations.
- Polite requests and common controller wording are normalized. Negative preferences retain their direction; negative numbers are no longer read as positive values.
- Relative curve follow-ups use the last changed controls and the current curve points. A vague improvement request asks for a goal; an answer such as 'steadier aim' completes that request.
- Complete multi-command clauses keep their own control/value pairs. Ambiguous requests assigning multiple values ask for details rather than applying the first value to every named control.
- Unknown wording gives relevant example ideas and HELP, with no mutation. Unsupported arbitrary macros and firmware writes are explained rather than claimed as completed.
- HELP now includes custom points, output caps, axis ratio, mouse mode, diagnostics, connection trouble, default response restoration, double input and offline operation.

## Limits

This is a controller-focused deterministic assistant, not a pretrained general language model. It cannot answer every possible question, create arbitrary timed macros, prove controller identity from a generic Windows name, or gain hardware-write support from a larger phrasebook. Suggestions still use the shared action vocabulary, validator, applier and measured drift workflow. Offline answers do not require an API, internet or an installed chat model.

## Verification

Targeted new offline release tests cover questions without mutation, negative preferences/numbers, literal remaps, compound values, ambiguity, goal clarification and relative curve follow-ups. Existing development benchmarks remain regression checks, not a fresh generalization claim. The frozen OfflineFinal set was not rescored or used to author phrases.

See offline-release-polish-plan.md for the associated UI scope. Component fixtures use synthetic data and render only their own WPF visuals; no desktop capture or full-app stress harness was run. Full live visual acceptance of the Zone-to-Dashboard flight remains for the owner.

Final verification: 446 Zone tests passed; Product build passed with six existing warnings. Updated scan and invitation components were rendered and the invitation callback checked. No Qwen model evaluation, live controller test or full-app screen capture was run in this task.
