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
Phase 1: DONE
Phase 1.5: DONE
Phase 2: DONE
Phase 3: DONE
Phase 4: DONE
Phase 5: DONE
Phase 6: DONE
Phase 6.5A: DONE
Phase 7A: DONE
Phase 7B: DONE
Phase 7C: DONE
Phase 7D: DOCS DONE
Phase 8: NEXT
```

Additional macros:

```txt
Macro 2 - Client Identity Audit Tool: PENDING (do not start before Phase 8)
Macro 3 - Sprite Preview Pipeline: PENDING (do not start before Phase 8)
Macro 4 - Spells Builder: DEFERRED
Macro 5 - Glyph Builder: DEFERRED
Macro 6 - Maps Builder: DEFERRED
```

## Latest relevant commits

```txt
276774c fix: remove duplicate effects editor from item layout
f7a5820 feat: add numeric formatting to item editor
13bcd1c feat: add item preview warnings
208e7b2 feat: improve item conditions editor
41a22f1 feat: add split layout to item editor
106f13d feat: unify items builder notifications
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
Phase 8 - Publish / QA
```

Phase 7C is now closed because:

```txt
Unified success/error panel shipped
Split write layout shipped
Conditions UX shipped
Preview warnings shipped
Numeric formatting shipped
Builds stayed green
Browser QA on item 12616 passed
```

## Phase 7C result snapshot

Closed scope:

```txt
1. Unified success/error feedback for save and validation flows
2. Split editor layout:
   left = runtime form
   right = preview / identity / warnings
   bottom = effects editor
3. Conditions textarea with contextual help
4. Pre-save preview warnings
5. Human numeric formatting for price / weight / level / stats
```

Main Angular files touched:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-write-page.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-effects-editor.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/shared/components/api-problem-panel.component.*
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.models.ts
```

Validation performed:

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" -> OK
npm run build in Angular-tools/Admin/RollblackLegacy.Admin.Angular -> OK
/admin/items/12616/edit -> load OK
/admin/items/12616/edit -> save OK
/admin/items/12616/edit -> reload OK
/admin/items/12616/edit -> preview visible
/admin/items/12616/edit -> effects visible once after duplicate-layout fix
/admin/items/12616/edit -> forcing weapon TypeId returns 422 with visible traceId
```

Important validation limit kept on purpose:

```txt
409 and 500 now use the same unified panel path, but they were not fault-injected against the current VPS-backed environment.
We did not manufacture DB conflicts or backend outages just to force those statuses during Phase 7C.
```

Do not touch next:

```txt
client publish
sprite extraction implementation
weapons
gameplay
mass asset import
Client2.3.7 tracked payloads
```

## Exact next phase after 7C

This is now the exact next action:

### Phase 8 - Publish / QA

Goal:

```txt
guarantee that what is created in DB
exists in client
is visible
is deliverable
is equippable
```

After Phase 8 is closed:

```txt
Macro 2 - Client Identity Audit Tool
Macro 3 - Sprite Preview Pipeline
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

Recommended browser QA when Phase 8 starts:

```txt
/admin/items/12616
/admin/items/12616/edit
/admin/items/39
/admin/items/new
```

Check:

```txt
client visibility readiness
qa-summary panel
write flow still green after Phase 7C
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
3. Confirm last commit = cc94a1f or newer.
4. Start only Phase 7C.
```

Then:

```txt
implement the write-form UX polish slice
validate dotnet build + npm run build
do not start Macro 2 or Macro 3 before Phase 8 is closed
update this handoff again if the turn gets close to the final 15% of budget
```
