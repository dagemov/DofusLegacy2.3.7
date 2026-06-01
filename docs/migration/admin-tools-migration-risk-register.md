# Admin Tools Migration Risk Register

## Risk table

| ID | Risk | Phase(s) | Impact | Likelihood | Mitigation | Trigger |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | `sunshine` schema differs from legacy `Rollback` assumptions used by old Blazor or old Admin API code | 1-6 | High | Medium | re-audit tables per module before code port, avoid blind SQL reuse | first contract or repository implementation assumes a missing/renamed column |
| R2 | Client publish workflows for items/spells depend on local curated client folders and FFDec | 3-4 | High | High | keep publish logic in infrastructure, require backups, document local prerequisites | publish flow cannot find `Items*.swf`, `Spells*.swf`, `SpellLevels*.swf`, `i18n*.swf`, or FFDec |
| R3 | Spell migration can introduce gameplay regressions through level/effect/glyph-trap mismatch | 4 | Very high | High | stage spells after items, add dry-run/validation, require in-game QA | edited spell saves but behaves incorrectly in combat |
| R4 | Map and monster-group tooling can corrupt live spawn data if direct writes are enabled too early | 6 | Very high | Medium | start read-only, add guarded writes later, require DB backups | write endpoints are requested before audit views and validation are stable |
| R5 | Secrets or operator credentials get committed during VPS/DB documentation or config work | 2, 8 | High | Medium | use placeholders only in docs, keep secrets in ignored local files or password manager | password or key appears in `git status`, diff, or screenshots |
| R6 | Working tree drift or mixed local changes contaminate migration branches | 0-8 | Medium | High | use clean worktrees for planning and focused branches for features | branch switch blocked by unrelated local files |
| R7 | Team copies large legacy folders or assets instead of curating them | 3-7 | High | Medium | ban folder-level copy, require inventory-driven selection, review diffs for volume | PR contains `bin/obj/artifacts` or bulk assets with no module rationale |
| R8 | Public website auth flow and future admin auth flow become entangled | 2 | Medium | Medium | keep admin API separate from public website shell, reuse auth library but not UI flow | operator-only concerns start leaking into public pages |
| R9 | Lack of automated or scripted validation leaves risky admin mutations under-tested | 3-6 | High | Medium | define smoke tests and manual QA checklists per module | module is "done" without CRUD/publish/in-game validation notes |
| R10 | Oversized commits make rollback and blame difficult | all | Medium | High | commit by intention, split docs, scaffolding, and feature work | a single commit mixes docs, DB config, assets, and multiple modules |
| R11 | Admin metadata tables from Blazor are reused without confirming long-term ownership | 1, 3-5 | Medium | Medium | review each admin-only table before porting, keep schema small and explicit | code tries to recreate or extend old support tables without documented decision |
| R12 | Monster/NPC visual preview strategy is weaker than the item preview strategy | 5 | Medium | High | treat preview renderer as a separate concern, do not promise full look fidelity too early | UI shows placeholder assets where operators expect in-client rendering |

## Priority watchlist

Highest-risk modules for execution order:

1. spells
2. maps
3. monster groups and spawn writes

Lowest-risk starting points:

1. architecture and contracts
2. account/website integration alignment
3. items

## Operational reminders

- No production-like DB write should happen without a backup checkpoint.
- No client publish step should happen without file backups.
- No secret should enter tracked files.
- No browser UI should gain direct DB access.
