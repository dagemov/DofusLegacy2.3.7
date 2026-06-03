# Blazor → Angular Admin — Plan de porte

Date: `2026-06-02`  
Reference: `legacy-reference/Rollback.Web/` + `legacy-reference/Rollback.Admin/`  
Target: `Angular-tools/Admin/`

## Estrategia fija

```txt
Rollback legacy (referencia)     →  Admin.Application + Infrastructure + Contracts + Api
Angular RollblackLegacy.Admin    →  solo vistas, DTOs, sin serializar effects
Spells / glifos / mapas          →  después de Items Phase 8
```

---

## Dónde estamos (subfases de esta iniciativa)

**Iniciativa A — Referencia legacy en repo oficial (8 subfases)**

| Subfase | Entregable | Estado |
| ---: | --- | --- |
| A.1 | Copia controlada `legacy-reference/Rollback.Web/` | **DONE** |
| A.2 | Companion `legacy-reference/Rollback.Admin/` (lógica Items) | **DONE** |
| A.3 | Inventario `rollback-web-functional-inventory.md` | **DONE** |
| A.4 | Mapas de porte (`items-functional-port-map`, effects map) | **DONE** |
| A.5 | Roadmap + risk register actualizados | **DONE** (este doc set) |
| A.6 | Commits `chore:` + `docs:` | **DONE** (`c277a44`) |
| A.7 | Builds `dotnet` + `npm run build` | **DONE** (2026-06-02) |
| A.8 | QA manual item 12616 (AP/PM/Vitalidad + icono) | **PASSED** (2026-06-03) |

**→ Iniciativa A cerrada (A.8 / A.8).** Siguiente: **Phase 7C — Form UX Polish**.

Detalle QA: [items-builder-a8-qa-item-12616.md](./items-builder-a8-qa-item-12616.md).

---

## Producto final — Items Builder (macro, 8 fases)

| Fase macro | Nombre | Estado | % Items parity |
| ---: | --- | --- | ---: |
| 1–6 | Audit, API read, Angular list, assets | **DONE** | 40% |
| 7A | Icon selector modal | **DONE** | 55% |
| 7B | Effects / characteristics editor (C# codec) | **DONE** | 75% |
| 7C | Form UX polish (toasts, layout, conditions hints) | **NEXT** | → 85% |
| 7D | Client sprite preview extraction | **PENDING** | → 90% |
| 8 | Publish / QA workflow (sin FFDec masivo) | **PENDING** | → 95% |
| 9 | Spells admin | **DEFER** | — |
| 10 | Glifos / trampas | **DEFER** | — |

**→ Estamos en fase macro 7C de 8** para el producto Items Builder (antes de Spells).

---

## Subfase 7C — próximo trabajo (solo Items)

| # | Tarea | Capa | Referencia Blazor |
| ---: | --- | --- | --- |
| 7C.1 | Unificar toasts / errores 409/422/traceId en write | Angular | `Items.razor` status stack |
| 7C.2 | Layout form = split lista/editor (densidad) | Angular | `item-editor-layout` |
| 7C.3 | Conditions: textarea + hints no bloqueantes | API hint opcional + Angular | `StringCriterion` |
| 7C.4 | Preview warnings visibles pre-save | Angular | `GameAssetPreview` unresolved |
| 7C.5 | Duplicate flow documentado en QA checklist | Angular + docs | extra vs Blazor |

---

## Subfase 7D — preview cliente (documentación primero)

| # | Tarea | Capa | Decisión |
| ---: | --- | --- | --- |
| 7D.1 | Documentar extractor sprite vs `IconId` | docs + infra | `REFERENCE_ONLY` Blazor publish |
| 7D.2 | Endpoint preview enriquecido (sin SWF write) | API read | No `ItemClientPublishService` |

---

## Subfase 8 — QA / publish

| # | Tarea | Capa | Decisión |
| ---: | --- | --- | --- |
| 8.1 | Panel QA derivado (no workflow DB) | Angular | Blazor audit badges |
| 8.2 | Checklist operador + item tester SQL | docs | `dofus-tester-item-creation.md` |
| 8.3 | Client publish real | — | **OUT OF SCOPE** Phase 8 |

---

## Qué va a cada capa (resumen)

| Concern | Admin API (C#) | Angular |
| --- | --- | --- |
| CRUD item | `ItemsAdminWriteService`, repos | forms, routing |
| Effects hex | `SunshineItemEffectsCodec`, `ItemEffectsAdminService` | `item-effects-editor` bind DTO |
| Effect labels | `LegacyBlazorEffectLabelRegistry` | dropdown groups |
| Icon list | `item-icons` query | modal selector |
| Conditions | validate optional, persist string | textarea |
| Publish cliente | **no** en MVP | badge solo lectura |
| BinaryEffects | **nunca** en Angular | — |

---

## Documentos relacionados

- [rollback-web-functional-inventory.md](./rollback-web-functional-inventory.md)
- [items-functional-port-map.md](./items-functional-port-map.md)
- [items-effects-port-map.md](./items-effects-port-map.md)
- [blazor-functional-port-map.md](./blazor-functional-port-map.md)
- [items-builder-blazor-parity-audit.md](./items-builder-blazor-parity-audit.md)
