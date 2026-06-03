# Legacy reference — Rollback Admin (Blazor)

Controlled functional reference copied from `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\` into the official repo.

**Do not run or deploy this tree.** It is read-only context for porting to `Angular-tools/Admin/` and `docs/admin-tools/`.

## Contents

| Path | Role | Files (approx.) |
| --- | --- | --- |
| `Rollback.Web/` | Blazor UI (pages, components, wwwroot) | 101 source/wwwroot files |
| `Rollback.Admin/` | Business logic used by the Web host (items, effects, publish) | 139 `.cs` + models |

Excluded on copy: `bin/`, `obj/`, `.vs/`, `logs/`, `artifacts/`, `.tmp/`, `*.user`, `*.suo`, `*.cache`.

## PNG / static assets (curated, not a bulk dump)

| Location | Count | Purpose |
| --- | ---: | --- |
| `Rollback.Web/wwwroot/admin-assets/items/` | 33 | Operator manual item preview uploads from legacy admin |
| `Rollback.Web/wwwroot/assest-img/` | 4 | Portal branding / hero (not item catalog) |
| `Rollback.Web/wwwroot/css/` + open-iconic | 8 | Theme and icon font (reference only) |

Item icon catalogs from the game client are **not** copied here; Sunshine Admin uses `Angular-tools/Admin` asset pipeline docs instead.

## Broken references (expected)

`Rollback.Web.csproj` still references sibling projects (`Rollback.Admin`, `Rollback.Accounts`, `Rollback.Protocol`, `Rollback.World`) that are **not** fully vendored in this folder. Only `Rollback.Admin` is copied as a companion. Build is intentionally not required for this reference tree.

## Official port targets

```txt
Rollback.Web + Rollback.Admin (legacy)  →  RollblackLegacy.Admin.Application
                                         →  RollblackLegacy.Admin.Infrastructure
                                         →  RollblackLegacy.Admin.Contracts
                                         →  RollblackLegacy.Admin.Api
                                         →  RollblackLegacy.Admin.Angular (views only)
```

## Docs

- [rollback-web-functional-inventory.md](../docs/admin-tools/items-builder/rollback-web-functional-inventory.md)
- [blazor-to-angular-port-plan.md](../docs/admin-tools/items-builder/blazor-to-angular-port-plan.md)
- [items-functional-port-map.md](../docs/admin-tools/items-builder/items-functional-port-map.md)
