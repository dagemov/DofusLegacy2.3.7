# Macro 3 / Phase 7 — Visual Preview Final QA + Macro Closure

## Estado

| Campo | Valor |
| --- | --- |
| Fase | Macro 3 / Phase 7 |
| Macro 3 global | **COMPLETE** |
| Fecha | `2026-06-03` |
| Rama | `feature/item-sprite-preview-final-qa-phase7` |
| EntityLook renderer | **DEFERRED** — no requerido para Items Builder MVP |

## Objetivo

Validar coherencia UX y contratos API de las cuatro superficies operativas:

| Superficie | Significado |
| --- | --- |
| **Icon Preview** | Inventario / bolsa — resuelve por `IconId` (`by-icon/`) |
| **Appearance Preview** | Look equipado — skin por `AppearanceId` (`by-appearance/`) |
| **Client Identity** | ¿El cliente conoce el `ItemId` en `Items.d2o`? |
| **Publication Status** | Visibilidad operativa + patch cliente + assets |

## UX review (labels)

Pantallas revisadas en Angular Admin:

| Ruta | Cambio Phase 7 |
| --- | --- |
| Detalle item | Copy de cuatro superficies; botón «Estado de publicación» |
| Icon Preview card | «Preview de inventario / Icon Preview» |
| Appearance Preview card | «Preview de apariencia equipada / Appearance Preview» |
| Client Identity card | «Identidad cliente / Client Identity» |
| Publication Status | Título ES + separación explícita vs previews |
| Icon selector | Aclara que no toca `AppearanceId` |
| QA readiness | Badges `Icon preview` + `Appearance preview` |

Sin renderer, sin mensajes que equiparen `IconId` con look equipado.

## Smoke tests API

Ejecutado vía servicios Admin (equivalente a endpoints HTTP documentados), entorno:

```txt
Repo: DofusLegacy2.3.7
Config: appsettings.Development.local.json
Client: Client2.3.7 presente
Fecha: 2026-06-03
```

Endpoints validados (mapeo 1:1):

| Endpoint lógico | Servicio |
| --- | --- |
| `GET /api/admin/v1/items/{id}` | `IItemsAdminReadService.GetItemAsync` |
| `GET /api/admin/v1/items/{id}/publication-status` | `GetPublicationStatusAsync` |
| `GET /api/admin/v1/items/appearance-preview-state` | `ResolveAppearancePreviewStateAsync` |
| `GET /api/admin/v1/client-identity/items/{id}` | `IClientItemIdentityReadService.GetItemAsync` |

### Tabla QA

| Route | Item | Expected Icon Preview | Expected Appearance Preview | Expected Client Identity | Expected Publication Status | Actual Result (API smoke) | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `/admin/items/7754` | Dofus Ocre | `FOUND` (23012 curado) | `NOT_APPLICABLE` (AppearanceId=0) | `CLIENT_KNOWN` / SAFE | `VISIBLE` + `PUBLISHED` | icon=FOUND, app=N/A, known=True | **PASS** |
| `/admin/items/7754/publication-status` | Dofus Ocre | — | — | ClientKnown=True | VISIBLE / PUBLISHED | igual smoke publication | **PASS** |
| `/admin/items/12616` | ADMIN TEST | `FOUND` (1003 curado) | `UNKNOWN` (1004 no en Appearances.d2o) | `CLIENT_UNKNOWN` + APPEARANCE_UNKNOWN | `NEEDS_CLIENT_PATCH` | icon=FOUND, app=UNKNOWN, known=False | **PASS** |
| `/admin/items/12616/edit` | ADMIN TEST | Form refresh IconId | Form UNKNOWN + warning | — | — | mismos estados que detalle | **PASS** |
| `/admin/items/12617/publication-status` | Dofus Tester | `FOUND` (23012) | `NOT_APPLICABLE` | `CLIENT_UNKNOWN` | `VISIBLE_WITH_PATCH` | icon=FOUND, known=False | **PASS** |
| `/admin/items/icon-selector` | — | Catálogo `IconId` | No aplica | No aplica | No aplica | UX labels; sin API item | **PASS** |
| `/admin/items/39` | Petite Amulette | `FOUND` (1001) | `NOT_APPLICABLE` | `CLIENT_KNOWN` | `VISIBLE` / `PUBLISHED` | icon=FOUND, known=True | **PASS** |

### Casos destacados

**7754 — Dofus Ocre**

- Icon preview real (`by-icon/23012.png`).
- Sin appearance equipada (0).
- Template conocido por cliente.

**12616 — ADMIN TEST**

- Icon preview OK.
- `AppearanceId=1004` → `AppearanceKnown=false` → estado `UNKNOWN`.
- Template `12616` ausente en `Items.d2o` → publication `NEEDS_CLIENT_PATCH`.

**12617 — Dofus Tester**

- Comparte `IconId` con 7754 pero **no** es visible: `CLIENT_UNKNOWN`.
- Demuestra regla: `IconId` ≠ visibilidad cliente.

**39 — control vanilla**

- Item cliente conocido, icon 1001 curado, appearance 0.

### Appearance preview endpoint

```txt
GET appearance-preview-state?appearanceId=1004&appearanceKnown=false
→ state=UNKNOWN, path=/assets/item-previews/by-appearance/1004.png
```

## Browser QA (operador)

Pendiente de confirmación visual con stack levantado (API + `ng serve`):

```txt
/admin/items/7754
/admin/items/7754/publication-status
/admin/items/12616
/admin/items/12616/edit
/admin/items/12617/publication-status
/admin/items/icon-selector
```

API smoke **PASS**; browser marcado **PENDING_OPERATOR** si no se ejecutó en esta sesión.

## Build validation

| Comando | Resultado |
| --- | --- |
| `dotnet build Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj` | **OK** (0 errors) |
| `npm run build` (Angular Admin) | **OK** (budget warning +589 B) |
| `Sunshine.sln` | No resuelto en shell (ruta con espacios); Admin API es gate oficial |

## Macro 3 closure

| Phase | Entregable | Estado |
| --- | --- | --- |
| 1 | Source map + audit scaffold | DONE |
| 2 | D2P icon extract (puntual) | DONE |
| 3 | Curated `by-icon/23012` | DONE |
| 4 | Dry-run/approve workflow | DONE / PARTIAL |
| 5 | Appearance identity audit | DONE |
| 6 | Appearance preview diagnostics | DONE / PARTIAL |
| 7 | Final QA + macro closure | **DONE** |

### Explícitamente fuera de Macro 3

```txt
EntityLook renderer / Tiphon pipeline
Extracción masiva sprites
Preview automático por AppearanceId sin PNG curado
Spells / Maps / NPC / Glyph builders
```

### Siguiente macro

**Requiere aprobación explícita del operador.** Candidatos documentados en roadmap (Macro 4 Spells, publicación cliente ampliada, etc.) — **no iniciados** en Phase 7.

## Referencias

- [README Macro 3](./README.md)
- [Phase 6 appearance curated](./appearance-preview-curated-workflow-phase6.md)
- [Phase 5 appearance audit](./appearance-identity-audit-phase5.md)
