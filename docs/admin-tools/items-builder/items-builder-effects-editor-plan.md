# Phase 7B Plan - Items Effects/Characteristics Editor

## Goal

Reintroduce functional parity for item effects/stat editing in Angular + Admin API without copying legacy Blazor stack.

## Audit-backed baseline

- Legacy runtime source uses `items_templates.BinaryEffects` (blob) with runtime serializer (`EffectManager`).
- Current Sunshine Admin flow reads/writes `sunshine.items.Effects` as hex text and only decodes it for detail view.
- Current write flow does **not** accept/edit effects payload.

## Phase 7B target outcome

- Operator can edit effects/characteristics in Create/Edit with clear row semantics.
- API validates and persists effect payload safely.
- UI keeps identity separation (`ItemId`, `IconId`, `AppearanceId`) while adding effect editing.

## Proposed architecture (clean reimplementation)

1. **Contracts**
   - Add write DTO for effects rows (action id + dice/value payload).
   - Keep backward-safe defaults for items with empty effects (`0000`).
2. **Application service**
   - Validate structural integrity of submitted effects rows.
   - Keep non-weapon guardrails unchanged.
3. **Infrastructure serializer**
   - Add explicit Sunshine effect codec in Admin Infrastructure.
   - Decode existing `items.Effects` -> editable row model.
   - Encode submitted row model -> persisted hex string.
4. **Angular form**
   - Add dedicated `EffectsEditor` section (row add/remove/reorder, effect id, value/dice fields).
   - Keep initial row presets for common stats but allow advanced IDs.
5. **Diagnostics**
   - Surface parser/serialization warnings before save.
   - Keep save allowed when safe fallback applies, block on malformed payload.

## AP/PM/stats handling rules

- AP/PM/stats are represented by effect action IDs in effect payload (not inferred from name).
- No invented mappings: use protocol enum source already used by `AdminProtocolCatalog`.
- Preserve unknown but valid IDs as pass-through with explicit warning label.

## Validation rules (Phase 7B)

- Reject malformed effect rows (invalid numeric bounds, impossible dice tuple, missing required fields for selected shape).
- Preserve deterministic order of rows.
- Do not silently drop unknown action IDs; store when structurally valid.
- Keep conditions field independent from effects editor.

## Testing strategy

- Unit tests for encode/decode round-trip with representative effects.
- API tests for create/update/duplicate with effects payload.
- UI form tests for row editing and payload emission.
- Manual QA checklist:
  - create item with simple stat effect
  - update existing item effect
  - duplicate item and verify effects copied/edited as expected

## Risks

- Sunshine effect format drift or unsupported effect variants.
- Operator confusion if labels exist but serialization type is ambiguous.
- Regressions in detail decode for legacy rows.

## Mitigation

- Keep codec isolated and heavily tested.
- Keep unknown-effect fallback with warning instead of destructive normalization.
- Gate rollout after Phase 7A icon selector stabilization.

## Non-goals in Phase 7B

- Weapon-specific effect workflows.
- Client publish or i18n text publish.
- Gameplay balance logic.
