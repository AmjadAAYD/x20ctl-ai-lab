# Offline assistant and UI polish — 15 October 2026

Authorized scope: improve the Dashboard help invitation and scan panels; improve the Zone-to-Dashboard transition; substantially broaden offline assistant usefulness. Qwen stays in this class-project build; eventual shipping direction is offline-only.

1. Audit current offline routing, follow-up memory and help coverage. Add deterministic routing/knowledge and curated phrase examples, preserving validators, honest unsupported operations, and measured drift.
2. Add independent regression cases for paraphrases, explanations, relative follow-ups, negative/out-of-range values, cancellation, and requests needing details. Check the existing development cases; do not tune to or rescore the frozen final evaluation set.
3. Redesign the invitation as an Agent XCTL chat card; polish scan layout/empty-state hierarchy; add staged Dashboard entry motion with reduced-motion handling.
4. Build, run affected tests, inspect isolated component renders (no screen capture or full-app stress harness), commit privately, and relaunch.

No claims of universal question coverage or new hardware-write support. No public publishing or immediate removal of the school model.
