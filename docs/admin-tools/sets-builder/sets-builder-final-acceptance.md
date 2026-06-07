# Sets Builder — aceptación final

**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**Base cherry-pick:** `feature/items-sets-production-acceptance-test`

## Alcance entregado en código

| Requisito | Estado |
| --- | --- |
| Listado `/admin/item-sets` con paginación | **OK** — `ItemSetSearchRequest` + filtros |
| Filtro nombre / nivel / cantidad partes | **OK** |
| Total count + empty/error + traceId | **OK** (API problem panel) |
| Crear set | **OK** — POST `/api/admin/v1/item-sets` |
| Editar set | **OK** — PUT |
| Borrar set | **OK** — DELETE |
| Agregar/quitar item por ItemId | **OK** — write page |
| Bonos por piezas (2–5 + completo) | **OK** — `item-set-bonus-editor` + catálogo effects |
| Recargar tras guardar | **OK** |

## Visibilidad cliente (set)

| Check | Estado |
| --- | --- |
| `ItemSets.d2o` en package si existe staging | **OK** — `PublicationPackagePatchFiles` |
| Publish automático sets | **NO** — igual que items |
| Set visible solo en DB | Documentado — no afirmar visible |

## Browser QA

| Ruta | Estado |
| --- | --- |
| `/admin/item-sets` | **PENDING_OPERATOR** |
| `/admin/item-sets/new` | **PENDING_OPERATOR** |
| `/admin/item-sets/:id/edit` | **PENDING_OPERATOR** |

## Builds (automático)

```bash
dotnet build Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj
npm run build
```

## Criterio de cierre

- [x] CRUD API + UI
- [x] Bonos con catálogo real
- [ ] Operador confirma listado no se queda cargando en VPS
- [ ] Operador valida set nuevo en juego tras publish items miembros
