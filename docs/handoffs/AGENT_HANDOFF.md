# Agent Handoff - Admin Tools Migration / Client Identity Audit Tool

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
feature/client-identity-audit-tool-phase1
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
Macro 2 - Client Identity Audit Tool: IN_PROGRESS (Phase 1 DONE)
Macro 3 - Sprite Preview Pipeline: PENDING
Macro 4 - Spells Builder: DEFERRED
Macro 5 - Glyph Builder: DEFERRED
Macro 6 - Maps Builder: DEFERRED
```

## Latest relevant commits

```txt
c606976 feat: add client identity audit runtime scaffold
d122434 feat: add client identity audit tool scaffold
944ff0c docs: update agent handoff
8ffdfa6 docs: complete phase 8 publication workflow
00fdfbf feat: add item publication diagnostics
```

## Exact current phase

We are here:

```txt
Macro 2 - Client Identity Audit Tool
Phase 1 - plan + scaffold read-only
Status: CLOSED
```

## What Phase 1 delivered

Closed scope:

```txt
1. read-only scaffold under infrastructure/scripts/ClientIdentityAudit
2. DB lookup from sunshine.items
3. D2O lookup for Items, ItemTypes, ItemSets, Appearances
4. D2I lookup for i18n_es and i18n_en
5. control-case report for 7754, 12616, 12617, 39
6. roadmap and docs for Macro 2 updated
```

## Tool delivered

Path:

```txt
infrastructure/scripts/ClientIdentityAudit
```

Files:

```txt
infrastructure/scripts/ClientIdentityAudit/.gitignore
infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj
infrastructure/scripts/ClientIdentityAudit/Program.cs
```

Solution integration:

```txt
Sunshine net11.0/Sunshine net11.0/Sunshine.sln
```

## Phase 1 validation performed

Builds:

```txt
dotnet build "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -> OK
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" -> OK
```

Tool run validated:

```txt
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md"
```

## Control-case outcomes

```txt
7754  -> CLIENT_KNOWN / SAFE_EXISTING_TEMPLATE
12616 -> CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH / APPEARANCE_UNKNOWN
12617 -> CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH
39    -> CLIENT_KNOWN / SAFE_EXISTING_TEMPLATE / preview by IconId=1001
```

Important validated facts:

```txt
7754 exists in Items.d2o
12616 does not exist in Items.d2o
12617 does not exist in Items.d2o
DescriptionId 50090 resolves in ES and EN
DescriptionId 50091 resolves in ES and EN
IconId alone still does not publish a template
```

## D2I format note

Important discovery for future work:

```txt
The current client's i18n_es.d2i and i18n_en.d2i use a simple index map:
id -> offset
```

This Phase 1 scaffold does not mutate D2I.
It only reads string offsets in read-only mode.

## Current documentary baseline

New or updated docs that matter now:

```txt
docs/admin-tools/client-identity/README.md
docs/admin-tools/client-identity/client-identity-audit-tool-phase1.md
docs/admin-tools/client-identity/client-identity-source-map.md
docs/admin-tools/client-identity/client-identity-item-check-report.md
docs/roadmap/admin-tools-migration-master-plan.md
docs/roadmap/admin-tools-migration-master-plan.html
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
touch client files in write mode
modify d2o/d2i/d2p
audit weapons
scan 44k records
touch gameplay
write to production without backup
commit secrets
copy bin/obj/node_modules/dist/logs/artifacts
start Macro 3 before Macro 2 is closed
```

## Exact next action

If you are the next agent, do this first:

```txt
1. Read this handoff completely.
2. Confirm branch = feature/client-identity-audit-tool-phase1.
3. Confirm last commit = c606976 or newer.
4. Start Macro 2 Phase 2 only.
```

Then:

```txt
promote the read-only scaffold into a reusable service layer
keep it read-only
do not add UI yet unless explicitly requested
extend audit depth without touching client files in write mode
update this handoff again if the turn gets close to the final 15% of budget
```
