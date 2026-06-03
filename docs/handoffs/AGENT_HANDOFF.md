# Agent Handoff - Admin Tools Migration / Items Builder

Generated: `2026-06-03`

Read this file before starting any implementation work.

## Mandatory handoff rule

Do not continue implementing if this handoff was not produced or is clearly outdated.

The next agent must:

1. read the latest handoff first
2. confirm repo, branch, phase, and last commit
3. only then continue implementation

If the current agent is getting close to the paid token/rate limit threshold, stop before the last stretch and update this file first.

Working rule for future agents:

- when remaining budget feels low, around the last `15%`, stop implementation
- update `docs/handoffs/AGENT_HANDOFF.md`
- record exact state, validations, risks, and next action
- only then end the turn

## Repository

Official repo only:

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
```

No external worktrees.
No parallel repos.
No implementation outside the official repo.

## Current branch

```txt
feature/items-builder-vps-qa-stabilization
```

## Real Admin stack

```txt
Angular-tools/Admin/
```

Do not use `src/Admin/`.

Canonical paths:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular
Angular-tools/Admin/RollblackLegacy.Admin.Api
Angular-tools/Admin/RollblackLegacy.Admin.Application
Angular-tools/Admin/RollblackLegacy.Admin.Contracts
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure
Angular-tools/Admin/RollblackLegacy.Admin.Domain
```

## Current roadmap snapshot

Items Builder:

```txt
Phase 1-6: DONE
Phase 6.5A: DONE
Phase 7A: DONE
Phase 7B: DONE
Phase 7C: NEXT / AUTHORIZED
Phase 7D: DOCS DONE
Phase 8: PENDING
```

Additional lane:

```txt
Future client publication pipeline: ANALYSIS COMPLETE / IMPLEMENTATION PENDING
```

## Latest relevant commits

```txt
a9fed9e docs: plan item client sprite preview extraction
7a4ed12 docs: define client publication pipeline for custom items
32c0b71 feat: enable visible dofus tester vendor fallback
cc02466 docs: audit dofus tester visibility and restart safety
```

## Current status confirmed

### Legacy reference

Imported and documented:

```txt
legacy-reference/Rollback.Web/
legacy-reference/Rollback.Admin/
```

Reference only. Do not treat as deployable code.

### Stabilization gate

Closed and validated:

```txt
5366a9b docs: record items builder stabilization gate before phase 7c
```

Confirmed during this arc:

```txt
dotnet build Sunshine.sln: OK
npm run build Angular: OK
Admin API -> VPS DB: isRemote=true
items/effects endpoints: valid JSON
```

### Dofus Tester visibility

Confirmed distinction:

```txt
7754 = client-known visible fallback
12617 = server-side custom template, still not client-visible
```

Important facts:

```txt
ItemId = objectGID/template identity the client must know
IconId = inventory icon / basic preview identity
AppearanceId = equipped look identity
sunshine.items does not currently expose ClientNameId as a DB column
```

## Phase 7D documentary conclusion

Closed in:

```txt
a9fed9e docs: plan item client sprite preview extraction
```

Key conclusions:

```txt
The current client is D2O/D2I/D2P based.
JPEXS / FFDec is useful for legacy SWF research, not as the primary current-client pipeline.
7754 / Dofus Ocre is the safe control case.
12617 / Dofus Tester remains invisible until client publication exists.
AppearanceId = 458 is not verified and must not be treated as truth.
```

## Exact current phase

We are here:

```txt
Macro 1 - Items Builder
Phase 7C - Form UX polish
```

Phase 7C is authorized because:

```txt
Phase 7A: DONE
Phase 7B: DONE
Stabilization gate: PASSED
VPS DB target: isRemote=true
7D docs are already closed
```

## Phase 7C implementation target

Goal:

```txt
polish the existing Create/Edit UX without reopening client publish or sprite extraction work
```

Expected Angular files:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-write-page.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-effects-editor.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/shared/components/api-problem-panel.component.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.models.ts
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.api.ts
```

Expected changes:

```txt
1. Unified success/error alerts or toasts
2. Cleaner 409/422/traceId UX
3. Split layout:
   left = runtime form
   right = preview / identity / warnings
   bottom = effects editor
4. Conditions as free textarea with hints
5. Preview warnings before save
6. Better numeric formatting for price / weight / level / values
```

Do not touch in 7C:

```txt
client publish
sprite extraction implementation
weapons
gameplay
mass asset import
Client2.3.7 tracked payloads
```

Expected commit:

```txt
feat: polish items write form ux
```

## Next macro after 7C

Do not jump there yet, but keep it explicit:

### Macro 2 - Client Data Audit Tools

Planned next documentary/technical lane:

```txt
read-only tooling for:
Items.d2o
Appearances.d2o
ItemTypes.d2o
ItemSets.d2o
i18n_es.d2i
i18n_en.d2i
```

Goal:

```txt
answer if the client knows a template, a name, and an appearance before claiming visibility
```

## Files recently added or updated that matter now

Client publication / visibility:

```txt
docs/admin-tools/items-builder/items-builder-client-publication-analysis.md
docs/admin-tools/items-builder/item-publication-pipeline.md
docs/admin-tools/items-builder/visible-item-checklist.md
docs/admin-tools/items-builder/qa-vendor-test-checklist.md
docs/infrastructure/vps-restart-safety-checklist.md
```

Phase 7D docs:

```txt
docs/admin-tools/items-builder/items-client-sprite-preview-extraction-plan.md
docs/admin-tools/items-builder/items-client-appearance-mapping-audit.md
docs/admin-tools/items-builder/items-client-jpexs-ffdec-notes.md
```

Master roadmap / indexes:

```txt
docs/admin-tools/items-builder/README.md
docs/roadmap/admin-tools-migration-master-plan.md
docs/roadmap/admin-tools-migration-master-plan.html
docs/admin-tools/migration/admin-tools-migration-risk-register.md
```

## Validation baseline

Latest validated outcomes in this branch:

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" -> OK
npm run build in Angular-tools/Admin/RollblackLegacy.Admin.Angular -> OK
```

For the next implementation step, re-run:

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"
cd Angular-tools/Admin/RollblackLegacy.Admin.Angular
npm run build
```

Recommended browser QA after 7C:

```txt
/admin/items/12616/edit
/admin/items/39/edit
/admin/items/new
```

Check:

```txt
preview
warnings
effects editor still works
traceId remains visible on error
```

## Dirty files that are not yours

Do not revert or stage:

```txt
Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/WorldServerManager.cs
```

Also leave local untracked files alone:

```txt
Client2.3.7/cliente.rar
Client2.3.7/cliente/
Client2.3.7/version
config/Database.local.xml
config/Database.runtime.backup.xml
config/Database.team.xml
```

## Absolute prohibitions

```txt
create external worktrees
create parallel repos
use src/Admin when the real stack is Angular-tools/Admin
audit weapons
scan 44k records
touch client files without an approved phase
touch gameplay
run mass D2P/SWF extraction
write to production without backup
commit secrets
copy bin/obj/node_modules/dist/logs/artifacts
start a phase outside the roadmap without asking
```

## Exact next action

If you are the next agent, do this first:

```txt
1. Read this handoff completely.
2. Confirm branch = feature/items-builder-vps-qa-stabilization.
3. Confirm last commit = a9fed9e or newer.
4. Start only Phase 7C.
```

Then:

```txt
implement the write-form UX polish slice
validate dotnet build + npm run build
update this handoff again if the turn gets close to the final 15% of budget
```
