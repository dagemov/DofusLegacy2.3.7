# Items Builder Blazor Parity Audit (Phase 7 Corrective)

## Scope

- Official repo only: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`
- Legacy reference audited (read-only): `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\Rollback.Web` plus `Rollback.Admin`
- Focus limited to Items Builder parity for non-weapon flow
- No DB writes, no client file writes, no gameplay changes

## Legacy Blazor functional inventory

| Feature area | File | Component/class | Method(s) | DB field(s) touched | Serialization format | Functional value | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Item list + filters + detail shell | `Rollback.Web/Pages/Admin/Items.razor` | page component | `ReloadAsync`, `LoadItemAsync`, `CreateNewAsync` | Reads from runtime item row via service | n/a | Operator works in one screen (list + editor + preview) | `PORT_TO_ANGULAR` |
| Item create/update/delete orchestration | `Rollback.Admin/Services/ItemAdminService.cs` | `ItemAdminService` | `SaveAsync`, `DeleteAsync` | `items_templates`: `Id`, `TypeId`, `Level`, `Weight`, `Usable`, `Targetable`, `Etheral`, `Price`, `ItemSetId`, `StringCriterion`, `AppearanceId`, `BinaryEffects`, `RecipesCSV`, `TwoHanded`, `APCost`, `MinRange`, `MaxRange`, `CastInLine`, `CastTestLOS`, `CriticalHitProbability`, `CriticalHitBonus`, `CriticalFailureProbability` | `BinaryEffects` is binary blob from `EffectManager.SerializeEffects` | Rich runtime editing beyond basic CRUD | `REIMPLEMENT_CLEAN` |
| Effects editor UI | `Rollback.Web/Components/Admin/EffectListEditor.razor` | `EffectListEditor` | `AddEffectRow`, `UpdateEffectId`, `UpdateEffectKind`, row move/remove helpers | indirect through item save to `BinaryEffects` | Per-row typed model (integer/dice/string/duration/date/mount/base) | Real effect/stat editing for operators | `PORT_TO_ANGULAR` |
| Effects deserialize/serialize engine | `Rollback.Admin/Services/GameEffectEditorService.cs` | `GameEffectEditorService` | `Deserialize`, `Serialize`, `MapToRow`, `MapToEffect` | `items_templates.BinaryEffects` | Native `EffectManager.DeserializeEffects` / `SerializeEffects` | Canonical mapping between runtime effects and UI rows | `REIMPLEMENT_CLEAN` |
| Item edit model | `Rollback.Admin/Models/Items/ItemEditModel.cs` | `ItemEditModel` | n/a | carries all write columns above + diagnostics fields | `Effects: List<GameEffectEditRow>` | Full Create/Edit payload including advanced combat fields | `REIMPLEMENT_CLEAN` |
| Conditions handling | `Rollback.Web/Pages/Admin/Items.razor` | page component | bound input `_form.StringCriterion` | `items_templates.StringCriterion` | raw string | Advanced manual condition editing allowed | `PORT_TO_ANGULAR` |
| AP/range/critical fields | `Rollback.Web/Pages/Admin/Items.razor` + `ItemAdminService.SaveAsync` | page + service | binds + save | `APCost`, `MinRange`, `MaxRange`, `CriticalHitProbability`, `CriticalHitBonus`, `CriticalFailureProbability` | numeric scalar columns | Real gameplay metadata editing | `REIMPLEMENT_CLEAN` |
| Icon/appearance preview + validator | `Rollback.Web/Pages/Admin/Items.razor`, `Rollback.Web/Components/Admin/GameAssetPreview.razor` | page + `GameAssetPreview` | `ResolveBitmapLookup`, preview resolve in component | no mandatory write | item preview resolved by `IconId`/bitmap + `AppearanceId` context | Visual safety before save | `PORT_TO_ANGULAR` |
| Type selector | `Rollback.Web/Pages/Admin/Items.razor` | page component | `OnTypeChanged` | `TypeId` | enum option mapping | Prevents invalid free typing for type | `PORT_TO_ANGULAR` |
| Set selector state | `Rollback.Web/Pages/Admin/Items.razor` | page component | bound `ItemSetId` | `ItemSetId` | scalar | Explicit set linkage in editor | `PORT_TO_ANGULAR` |
| Number formatting in list | `Rollback.Web/Pages/Admin/Items.razor` | page component | inline `@item.Price.ToString("N0")` | read-only | locale numeric format | Better operator readability (`1,000`) | `PORT_TO_ANGULAR` |
| Diagnostic/warning panels | `Rollback.Web/Pages/Admin/Items.razor` + `Rollback.Admin/Services/ItemIdentityDiagnosticService.cs` | page + service | diagnostic calls through `GetByIdAsync` result | joins runtime + metadata tables | structured DTO snapshots | Avoids unsafe blind writes | `REFERENCE_ONLY` |

## Legacy support for requested capabilities

| Capability | Legacy status | Evidence |
| --- | --- | --- |
| Effects editor | Present | `EffectListEditor.razor` + `GameEffectEditorService` |
| AP editing | Present (`APCost`) | `Items.razor` + `ItemAdminService.SaveAsync` |
| PM editing (direct column) | Not explicit as direct column in item template | No dedicated `PM` scalar in audited save statement |
| Stats/bonuses editing | Present through effect rows | effect types and `EffectId`-based rows in `BinaryEffects` |
| Icon visual selector | Partial | visual preview and bitmap validator exist; no explicit modal picker like current Angular component |
| Preview before save | Present | live preview cards + bitmap validator |
| Conditions advanced string | Present | `StringCriterion` bound and persisted as raw string |
| Operator-centered edit flow | Present | single-screen list/detail/effects/preview/save |

## Effects and characteristics audit findings

- Legacy authoritative storage is `items_templates.BinaryEffects` (blob), not plain text.
- Legacy parsing/serialization is delegated to runtime effect engine:
  - Deserialize: `EffectManager.DeserializeEffects(binaryEffects)`
  - Serialize: `EffectManager.SerializeEffects(effects)`
- Effect row model supports mixed kinds: integer, dice, string, duration, date, mount, base.
- AP/PM/stats are represented as effect actions (`EffectId`) inside serialized effects where relevant, plus `APCost` for cast cost.
- Validation is mostly structural and editor-driven (row kind coercion, known option fallback), with limited semantic hard stops in UI layer.

## Icon selector and preview audit findings

- Legacy preview resolves from `GameAssetPreviewService` using entity identity and appearance context.
- Legacy also exposes manual bitmap validation (`16134`, `16134.png`) before saving.
- Legacy flow distinguishes:
  - `IconId` (inventory bitmap identity)
  - `AppearanceId` (equipped/visual appearance)
  - `ItemId` (runtime row id)
- Legacy does not expose a dedicated modal icon catalog browser equivalent to Angular `ItemIconSelectorComponent`; this is a parity gap in UX shape, not in concept.

## Conditions audit findings

- Conditions are stored as plain string (`StringCriterion` in legacy templates, `Criteria` in Sunshine table).
- Legacy UI uses free-form input (advanced operator text semantics preserved).
- No visual builder for conditions was found in audited items flow.
- Validation is minimal (mainly non-destructive persistence of raw operator string).

## Numeric formatting audit findings

- Legacy list display formats price with grouped thousands (`N0`).
- Editor numeric inputs remain raw numbers for data entry.
- Recommendation parity:
  - Keep grouped display in lists/readonly labels.
  - Keep raw editable numeric controls in write forms.

## Out of scope confirmations

- No weapon workflow audited.
- No mass row scan (no 44k traversal) performed.
- No worktree/repo cloning used.
- No writes executed against DB or client assets in this phase.
