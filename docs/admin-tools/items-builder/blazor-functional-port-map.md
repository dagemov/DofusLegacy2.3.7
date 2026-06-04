# Blazor Functional Port Map — Admin Tools

Date: `2026-06-02`  
Reference: `C:\Users\Hombr\source\repos\DofusBeta-2.0\Dofus-2\Rollback\Rollback.Web`  
Target: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Angular-tools\Admin\`

## Strategy

```txt
Blazor legacy (funcional) → Admin.Application + Admin.Infrastructure (C#)
                         → Admin.Api (JSON)
                         → Angular (solo vista/UX)
```

No copiar `BinaryEffects` de Rollback a Sunshine. Serialización Sunshine = `ObjectEffectSerializer` (hex en `items.Effects`).

---

## Items Builder

| Blazor source | Clase / componente | Qué hacía | Tabla / formato | Port | Admin equivalente |
| --- | --- | --- | --- | --- | --- |
| `Pages/Admin/Items.razor` | Página principal | Lista + editor split; CRUD | `items_templates` | **PORT** | `items-page`, `item-write-page`, `item-detail-page` |
| `Components/Admin/EffectListEditor.razor` | Editor filas | Add/remove/reorder effects; kinds | `GameEffectEditRow` | **PORT** | `item-effects-editor.component` |
| `Components/Admin/GameAssetPreview.razor` | Preview PNG | Manual → IconId bitmap → placeholder | URLs / filesystem | **DEFER** | `item-preview-card` (curated paths) |
| `Services/ItemAdminService.cs` | Orquestador CRUD | Save/Delete/GetPaged | `items_templates` + overrides | **PORT** (adaptado) | `ItemsAdminRead/WriteService` |
| `Services/GameEffectEditorService.cs` | Serialize effects | `EffectManager` ↔ rows | `BinaryEffects` BLOB | **ADAPT** | `SunshineItemEffectsCodec` (hex) |
| `Services/GameEffectDisplayService.cs` | Labels ES + grupos | Dropdown agrupado | — | **PORT** | `LegacyBlazorEffectLabelRegistry` |
| `Services/ItemClientPublishService.cs` | Publish cliente | SWF/i18n/PNG | Client FS | **SKIP** | No Sunshine client publish |
| `Services/ItemAppearanceResolverService.cs` | Appearance auto | SHA256 / metadata | templates | **DEFER** | — |
| `Services/ItemIdentityDiagnosticService.cs` | Auditoría identidad | Compare runtime/client | — | **DEFER** | Warnings parciales en detail |
| `Components/Admin/ItemCatalogPicker.razor` | Picker facetas | Búsqueda por tipo | templates | **DEFER** | Sets / futuro |

### Items flows

| Flujo Blazor | Port status | Notas |
| --- | --- | --- |
| Crear item | **PORT** | `POST /items`; `MAX(Id)+1` MyISAM |
| Editar item | **PORT** | `PUT /items/{id}` |
| Duplicar | **N/A en Blazor** | Angular tiene `duplicate` (extra) |
| Eliminar | **DEFER** | No expuesto en Angular MVP |
| Effects editor | **PORT** | `PUT /items/{id}/effects` |
| Icon selector modal | **PORT** | `GET /item-icons` + modal 7A |
| Conditions (`StringCriterion`) | **PORT** | Campo texto en write form → `Criteria` |
| Type selector | **PORT** | `ItemTypeEnum` → options API |
| Set link | **PORT** | `item-sets/options` + `ItemSetId` |

---

## Effects / Characteristics

| Blazor | Sunshine Admin | Port |
| --- | --- | --- |
| `BinaryEffects` + `EffectManager` | `items.Effects` hex + `SunshineItemEffectsCodec` | **ADAPT** |
| `GameEffectDisplayService` labels | `LegacyBlazorEffectLabelRegistry` | **PORT** |
| Kinds: Integer, Dice, Duration… | Phase 7B: Integer + preserve unsupported | **PARTIAL** |

Ver [items-effects-port-map.md](./items-effects-port-map.md).

---

## Icon / PNG Preview

| Blazor | Port |
| --- | --- |
| Bitmap validator por IconId | **DEFER** |
| Manual PNG upload → `admin_entity_asset_overrides` | **DEFER** |
| Curated `/assets/item-previews/by-icon/{id}.png` | **PORT** (Phase 6/7A) |

---

## Conditions

| Blazor | Port |
| --- | --- |
| `StringCriterion` texto libre | **PORT** → `items.Criteria` |
| Visual criterion builder | **DEFER** |

---

## Sets

| Blazor | Port |
| --- | --- |
| `ItemSetId` numérico en Items | **PORT** |
| Sets admin page + CSV membership | **DEFER** (otro módulo) |

---

## Spells (DEFER — no portar antes de cerrar Items)

| Blazor source | Notas | Port |
| --- | --- | --- |
| `Pages/Admin/Spells.razor` | Reusa `EffectListEditor` | **DEFER** |
| `SpellEffectCatalogService` | Catálogo hechizos | **DEFER** |

---

## Glyphs (DEFER)

No hay página admin de Glyphs en `Rollback.Web` inventariada.

---

## Ignorar explícitamente

| Módulo | Razón |
| --- | --- |
| `ItemClientPublishService` | Cliente Rollback ≠ cliente Sunshine 2.3.7 |
| `admin_entity_client_metadata` | Sin tabla equivalente obligatoria |
| `BinaryEffects` directo | Formato distinto a hex ObjectEffect |
| Armas / `items_weapons` | Fuera de alcance Items Builder |
| 44k scan | Prohibido |
