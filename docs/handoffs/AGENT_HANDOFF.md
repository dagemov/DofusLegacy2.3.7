# Agent Handoff — Items Builder (DofusLegacy2.3.7)

**Generated:** 2026-06-02  
**Read this file before starting any implementation work.**

---

## Repository

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
```

Official source of truth only. No external worktrees, clones, or parallel repos.

**Admin stack location (not `src/Admin/`):**

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular
Angular-tools/Admin/RollblackLegacy.Admin.Api
Angular-tools/Admin/RollblackLegacy.Admin.Application
Angular-tools/Admin/RollblackLegacy.Admin.Contracts
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure
```

**Legacy Blazor reference (read-only, outside repo):**

```txt
C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\Rollback.Web
C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\Rollback.Admin
```

---

## Branch

```txt
feature/items-builder-vps-qa-stabilization
```

Optional clean branch name if splitting work later:

```txt
feature/items-builder-icon-selector-phase7a   (already merged into work above via commits)
feature/items-builder-effects-phase7b       (suggested for 7B+)
```

**Do not commit unrelated dirty files:**

- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/WorldServerManager.cs` (pre-existing local change)
- `Client2.3.7/**`, `config/Database*.xml` (untracked, likely secrets/local)

---

## Current phase

```txt
Phase 7B.0 — Effects Serialization Audit (NEXT)
```

Sub-phase of:

```txt
Phase 7B — Item Effects/Characteristics Editor
```

**Current status:** `NOT STARTED` (audit + implementation pending)

---

## Last completed phase

```txt
Phase 7A — Item Icon Selector Modal — DONE
```

Prior corrective audit:

```txt
Phase 7 — Blazor Parity Corrective Audit — DONE (docs only, commit cbfb3d3)
```

---

## Current status (roadmap snapshot)

| Phase | Status |
| --- | --- |
| Phase 1 – Items Builder Audit | DONE |
| Phase 1.5 – Admin Clean Architecture Scaffold | DONE |
| Phase 2 – Read-only API | DONE / PARTIAL VALIDATED |
| Phase 3 – Angular List/Detail | DONE / PARTIAL VALIDATED |
| Phase 4 – Diagnostics + Preview UI | DONE |
| Phase 5 – Live Data Workflow | DONE |
| Phase 6 – Asset Pipeline + PNG Preview | DONE |
| Phase 6.5A – Client Asset Intelligence Audit | DONE |
| Phase 7 – Item Create/Edit | **PAUSED / PARTIAL** |
| Phase 7A – Item Icon Selector Modal | **DONE** |
| Phase 7B – Effects/Characteristics Editor | **NEXT** (start with 7B.0 audit) |
| Phase 7C – Item Form UX Polish | PENDING |
| Phase 8 – Publish / QA Workflow | PENDING |

---

## What Phase 7A delivered

- Bootstrap modal `ItemIconSelectorModalComponent` wired into Create/Edit (`item-write-page`).
- Standalone route kept: `/admin/items/icon-selector`.
- Icon selection sets `form.iconId`, refreshes preview via existing `getPreviewState`; **does not** change `AppearanceId`.
- List page mini-preview column when `previewState.resolvedPath` or `byIconPath` exists.
- API: `GET /api/admin/v1/item-icons` unchanged route; `ItemIconOptionDto` extended with `PreviewState` (`FOUND` for curated PNG catalog entries).

---

## Commits created (this arc)

| Commit | Message |
| --- | --- |
| `727d2c6` | `feat: add item icon selector modal` |
| `cbfb3d3` | `docs: audit item builder parity gaps with legacy blazor` |

Earlier related (same branch, context only):

| Commit | Message |
| --- | --- |
| `8fc7819` | `docs: document admin api vps database profile` |
| `5e38e1a` | `fix: stabilize items builder live vps workflow` |
| `3b66428` | `feat: add items builder create edit workflow` |
| `da75e03` | `feat: add item icon selector for builder workflow` |

---

## Files touched (Phase 7A commit `727d2c6`)

**Angular**

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-icon-selector-modal.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-icon-selector-modal.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-icon-selector-modal.component.scss`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-write-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-write-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-write-page.component.scss`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-icon-selector.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/items-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/items-page.component.scss`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.models.ts`

**Admin API**

- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Items/ItemIconOptionDto.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Items/ItemsAdminReadRepository.cs`

**Docs (7A + audit)**

- `docs/admin-tools/items-builder/items-builder-icon-selector-plan.md`
- `docs/admin-tools/items-builder/README.md`
- `docs/roadmap/admin-tools-migration-master-plan.md`
- `docs/roadmap/admin-tools-migration-master-plan.html`
- `docs/admin-tools/migration/admin-tools-migration-risk-register.md`
- `docs/admin-tools/items-builder/items-builder-blazor-parity-audit.md`
- `docs/admin-tools/items-builder/items-builder-create-edit-gap-analysis.md`
- `docs/admin-tools/items-builder/items-builder-effects-editor-plan.md`

---

## Validation performed

| Check | Result | Notes |
| --- | --- | --- |
| `npm run build` (Admin Angular) | **PASS** | Warning: initial bundle ~6.3 kB over 500 kB budget |
| `dotnet build Sunshine.sln` | **PASS** | First attempt failed: `RollblackLegacy.Admin.Api` DLL locked (PID 64252); stop process and rebuild succeeded |
| Browser manual QA (`/admin/items/new`, `/admin/items/:id/edit`, icon-selector) | **NOT RUN** in last session | Next agent should run before claiming 7A sign-off in production-like env |
| DB writes for 7A | **N/A** | Read-only icon catalog + form field only |

---

## Known gaps (why Phase 7 remains PAUSED)

Create/Edit is **basic CRUD only**. Missing vs legacy Blazor:

1. **Effects/characteristics editor** — critical; no write payload for `items.Effects`.
2. **AP/PM/stats via effects** — not editable in Angular write form.
3. **Advanced template fields** — legacy had `APCost`, range, crit, etc. on `items_templates`; Sunshine write path is narrower.
4. **Description / IsVisible** — contract fields exist; API warns they are not persisted to client i18n / no DB column.
5. **Phase 7 write effects behavior today:**
   - Create: `Effects = "0000"` hardcoded in `ItemsAdminWriteRepository`.
   - Update: does **not** update `Effects` column.
   - Duplicate: copies source `Effects` hex only.
   - Read: `AdminProtocolCatalog.DecodeItemEffects` for detail display only.

---

## Phase 7B.0 — Effects Serialization Audit (required first)

**Goal:** Document how Sunshine stores item effects — no implementation, no DB writes, no invented format.

### Must answer (from parity audit + code)

| Question | Known pointers |
| --- | --- |
| Where are effects stored? | `sunshine.items.Effects` (hex string in current Admin); legacy `items_templates.BinaryEffects` (blob) |
| Column type / format | Hex in Sunshine; decode in `AdminProtocolCatalog.DecodeItemEffects` |
| Legacy serialize/deserialize | `GameEffectEditorService` → `EffectManager.SerializeEffects` / `DeserializeEffects` (Blazor repo) |
| Effect type IDs in decode | Cases `70` (int), `73` (dice), `82` (min/max), `71`, `74`, `76` in `AdminProtocolCatalog.cs` |
| AP/PM/stats representation | Effect `actionId` rows in payload; separate `APCost` column in legacy template (verify Sunshine schema) |
| Enum labels | `Sunshine.Protocol/Enums/EffectsEnum.cs` via `AdminProtocolCatalog` |

### Files to read first (official repo)

- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Items/AdminProtocolCatalog.cs` (`DecodeItemEffects`)
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Items/ItemsAdminWriteRepository.cs` (`EmptyEffectsHex`, insert/update SQL)
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Items/ItemsAdminReadRepository.cs` (select `Effects`)
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Items/ItemEffectDto.cs`
- `docs/admin-tools/items-builder/items-builder-blazor-parity-audit.md`
- `docs/admin-tools/items-builder/items-builder-effects-editor-plan.md`

### Reference only (Blazor)

- `Rollback.Admin/Services/GameEffectEditorService.cs`
- `Rollback.Admin/Services/ItemAdminService.cs` (`BinaryEffects`)
- `Rollback.Web/Components/Admin/EffectListEditor.razor`

### Deliverable for 7B.0

Update or add doc under:

```txt
docs/admin-tools/items-builder/
```

Suggested name: `items-builder-effects-serialization-audit.md` (or extend effects-editor-plan with audit section).

---

## Remaining tasks (checklist)

### Phase 7B.0 (audit only)

- [ ] Confirm `items.Effects` column type in Sunshine schema (hex length, null, empty = `0000`?)
- [ ] Document full binary/hex layout (count + per-effect records) with examples from real rows (sample 3–5 items, **not** 44k scan)
- [ ] Map `EffectsEnum` IDs for Vitality, Wisdom, AP, PM, stats, resistances (grep enum file, document IDs only)
- [ ] Compare Blazor `EffectManager` path vs `AdminProtocolCatalog` decode — list divergences
- [ ] Document encode path gap: read exists, write missing
- [ ] Update roadmap risk register if new serialization risks found

### Phase 7B (implementation — after 7B.0 approved)

- [ ] `ItemEffectWriteRow` contracts + extend `ItemCreateRequest` / `ItemUpdateRequest`
- [ ] `SunshineItemEffectsCodec` (encode/decode round-trip) in Infrastructure
- [ ] Wire `ItemsAdminWriteRepository` create/update/duplicate to persist effects
- [ ] Angular `EffectsEditor` component in `item-write-page`
- [ ] Unit tests for codec round-trip
- [ ] `npm run build` + `dotnet build`
- [ ] Manual QA: create/edit item with +Vitality / AP-style effects

### Phase 7C / 8 (later)

- [ ] Conditions UX polish (keep raw string)
- [ ] Numeric formatting (price/weight display)
- [ ] Publish/QA workflow (Phase 8 still PENDING)

---

## Risks / blockers

| Risk | Mitigation |
| --- | --- |
| **R28** — Claiming Phase 7 complete without effects editor | Keep Phase 7 `PAUSED / PARTIAL` until 7B done |
| **R22** — `MAX(Id)+1` on `items` (MyISAM) | No ad-hoc create smoke on shared DB without cleanup |
| **R30 / R31** — Icon selector false confidence | 7A done; browser QA still recommended |
| Sunshine vs Rollback effect format mismatch | 7B.0 audit must resolve before encoder implementation |
| `RollblackLegacy.Admin.Api` file lock during build | Stop running API process before `dotnet build` |
| Weapon types | Blocked in write service; do not expand to weapons in 7B |
| Secrets in `config/Database*.xml` | Never commit |

---

## Next recommended action

**Single next step:**

Perform **Phase 7B.0 Effects Serialization Audit** — read `AdminProtocolCatalog.DecodeItemEffects`, `ItemsAdminWriteRepository`, and legacy `GameEffectEditorService`, then write `docs/admin-tools/items-builder/items-builder-effects-serialization-audit.md` with concrete hex layout and effect ID mapping. **Do not implement Angular/API write for effects until audit doc is reviewed.**

---

## Things explicitly forbidden

```txt
- External worktrees or parallel repos
- Auditing weapons module
- Scanning 44k item records
- D2P/SWF extraction or client asset mass copy
- Gameplay changes
- Client file writes
- DB writes outside controlled item form QA (and none during 7B.0 audit)
- Implementing full Phase 7B UI/API before 7B.0 audit doc exists
- Copying entire Blazor folders into repo
- Committing config/Database*.xml, Client2.3.7 binaries, or secrets
- Marking Phase 7 Create/Edit as DONE before effects editor ships
- Auto-syncing IconId → AppearanceId
```

---

## Key doc index

| Doc | Purpose |
| --- | --- |
| `docs/admin-tools/items-builder/items-builder-blazor-parity-audit.md` | Blazor vs Angular gaps |
| `docs/admin-tools/items-builder/items-builder-create-edit-gap-analysis.md` | Feature matrix |
| `docs/admin-tools/items-builder/items-builder-effects-editor-plan.md` | 7B implementation plan |
| `docs/admin-tools/items-builder/items-builder-icon-selector-plan.md` | 7A DONE |
| `docs/roadmap/admin-tools-migration-master-plan.md` | Master roadmap |
| `docs/admin-tools/migration/admin-tools-migration-risk-register.md` | Risks R28–R31 |

---

## Handoff rule for next agent

1. Read this file completely.
2. Run `git status`, `git branch --show-current`, `git log --oneline -5`.
3. Do **not** touch unrelated dirty/untracked files.
4. Execute **Phase 7B.0 audit only** unless user explicitly approves implementation.
5. Update this `AGENT_HANDOFF.md` when phase status changes.
