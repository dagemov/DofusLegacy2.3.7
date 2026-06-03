# Admin Tools Migration Risk Register

## Risk table

| ID | Risk | Phase(s) | Impact | Likelihood | Mitigation | Trigger |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | `sunshine` schema differs from legacy `Rollback` assumptions used by old Blazor or old Admin API code | 1-6 | High | Medium | re-audit tables per module before code port, avoid blind SQL reuse | first contract or repository implementation assumes a missing/renamed column |
| R2 | Client publish workflows for items/spells depend on local curated client folders and FFDec | 3-4 | High | High | keep publish logic in infrastructure, require backups, document local prerequisites | publish flow cannot find `Items*.swf`, `Spells*.swf`, `SpellLevels*.swf`, `i18n*.swf`, or FFDec |
| R3 | Spell migration can introduce gameplay regressions through level/effect/glyph-trap mismatch | 4 | Very high | High | stage spells after items, add dry-run/validation, require in-game QA | edited spell saves but behaves incorrectly in combat |
| R4 | Map and monster-group tooling can corrupt live spawn data if direct writes are enabled too early | 6 | Very high | Medium | start read-only, add guarded writes later, require DB backups | write endpoints are requested before audit views and validation are stable |
| R5 | Secrets or operator credentials get committed during VPS/DB documentation or config work | 2, 8 | High | Medium | use placeholders only in docs, keep secrets in ignored local files or password manager | password or key appears in `git status`, diff, or screenshots |
| R6 | Multiple worktrees or parallel repo histories create competing truths for roadmap, docs, and accepted baselines | 0-8 | High | High | use the official repo as the single source of truth, avoid new external worktrees unless explicitly approved, and consolidate exploratory docs before starting new phases | a phase advances outside `C:\\Users\\Hombr\\source\\repos\\DofusLegacy2.3.7` and the official roadmap no longer matches the implementation story |
| R7 | Team copies large legacy folders or assets instead of curating them | 3-7 | High | Medium | ban folder-level copy, require inventory-driven selection, review diffs for volume | PR contains `bin/obj/artifacts` or bulk assets with no module rationale |
| R8 | Public website auth flow and future admin auth flow become entangled | 2 | Medium | Medium | keep admin API separate from public website shell, reuse auth library but not UI flow | operator-only concerns start leaking into public pages |
| R9 | Lack of automated or scripted validation leaves risky admin mutations under-tested | 3-6 | High | Medium | define smoke tests and manual QA checklists per module | module is "done" without CRUD/publish/in-game validation notes |
| R10 | Oversized commits make rollback and blame difficult | all | Medium | High | commit by intention, split docs, scaffolding, and feature work | a single commit mixes docs, DB config, assets, and multiple modules |
| R11 | Admin metadata tables from Blazor are reused without confirming long-term ownership | 1, 3-5 | Medium | Medium | review each admin-only table before porting, keep schema small and explicit | code tries to recreate or extend old support tables without documented decision |
| R12 | Monster/NPC visual preview strategy is weaker than the item preview strategy | 5 | Medium | High | treat preview renderer as a separate concern, do not promise full look fidelity too early | UI shows placeholder assets where operators expect in-client rendering |
| R13 | Item runtime identity, client `IconId`, and `AppearanceId` are conflated during the Items rewrite | 3 | High | High | keep separate DTO fields, keep diagnostics visible, never imply `ItemId == IconId` in UI or API | preview or publish logic selects the wrong bitmap for a valid item |
| R14 | Manual preview uploads are stored inside tracked repo paths and create asset bloat or noisy diffs | 3 | Medium | High | use ignored or external storage roots, document cleanup and ownership rules before implementation | `git status` starts showing uploaded PNGs after local admin use |
| R15 | The first Items slice tries to implement client publish work before preview and contracts are stable | 3 | High | Medium | keep Phase 1 as audit/contracts only, start implementation with read models and preview diagnostics | the branch begins touching `Items*.swf`, `i18n*.swf`, or local client bitmap packs before CRUD is proven |
| R16 | The Admin scaffold leaks direct database access into the API layer instead of staying behind Application abstractions | 1.5-3 | High | Medium | keep MySQL access inside Infrastructure, expose only interfaces to Api, review project references on every feature slice | API endpoints begin creating `MySqlConnection` or querying SQL directly |
| R17 | Real Sunshine credentials are committed through tracked admin development settings | 1.5-8 | High | Medium | commit only example settings, ignore `appsettings.Development.local.json`, keep placeholder passwords in tracked files | a real password appears in `src/Admin/RollblackLegacy.Admin.Api/appsettings*.json` diff |
| R18 | Items read-only API is marked complete without a real local `SunshineAdmin` validation pass | 2-3 | Medium | Medium | require one ignored-local-secret or equivalent safe runtime pass before full sign-off, keep explicit validation notes in docs, use the local Sunshine dump or equivalent seeded runtime | endpoints compile and return controlled `500` but no real item rows were queried locally |
| R19 | Browser clipboard APIs vary across localhost and in-app browser contexts, making copy helpers look broken to operators | 3 | Low | High | keep manual fallback labels and visible copy values, never rely on silent clipboard success | an operator clicks Copy and no clipboard write occurs |
| R20 | Curated preview seeds grow into an accidental bitmap dump because `by-icon` assets are copied without strict review | 6 | Medium | Medium | keep item preview seeds at `1-3` files per focused phase, document source path and hash, ignore generated dump folders | a PR suddenly adds dozens of PNGs under `src/assets/item-previews` |
| R21 | A future SWF/i18n intelligence pass conflates inventory icon, equipped appearance, and multilingual names into one low-confidence mapping | 6.5-7 | High | Medium | document `IconId`, `AppearanceId`, `ClientNameId`, `NameEs`, and `NameEn` as separate fields first, treat extraction as a research lane before wiring it into CRUD flows | a future extractor starts claiming one field can substitute for all client identity surfaces |
| R22 | Phase 7 create/duplicate writes rely on `MAX(Id)+1` and `MAX(DescriptionId)+1` against a `MyISAM` table, so concurrent writes or casual smoke tests can leave junk data or collide unexpectedly | 7 | High | Medium | keep generated-id logic behind the repository, reserve live happy-path validation for disposable or explicitly backed-up datasets, surface `409` clearly, and avoid ad-hoc operator smoke writes on shared DBs | the team wants to “just test create once” on a shared Sunshine runtime with no cleanup path |
| R23 | The team resumes full `Create/Edit` before operators have a safe icon-selection flow, causing `IconId` mistakes and low-confidence writes | 7-7A | High | Medium | complete the icon selector slice first, keep `ItemId`, `IconId`, and `AppearanceId` visibly separate, and rely on curated preview evidence before reopening write scope | operators begin choosing icons from memory or Navicat alone during write flows |
| R24 | Team members assume Phase 8 `READY_FOR_QA` means a persisted workflow state or real client publish approval when the status is only derived in Admin | 8 | High | Medium | keep the panel text explicit, document that `sunshine.items` has no workflow fields yet, disable publish actions, and require a separate future client-publish phase | someone treats the QA badge as proof that client metadata, i18n, or packaged assets were already published |
| R25 | Manual in-game QA via `.item add` mutates live character inventory on a shared environment and leaves test items behind | 8 | High | Medium | document the exact command, restrict use to controlled QA characters, require cleanup discipline, and prefer disposable or local datasets for smoke passes | an operator gives experimental items to a real character and cannot easily roll back the inventory state |
| R26 | Operators assume the Admin API is talking to the VPS when the current Angular proxy and `SunshineAdmin` actually point to local services | 8+ | High | Medium | expose safe `health/db` target fields (`host`, `port`, `user`, `isRemote`), document the current wiring, and confirm it before every remote-validation pass | browser data looks real but the team cannot tell whether it came from local MySQL or the VPS |
| R27 | Team scripts for preview imports or world restarts are run in destructive mode without an audit pass first | 6+ | High | Medium | keep dry-run/report mode by default, block weapons and suspicious categories, require explicit restart confirmation, and never auto-restart the whole VPS | a large PNG batch lands in Git or a restart script bounces the wrong service |
| R28 | Phase 7 is treated as complete while effects/characteristics editing is still missing, locking in a low-value CRUD and creating migration debt | 7 | High | Medium | Phase 7B shipped (`PUT /items/{id}/effects`); keep Phase 7 as `PAUSED / PARTIAL` until 7C/8; do not claim full Blazor parity | roadmap claims full parity while conditions/publish still deferred |
| R29 | Conditions handling is simplified into rigid presets and loses legacy operator flexibility for advanced criteria strings | 7B-7C | Medium | Medium | keep raw string editor path, add non-blocking validation hints instead of hard preset-only builder | operators can no longer input required criteria syntax used in production workflows |
| R30 | Icon selector is considered "done" without enough preview-state guardrails, causing `IconId` mistakes during item creation | 7A | High | Medium | make icon selection/preview checks first-class before save, keep unresolved preview warnings visible, and preserve icon/appearance separation in UI | new items are saved with weak or wrong icon identity despite visual selector being present |
| R31 | Closing Phase 7A without backend/contract parity on icon payload fields causes modal/frontend drift (`PreviewState`, counts, samples) in later refactors | 7A-7B | Medium | Medium | lock `item-icons` payload contract now, keep Angular model aligned with contracts, and validate build for both Angular and .NET on every selector change | selector UI compiles but silently loses metadata fields needed by operators |
| R32 | Offline SQL grants into `characters_items` can be overwritten if the target character is still connected and World later saves its in-memory inventory snapshot | QA / live item rollout | High | Medium | apply inventory grant only while the target characters are offline or during a controlled World stop/restart window; keep restore SQL ready before apply | a live player logs out after the SQL grant and the new row disappears or inventory state becomes inconsistent |
| R33 | VPS restart flow is blocked by missing SSH key or agent access, leaving DB patches applied without the controlled World reload needed for the new template to be visible in runtime | QA / live item rollout | High | Medium | verify SSH access before live apply, keep DB scripts staged but unapplied when restart cannot be executed, and never fake a successful restart in docs | `ssh root@174.138.35.107` returns `Permission denied (publickey)` or the documented key path is missing |

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
- No preview/manual asset path should be tracked by Git unless explicitly curated.
- No curated preview seed should be committed without documenting its source path and why it was chosen.
- No tracked admin settings file should contain a real Sunshine password.
- No DB-dependent phase should be called fully validated unless it has a documented safe runtime path such as an ignored local secret file or equivalent non-tracked configuration.
- Operator copy/debug helpers must keep a visible manual fallback whenever clipboard APIs fail.
- No mutating happy-path validation should be run on shared Sunshine data unless the row lifecycle and backup/cleanup path are explicit first.
- No new Admin phase should start in an external worktree unless that deviation is explicitly approved first.
- No derived QA readiness badge should be treated as a persisted publish state until a dedicated workflow model exists.
- No in-game `.item add` validation should be done on shared characters without an explicit cleanup path.
- No remote-vs-local claim should be made without checking `GET /api/admin/v1/health/db`.
- No preview-import or VPS-restart script should default to destructive execution.
- No Create/Edit parity claim should be made until effects/characteristics editing is implemented and validated.
- No conditions redesign should remove the raw operator string path without explicit approval.
- No icon-selector milestone should close without preview-state warning and identity-safety checks.
- No offline inventory grant should be applied while the target characters may still be online.
- No live DB patch that depends on a World reload should be marked complete until SSH restart access is real and validated.
