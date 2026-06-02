# Phase 7A Plan - Item Icon Selector Modal

## Goal

Harden icon selection so operators stop relying on memory/manual IDs before continuing full Create/Edit parity work.

## Audit findings that drive this phase

- Legacy Blazor had strong preview/validator workflow but no full modern modal catalog UX.
- Current Angular already has `ItemIconSelectorComponent` and `GET /item-icons`.
- Current gap is not feature existence; it is operational safety, defaults, and parity confidence before advanced editing resumes.

## Phase 7A target outcome

- Icon selector modal is the default path for icon assignment in Create/Edit.
- Preview state and icon choice are explicit and operator-safe before save.
- `IconId` and `AppearanceId` separation remains visible at all times.

## Functional scope

1. **Selector UX hardening**
   - Keep embedded modal in write page.
   - Improve empty/error states and selected icon confirmation.
   - Support quick search by icon id and sample item names.
2. **Preview contract hardening**
   - Require preview-state refresh after icon selection.
   - Keep explicit warning when preview is unresolved.
3. **Write-flow guardrails**
   - Keep save allowed with warning for missing preview, but require explicit operator confirmation UX.
   - Preserve icon choice in form reset/duplicate flows where applicable.
4. **Diagnostics**
   - Add concise warning copy for `IconId <= 0`, missing PNG, and weak client identity risk.

## Data and asset model

- PNG source remains curated under:
  - `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon`
- Resolver path remains API-driven:
  - `GET /api/admin/v1/item-icons`
  - `GET /api/admin/v1/items/preview-state`

## Acceptance criteria

- Operator can select icon visually without leaving write page.
- Selected icon updates preview immediately.
- Save result and warnings remain consistent with selected icon.
- Duplicate flow does not accidentally reuse wrong icon due to stale selector state.

## Risks and mitigations

- **Risk:** sparse preview assets reduce confidence.
  - **Mitigation:** keep unresolved-preview warnings explicit and searchable icon metadata visible.
- **Risk:** selector perceived as optional.
  - **Mitigation:** make selector path prominent in form and near IconId input.
- **Risk:** confusion between icon and appearance.
  - **Mitigation:** maintain side-by-side labels and warning copy in write page.

## Out of scope (7A)

- Effects/stat editing payload.
- Conditions builder redesign.
- Client publish workflows.
