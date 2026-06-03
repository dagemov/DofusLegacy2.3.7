# Rollback.Web — Inventario funcional

Date: `2026-06-02`  
Reference tree: `legacy-reference/Rollback.Web/` + `legacy-reference/Rollback.Admin/`  
Source origin: `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\`

Decision codes: `PORT_TO_ADMIN_API` | `PORT_TO_ANGULAR_VIEW` | `REFERENCE_ONLY` | `IGNORE` | `HIGH_RISK` | `DEFER`

---

## Items Builder (prioridad actual)

| Módulo | Archivo | Clase / componente | Método / flujo | Qué hacía | Tabla / formato | Decisión | Estado Sunshine |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Items UI | `Pages/Admin/Items.razor` | Página | `OnInitializedAsync`, `ReloadAsync` | Lista paginada + filtros tipo/nivel | `items_templates` (+ catálogo) | `PORT_TO_ANGULAR_VIEW` + API | `items-page` |
| Items UI | `Items.razor` | — | `LoadItemAsync`, `CreateNewAsync` | Split editor cargar / nuevo | `ItemEditModel` | `PORT_TO_ANGULAR_VIEW` | `item-write-page` |
| Items UI | `Items.razor` | — | `SaveAsync` | Persistir template + effects + publish opcional | SQL + `BinaryEffects` | `PORT_TO_ADMIN_API` | `ItemsAdminWriteService` |
| Items UI | `Items.razor` | — | `DeleteAsync` | Borrar template | `items_templates` | `DEFER` | No expuesto MVP |
| Items UI | `Items.razor` | — | `PublishClientSupportAsync` | Publicar SWF/i18n/PNG cliente | FS + FFDec | `HIGH_RISK` / `IGNORE` | Sin publish cliente Sunshine |
| Items UI | `Items.razor` | — | `ApplyIdentityCorrectionAsync` | Corregir IconId/Appearance | diagnóstico | `DEFER` | Warnings parciales en detail |
| Items UI | `Items.razor` | — | `UploadManualAssetAsync` | PNG manual preview | `wwwroot/admin-assets/items` | `REFERENCE_ONLY` | Pipeline curado en repo oficial |
| Effects UI | `Components/Admin/EffectListEditor.razor` | Componente | Add/Move/Remove rows | Editor filas effects + kinds | `GameEffectEditRow` | `PORT_TO_ANGULAR_VIEW` | `item-effects-editor` |
| Effects UI | `EffectListEditor.razor` | — | `UpdateEffectId`, kinds | Dropdown agrupado ES | — | `PORT_TO_ADMIN_API` (options) | `GET /item-effects/options` |
| Preview | `Components/Admin/GameAssetPreview.razor` | Componente | Render cascade | Manual → bitmap → placeholder | URLs | `PORT_TO_ANGULAR_VIEW` | `item-preview-card` (parcial) |
| Icon pick | `Components/Admin/ItemCatalogPicker.razor` | Componente | Facetas / búsqueda | Picker catálogo por tipo | templates | `DEFER` | Icon modal 7A distinto |
| Pager | `Components/Admin/AdminPager.razor` | Componente | Page change | Paginación lista | — | `PORT_TO_ANGULAR_VIEW` | Lista Angular |
| CRUD | `Rollback.Admin/Services/ItemAdminService.cs` | Servicio | `GetPagedAsync`, `GetByIdAsync`, `SaveAsync` | Orquestación SQL + effects + publish | `items_templates`, overrides | `PORT_TO_ADMIN_API` | Read/Write repos |
| CRUD | `ItemAdminService.cs` | — | `DeleteAsync` | Delete | `items_templates` | `DEFER` | — |
| Effects | `GameEffectEditorService.cs` | Servicio | `Deserialize` / `Serialize` | `EffectManager` ↔ filas | `BinaryEffects` BLOB | `ADAPT` → hex | `SunshineItemEffectsCodec` |
| Labels | `GameEffectDisplayService.cs` | Servicio | `GetOptions` | Grupos ES (Principales, Stats…) | — | `PORT_TO_ADMIN_API` | `LegacyBlazorEffectLabelRegistry` |
| Appearance | `ItemAppearanceCatalogService.cs` | Servicio | Opciones apariencia | Lista appearance por tipo | metadata cliente | `DEFER` | IconId focus 7A |
| Appearance | `ItemAppearanceResolverService.cs` | Servicio | Auto-resolve | SHA256 / client refs | — | `DEFER` | — |
| Publish | `ItemClientPublishService.cs` | Servicio | Publish | Parche cliente real | SWF/i18n | `HIGH_RISK` | `IGNORE` Sunshine |
| Publish | `FfdecItemScriptExtractor.cs` | Servicio | Extract | FFDec scripts | — | `IGNORE` | — |
| Diagnostics | `ItemIdentityDiagnosticService.cs` | Servicio | Compare | Runtime vs client identity | — | `REFERENCE_ONLY` | Detail warnings |
| Diagnostics | `ItemAuditEvaluator.cs` | Servicio | Audit flags | Calidad template | — | `REFERENCE_ONLY` | QA panel futuro |
| Models | `Models/Items/ItemEditModel.cs` | DTO | — | Form bindable | campos template | `PORT_TO_ADMIN_API` | Contracts DTOs |
| Models | `Models/GameEffects/GameEffectEditRow.cs` | DTO | — | Fila editor | effect id, kind, tiers | `PORT_TO_ADMIN_API` | `ItemEffectRowDto` |
| Web | `Services/AdminAssetUploadService.cs` | Servicio | Upload | Guardar PNG manual | filesystem | `REFERENCE_ONLY` | No port directo |
| Conditions | `Items.razor` | campo | `StringCriterion` textarea | Criterios legado texto libre | string SQL | `PORT_TO_ADMIN_API` + view | `Criteria` en write form |
| Types | `ItemTypeLabelService.cs` | Servicio | Labels | Nombres tipo ES | enum | `PORT_TO_ADMIN_API` | `item-types/options` |
| Sets | `SetAdminService.cs` | Servicio | Sets CRUD | Bonus sets | sets tables | `DEFER` | `item-sets/options` link only |

---

## Otros módulos Blazor (no prioridad — inventario corto)

| Módulo | Archivo | Decisión | Notas |
| --- | --- | --- | --- |
| Spells | `Pages/Admin/Spells.razor` | `DEFER` | Después de Items Phase 8 |
| Sets | `Pages/Admin/Sets.razor` | `DEFER` | Effects por tier en Blazor |
| Monsters | `Pages/Admin/Monsters.razor` | `DEFER` | Riesgo spawn/alta |
| NPCs / Vendors | `Pages/Admin/Npcs.razor`, `Vendors.razor` | `DEFER` | — |
| Maps / Spawns | `Spawns.razor` | `HIGH_RISK` | R4 risk register |
| Auth portal | `Login.razor`, `Register.razor` | `IGNORE` | Admin Angular usa auth propio |
| Accounts API | `Controllers/AuthController.cs` | `IGNORE` | — |

---

## Formato effects — decisión arquitectónica

| Sistema | Columna | Codec | Port |
| --- | --- | --- | --- |
| Rollback legacy | `BinaryEffects` | `EffectManager` (world) | **No copiar** a Sunshine |
| Sunshine oficial | `items.Effects` | hex `ObjectEffectSerializer` | `SunshineItemEffectsCodec` en Infrastructure |

Ver [items-builder-effects-serialization-audit.md](./items-builder-effects-serialization-audit.md).

---

## Assets copiados

| Ruta en repo | Archivos | Inventario |
| --- | ---: | --- |
| `legacy-reference/Rollback.Web/wwwroot/admin-assets/items/*.png` | 33 | Uploads manuales operador (referencia UX preview) |
| `legacy-reference/Rollback.Web/wwwroot/assest-img/` | 4 PNG/JPG | Branding portal, no ítems |

No se copiaron packs `by-icon` masivos ni carpetas cliente SWF.
