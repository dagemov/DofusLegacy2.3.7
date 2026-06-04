# Admin API VPS Database Connection

## Snapshot

- Date: `2026-06-02`
- Scope: Admin API database target selection
- Official repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`

## Goal

Allow the team to switch the Admin API between:

- `LOCAL_DB`
- `VPS_DB`

without changing code and without committing secrets.

## Canonical config files

Tracked defaults:

- `Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.json`
- `Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.Development.example.json`
- `Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.Development.vps.example.json`

Ignored local override loaded by the API in Development:

- `Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.Development.local.json`

## Load order

`Program.cs` loads the local development override only in Development:

1. `appsettings.json`
2. environment-specific defaults
3. `appsettings.Development.local.json` if present

That means the local file is the correct place to switch the active DB target without touching tracked code.

## Profiles

### LOCAL_DB

Use the local development profile when the Admin API should read from local MySQL:

```json
{
  "ConnectionStrings": {
    "SunshineAdmin": "Server=127.0.0.1;Port=3306;Database=sunshine;User ID=sunshine;Password=your-local-secret;Allow User Variables=true;TreatTinyAsBoolean=true"
  }
}
```

Reference:

- `appsettings.Development.example.json`

### VPS_DB

Use the VPS development profile when the Admin API should read from the team VPS MySQL:

```json
{
  "ConnectionStrings": {
    "SunshineAdmin": "Server=174.138.35.107;Port=3306;Database=sunshine;User ID=sunshine_remote;Password=your-vps-secret;Allow User Variables=true;TreatTinyAsBoolean=true"
  }
}
```

Reference:

- `appsettings.Development.vps.example.json`

## Safe team workflow

1. Keep tracked files secret-free.
2. Copy the appropriate example into `appsettings.Development.local.json`.
3. Replace only the password locally.
4. Restart the Admin API.
5. Validate `GET /api/admin/v1/health/db`.

Expected local signal:

- `host=127.0.0.1`
- `isRemote=false`

Expected VPS signal:

- `host=174.138.35.107`
- `isRemote=true`

## Git safety

Ignored by `.gitignore`:

- `appsettings.Development.local.json`
- `appsettings.Development.local.backup.json`
- `appsettings.Development.vps.local.json`

Rule:

- never commit real passwords
- never paste the live password into docs, screenshots, or PR bodies

## Validation checklist

Backend:

- `GET /api/admin/v1/health/db`
- `GET /api/admin/v1/items?page=1&pageSize=20`

Frontend:

- `/admin/items`

## Important note

Switching the Admin API database target does **not** change gameplay by itself. It only changes which MySQL source the Admin API reads from.
