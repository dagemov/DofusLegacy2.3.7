# Items — Mapa funcional de porte (Blazor → Sunshine Admin)

Date: `2026-06-02`  
Consolidates [items-functional-port-phase7b.md](./items-functional-port-phase7b.md) with the in-repo legacy reference.

## Reference paths

| Layer | Legacy (read-only) | Official |
| --- | --- | --- |
| UI | `legacy-reference/Rollback.Web/Pages/Admin/Items.razor` | `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/` |
| Effects UI | `legacy-reference/Rollback.Web/Components/Admin/EffectListEditor.razor` | `item-effects-editor.component.*` |
| Services | `legacy-reference/Rollback.Admin/Services/ItemAdminService.cs` | `ItemsAdminRead/WriteService`, repos |
| Effects codec | `GameEffectEditorService.cs` (`BinaryEffects`) | `SunshineItemEffectsCodec.cs` (hex) |
| Labels | `GameEffectDisplayService.cs` | `LegacyBlazorEffectLabelRegistry.cs` |

---

## Flujos Items — matriz de porte

| Flujo Blazor | API / Angular oficial | Decisión | Subfase |
| --- | --- | --- | --- |
| Listar + buscar + filtros | `GET /items`, `items-page` | `PORT_TO_ADMIN_API` + view | Done (Phase 3+) |
| Detalle diagnóstico | `GET /items/{id}`, `item-detail-page` | `PORT` | Done |
| Crear item | `POST /items`, `item-write-page` create | `PORT` | Done (7) |
| Editar campos template | `PUT /items/{id}` | `PORT` | Done (7) |
| Duplicar | `POST /items/{id}/duplicate` | `PORT` (extra Angular) | Done |
| Eliminar | — | `DEFER` | — |
| Effects editor | `GET/PUT /items/{id}/effects` | `PORT` | **7B DONE** |
| Icon selector | `GET /item-icons` + modal | `PORT` | **7A DONE** |
| Preview PNG | preview card + curated assets | `PARTIAL` | 7C–7D |
| Conditions string | `Criteria` en write DTO | `PORT` | 7C hints |
| Type / set selectors | options endpoints | `PORT` | Done |
| Publish cliente | Blazor `PublishClientSupportAsync` | `IGNORE` | Phase 8 badge only |
| Identity correction | Blazor botón corrección | `DEFER` | — |
| Manual asset upload | `admin-assets/items` | `REFERENCE_ONLY` | 7D |

---

## Campos template — mapping

| Blazor `ItemEditModel` | Sunshine / Admin DTO | Notas |
| --- | --- | --- |
| `Id` | `Id` | `short`, `MAX+1` MyISAM |
| `Name` / manual name | `Name` + i18n fields | Sunshine tiene `DescriptionId` |
| `TypeId` | `Type` | `ItemTypeEnum` |
| `Level`, `Price`, `Weight` | mismos | — |
| `IconId` | `IconId` | separado de `Id` (R13) |
| `AppearanceId` | `AppearanceId` | — |
| `Criteria` | `Criteria` | texto libre |
| `Effects` (binary) | `Effects` hex | codec distinto |
| `ItemSetId` | `ItemSetId` | link set |

---

## Estado por capacidad (% orientativo)

| Capacidad | % | Bloqueante restante |
| --- | ---: | --- |
| CRUD template | 90% | delete, publish |
| Effects | 80% | kinds Dice/Duration preserve-only |
| Icons | 85% | más guardrails pre-save (7C) |
| Preview | 50% | sprite cliente (7D) |
| Conditions | 70% | validación hints (7C) |
| QA / publish | 20% | Phase 8 |

---

## Próximo slice

Ver [blazor-to-angular-port-plan.md](./blazor-to-angular-port-plan.md) — **7C Form UX Polish**.

Effects detail: [items-effects-port-map.md](./items-effects-port-map.md).
