# Items Builder Stabilization / VPS Data / Assets / UX

## Snapshot

- Date: `2026-06-02`
- Branch: `feature/items-builder-vps-qa-stabilization`
- Status: `DONE`
- Suggested commit: `fix: stabilize items builder live vps workflow`

## Scope

This phase stabilizes the existing Items Builder for real operator use without opening new feature lanes.

Included:

- local vs VPS data target audit
- safer `health/db` response
- Spanish operator-facing error messaging
- option-loading confirmation for item types and item sets
- controlled first-wave PNG import without weapons
- safe restart scripts and VPS restart flow documentation

Explicitly excluded:

- weapons and `44k` records
- SWF or D2P extraction
- mass asset import
- gameplay changes
- automatic VPS restart

## Data target conclusion

Current live wiring in the official repo:

- Angular calls `/api/admin/v1`
- Angular proxy targets `http://127.0.0.1:5248`
- Admin API runs locally at `http://localhost:5248`
- `SunshineAdmin` currently points to local `127.0.0.1:3306`
- current database: `sunshine`
- current user is resolved safely through `GET /api/admin/v1/health/db`
- `isRemote=false` in the current local setup

This means the operator is **not** talking to the VPS right now. The current working path is:

`Angular -> local Admin API -> local MySQL sunshine`

## Health endpoint hardening

`GET /api/admin/v1/health/db` now returns safe connection target fields:

```json
{
  "status": "ok",
  "database": "sunshine",
  "host": "127.0.0.1",
  "port": 3306,
  "user": "sunshine",
  "isRemote": false
}
```

The response never exposes the password.

## Catalog and options findings

Observed behavior during stabilization:

- `/admin/items` loads real data from Sunshine
- `/admin/items/new` loads item types correctly
- `/admin/items/new` loads item sets correctly when the table has rows
- `/admin/items/icon-selector` loads the curated PNG catalog

Important source split:

- `Choose Type` is **not** DB-backed
- type options are loaded from `Sunshine.Protocol/Enums/ItemTypeEnum.cs` through `AdminProtocolCatalog`
- `Item Set` **is** DB-backed
- item sets come from `sunshine.items_sets`

## HTTP 200 / empty-catalog diagnosis

The previously reported "HTTP 200 but request could not be complete" state was not reproducible during this stabilization pass.

What changed anyway:

- parse-failure messaging is now human and in Spanish
- network failure messaging is now human and in Spanish
- shared problem panels keep `traceId` visible
- multiple Angular screens now have clearer empty-state language

## Spanish UX pass

Updated areas:

- list page
- item detail
- item write flow
- icon selector
- preview card
- diagnostics panel
- QA readiness panel
- shared API problem panel
- clipboard fallback labels

Operator goals:

- understand whether the problem is network, backend, or empty data
- copy `traceId` when support is needed
- avoid typing `IconId` blind

## VPS reality check

Expected historical SSH target:

- host: `174.138.35.107`
- user: `root`
- hostname: `RollBlackLegacy`

Current machine result during this phase:

```txt
Permission denied (publickey)
```

Conclusion:

- VPS runtime audit is currently blocked on this machine
- no restart was executed
- restart scripts are documented and prepared, but remain unvalidated against the live VPS until SSH access is restored

## Validation summary

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`: required for this phase
- `npm run build` under `Angular-tools/Admin/RollblackLegacy.Admin.Angular`: required for this phase
- `GET /api/admin/v1/health/db`: confirms local target safely
- `GET /api/admin/v1/items?page=1&pageSize=20`: catalog works
- `GET /api/admin/v1/items/types/options`: works
- `GET /api/admin/v1/item-sets/options`: works
- `GET /api/admin/v1/item-icons?page=1&pageSize=24`: works

Browser targets used in this phase:

- `/admin/items`
- `/admin/items/new`
- `/admin/items/39/edit`
- `/admin/items/icon-selector`

## Related docs

- [items-builder-options-loading-fix.md](./items-builder-options-loading-fix.md)
- [items-builder-png-import-plan.md](./items-builder-png-import-plan.md)
- [items-builder-phase7a-item-icon-selector.md](./items-builder-phase7a-item-icon-selector.md)
- [vps-world-restart-flow.md](../../infrastructure/vps-world-restart-flow.md)
