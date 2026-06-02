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

## Error 500 diagnostic

Fecha: `2026-06-02`  
Rama: `feature/items-builder-vps-qa-stabilization`  
Handoff previo commiteado: `98d106a docs: add agent handoff for items builder phase 7`

### Pantalla afectada

- Ruta por defecto de la app: `/admin/items` (redirección desde `/`)
- También visible en cualquier pantalla que use `app-api-problem-panel` cuando el backend responde error sin `ProblemDetails` útil

### Endpoints probados (Admin API en `http://localhost:5248`)

| Endpoint | Status | Notas |
| --- | --- | --- |
| `GET /api/admin/v1/health` | 200 | Servicio vivo |
| `GET /api/admin/v1/health/db` | 200 | Con `appsettings.Development.local.json` apunta a VPS `174.138.35.107` |
| `GET /api/admin/v1/items?page=1&pageSize=20` | 200 | Catálogo remoto operativo en esta máquina |
| `GET /api/admin/v1/items/39` | 200 | Detalle OK |
| `GET /api/admin/v1/items/39/identity` | 200 | Identidad OK |
| `GET /api/admin/v1/item-icons?page=1&pageSize=24` | 200 | Catálogo PNG OK |
| `GET /api/admin/v1/items/types/options` | 200 | Sin DB (enum protocolo) |
| `GET /api/admin/v1/item-sets/options` | 200 | Requiere DB |
| `GET /api/admin/v1/items/preview-state?iconId=1001` | 200 | Preview OK |
| `GET /api/admin/v1/items/39/qa-summary` | 200 | QA summary OK |

### Causa del mensaje duplicado «No se pudo completar la solicitud»

No era necesariamente dos fallos distintos. Había dos fuentes de duplicación en el cliente:

1. **Título y detalle genéricos iguales** en `toAdminApiProblem()` cuando la respuesta HTTP no traía `application/problem+json` parseable (cuerpo vacío, HTML, proxy, etc.).
2. **Dos paneles** en `items-page.component.html` (`lookupProblem` + `listProblem`) que podían mostrar el mismo error dos veces si fallaban catálogo y lookups.

El panel `api-problem-panel` además repetía un detalle por defecto aunque el título ya fuera genérico.

### Causas probables del HTTP 500 real

| Causa | Síntoma | Comprobación |
| --- | --- | --- |
| `SunshineAdmin` con password placeholder exacto `change-me` y `AllowDevelopmentPlaceholderConnectionString=false` | `GET /items` falla; `GET /health/db` responde `not_configured` | Revisar `appsettings.Development.local.json` |
| Admin API no levantado o proxy mal apuntado | Angular status `0`, no 500 | `ng serve` + `dotnet run` en puerto `5248` |
| MySQL remoto caído / firewall | 500 con título «No se pudo conectar con la base de datos Sunshine» | `GET /health/db` → `status: error` |
| Binarios Admin API desactualizados tras Phase 7A | 500 genérico en `item-icons` u otros | `dotnet build` completo y reiniciar API |

Config observada en esta máquina:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.Development.local.json
→ Server=174.138.35.107; User=sunshine_remote; Password=change-me-remote
```

`change-me-remote` **no** se trata como placeholder (solo `change-me` exacto). Si el VPS exige otra clave, actualizar ese archivo local (no commitear).

### Fix aplicado

Commit: `2b79283 fix: stabilize items builder error handling`

- **Angular:** `toAdminApiProblem()` evita duplicar título/detalle; lee `traceId` del cuerpo ProblemDetails; un solo panel en lista (`pageProblem`).
- **Angular:** `api-problem-panel` no muestra detalle si es igual al título.
- **Admin API:** `AdminApiExceptionHandler` registra excepción con `traceId`; `AdminNotConfiguredException` pasa a **HTTP 503**; en Development el `detail` incluye `exception.Message` para diagnóstico.

### Validación post-fix

- `dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"`
- `npm run build` en `Angular-tools/Admin/RollblackLegacy.Admin.Angular`
- Navegador: `/admin/items` debe mostrar un solo mensaje de error claro (con `traceId` si el API responde ProblemDetails) o catálogo si API+DB están OK

### Phase 7B.0

No iniciada. Este bloque solo cubre estabilización del error 500 / UX de errores.

## Related docs

- [items-builder-options-loading-fix.md](./items-builder-options-loading-fix.md)
- [items-builder-png-import-plan.md](./items-builder-png-import-plan.md)
- [items-builder-phase7a-item-icon-selector.md](./items-builder-phase7a-item-icon-selector.md)
- [vps-world-restart-flow.md](../../infrastructure/vps-world-restart-flow.md)
