# Agent Handoff - Admin Tools Migration / Items Builder

Generated: `2026-06-03`

Read this file before starting any implementation work.

## Mandatory handoff rule

Do not continue implementing if this handoff was not produced or is clearly outdated.

The next agent must:

1. read the latest handoff first
2. confirm repo, branch, phase, and last commit
3. only then continue implementation

If the current agent is getting close to the paid token or rate-limit threshold, stop before the last stretch and update this file first.

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

Canonical paths:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular
Angular-tools/Admin/RollblackLegacy.Admin.Api
Angular-tools/Admin/RollblackLegacy.Admin.Application
Angular-tools/Admin/RollblackLegacy.Admin.Contracts
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure
Angular-tools/Admin/RollblackLegacy.Admin.Domain
```

Do not use `src/Admin/`.

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
Phase 8: DONE
```

Additional macros:

```txt
Macro 2 - Client Identity Audit Tool: NEXT
Macro 3 - Sprite Preview Pipeline: PENDING
Macro 4 - Spells Builder: DEFERRED
Macro 5 - Glyph Builder: DEFERRED
Macro 6 - Maps Builder: DEFERRED
```

## Latest relevant commits

```txt
8ffdfa6 docs: complete phase 8 publication workflow
00fdfbf feat: add item publication diagnostics
37c17b1 docs: update agent handoff
276774c fix: remove duplicate effects editor from item layout
f7a5820 feat: add numeric formatting to item editor
13bcd1c feat: add item preview warnings
208e7b2 feat: improve item conditions editor
41a22f1 feat: add split layout to item editor
106f13d feat: unify items builder notifications
```

## Exact current phase

We are here:

```txt
Macro 1 - Items Builder
Phase 8 - Publish / QA
Status: CLOSED
```

What Phase 8 closed:

```txt
1. QA summary already existed and remains in place
2. Item Publication Status was added as a read-only client visibility diagnostic
3. Publication matrix for 7754 / 12616 / 12617 was documented
4. Publish decision workflow was documented
5. Admin command catalog was documented
6. Vendor publication workflow was documented
7. Production QA checklist was documented
```

## Phase 8 implementation snapshot

### Backend

Added:

```txt
GET /api/admin/v1/items/{itemId}/publication-status
```

Main files:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/ItemsAdminController.cs
Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Items/IItemClientPublicationInspector.cs
Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/Items/IItemsAdminReadService.cs
Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/Items/ItemClientPublicationAuditResult.cs
Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/ItemsAdminReadService.cs
Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Items/ItemPublicationStatusDto.cs
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Configuration/AdminClientPublicationOptions.cs
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/DependencyInjection/AdminInfrastructureServiceCollectionExtensions.cs
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Items/FileSystemItemClientPublicationInspector.cs
```

Behavior:

- reads `Client2.3.7/data/common/Items.d2o` in read-only mode
- checks whether the exact `ItemId` exists in client metadata
- classifies:
  - `CLIENT_KNOWN`
  - `CLIENT_UNKNOWN`
  - `CLIENT_DATA_UNAVAILABLE`
- derives:
  - `PUBLISHED`
  - `NEEDS_CLIENT_PATCH`
  - `UNVERIFIED`
- exposes visibility result:
  - `VISIBLE`
  - `VISIBLE_WITH_PATCH`
  - `INVISIBLE`

### Angular

Added route:

```txt
/admin/items/:itemId/publication-status
```

Main files:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.api.ts
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.facade.ts
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/data-access/items.models.ts
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-detail-page.component.html
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-publication-status-page.component.ts
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-publication-status-page.component.html
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-publication-status-page.component.scss
Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/app.routes.ts
```

UI outcome:

- dedicated `Item Publication Status` screen
- visible classification for `Client Known`, `Client Unknown`, `Published`, `Needs Client Patch`, `Needs Asset`, `Needs QA`
- direct reasons and recommended actions

## Validation performed

Builds:

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" -> OK
npm run build in Angular-tools/Admin/RollblackLegacy.Admin.Angular -> OK
```

Runtime/API validation:

```txt
GET /api/admin/v1/health/db -> host=174.138.35.107, port=3306, user=sunshine_remote, isRemote=true
GET /api/admin/v1/items/7754/publication-status -> VISIBLE / CLIENT_KNOWN / PUBLISHED
GET /api/admin/v1/items/12616/publication-status -> VISIBLE_WITH_PATCH / CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH
GET /api/admin/v1/items/12617/publication-status -> VISIBLE_WITH_PATCH / CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH
```

Read-only client evidence:

```txt
Client2.3.7/data/common/Items.d2o
7754   -> present
12616  -> missing
12617  -> missing
```

Browser validation completed:

```txt
/admin/items/7754/publication-status -> client-known visible status shown
/admin/items/12617/publication-status -> needs client patch shown
```

## Current documentary baseline

New or updated docs that matter now:

```txt
docs/admin-tools/items-builder/items-builder-publish-qa-phase8.md
docs/admin-tools/items-builder/items-client-visibility-matrix.md
docs/admin-tools/items-builder/items-publish-decision-workflow.md
docs/admin-tools/items-builder/items-admin-command-catalog.md
docs/admin-tools/items-builder/items-vendor-publication-workflow.md
docs/admin-tools/items-builder/items-production-qa-checklist.md
docs/admin-tools/items-builder/README.md
docs/roadmap/admin-tools-migration-master-plan.md
docs/roadmap/admin-tools-migration-master-plan.html
```

## Dofus Tester visibility distinction

Confirmed and should not be forgotten:

```txt
7754 = client-known visible fallback
12617 = server-side custom template, still not client-visible
```

Important facts:

```txt
ItemId = objectGID/template identity the client must know
IconId = inventory icon / basic preview identity
AppearanceId = equipped look identity
IconId alone does not publish a template
Vendor stock alone does not publish a template
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
3. Confirm last commit = 8ffdfa6 or newer.
4. Start Macro 2 only, not Macro 3.
```

Then:

```txt
implement the Client Identity Audit Tool as a read-only continuation of publication diagnostics
reuse the Phase 8 matrix and Items.d2o audit rules
keep client files read-only unless a separately approved client publication phase is opened
update this handoff again if the turn gets close to the final 15% of budget
```
