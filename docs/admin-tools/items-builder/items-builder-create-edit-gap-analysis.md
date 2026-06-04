# Items Builder Create/Edit Gap Analysis (Blazor vs Angular/Admin API)

## Intent

Define exactly what exists today and what must be completed before resuming full Phase 7 Create/Edit implementation.

## Comparison matrix

| Feature | Blazor old | Angular current | Admin API current | Gap | Priority | Decision | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| List preview image | Present (`GameAssetPreview`) | Present (`item-preview-card`, icon preview path) | Present (`preview-state`) | aligned | Medium | `PORT_TO_ANGULAR` | `DONE` |
| Icon selector modal | Partial (preview + validator, no full modal catalog) | Present (`ItemIconSelectorComponent`) | Present (`GET /item-icons`) | usable but needs parity hardening in write flow defaults | High | `REIMPLEMENT_CLEAN` | `PARTIAL` |
| Create item | Present | Present | Present (`POST /items`) | currently basic field set only | High | `REIMPLEMENT_CLEAN` | `PARTIAL` |
| Edit item | Present | Present | Present (`PUT /items/{id}`) | currently basic field set only | High | `REIMPLEMENT_CLEAN` | `PARTIAL` |
| Duplicate item | Present (operator flow via load/new semantics) | Present | Present (`POST /items/{id}/duplicate`) | still tied to basic payload | Medium | `REIMPLEMENT_CLEAN` | `PARTIAL` |
| Effects editor | Present (rich typed rows) | Missing in current write form | Read-only decode only (`ItemEffectDto` in detail) | cannot edit serialized effects | Critical | `PORT_TO_ANGULAR` | `MISSING` |
| Stats/characteristics editor | Present via effects + AP/range/crit fields | Missing as dedicated editing workflow | Missing write support for effects/stat actions | no gameplay-stat parity | Critical | `REIMPLEMENT_CLEAN` | `MISSING` |
| AP/PM editing | AP cost present; PM/stats mainly via effects | Missing | Missing in write contract/repo | high-risk parity gap | Critical | `REIMPLEMENT_CLEAN` | `MISSING` |
| Conditions string editor | Present (raw criterion string) | Present (`conditions` field) | Present (`Criteria`) | mostly parity, needs UX polish and validation hints | Medium | `PORT_TO_ANGULAR` | `PARTIAL` |
| Set selector | Present (`ItemSetId`) | Present | Present (`SetId` -> `ItemSetId`) | aligned | Medium | `PORT_TO_ANGULAR` | `DONE` |
| Type selector | Present | Present | Present (type options + validation) | aligned | Medium | `PORT_TO_ANGULAR` | `DONE` |
| IconId vs AppearanceId separation | Explicit and documented in UI | Explicit in form/warnings | Explicit in contracts/repo | aligned, keep strict | High | `PORT_TO_ANGULAR` | `DONE` |
| Number formatting | Present (formatted list, raw editor) | Partial | n/a | list/detail formatting policy not fully standardized | Low | `REIMPLEMENT_CLEAN` | `PARTIAL` |
| Warnings/diagnostics | Rich diagnostic panel | Partial advisory warnings | Partial warnings + QA summary | weaker than legacy operator confidence layer | High | `REIMPLEMENT_CLEAN` | `PARTIAL` |
| Preview before save | Present (live + validator) | Present (preview-state + icon selector) | Present (`ResolvePreviewState`) | aligned for icon preview only | Medium | `PORT_TO_ANGULAR` | `DONE` |

## Core parity blockers

1. No editable effects/characteristics pipeline in current Angular write form.
2. Current write contracts do not carry any effects payload.
3. Current write repository hardcodes `Effects = "0000"` on create and copies source effects on duplicate.
4. AP/range/crit and other advanced runtime attributes from legacy template model are not represented in Sunshine write contract.

## Practical impact

- Current implementation works as constrained CRUD but does not reproduce old operator value.
- Resuming "full Create/Edit" now would lock in a reduced product and create future migration debt.

## Corrective sequencing

- **Phase 7**: keep `PAUSED / PARTIAL` until parity blockers are addressed.
- **Phase 7A (next)**: icon selector modal hardening and preview safety contracts.
- **Phase 7B (after 7A)**: effects/characteristics editor (parse + edit + serialize).
- **Phase 7C (after 7B)**: form UX polish, diagnostics strengthening, numeric and conditions ergonomics.

## Explicit out-of-scope for this corrective audit

- Weapons workflow.
- Mass data traversal.
- Client publish implementation.
- DB/schema mutation outside documented parity plan.
