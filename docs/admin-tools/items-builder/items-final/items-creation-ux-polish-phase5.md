# Items Builder — UX polish Phase 5

**Rama:** `feature/client-publication-controlled-patch-phase5`

## Antes / después

| Antes | Después |
| --- | --- |
| Formulario plano tipo “slice técnico” | Flujo en 5 secciones: Identidad → Visual → Características → Reglas → Publicación |
| Errores de save como panel API genérico | Modal **No se pudo guardar el item** con secciones humanas + detalles técnicos colapsados |
| Catálogo de 507 efectos como vía principal | Tarjetas **Stats frecuentes** + búsqueda humana (`daño aire`, `golpes críticos`, …) |
| EffectId visible en cada fila | Nombre humano primero; EffectId solo con “Ver detalles técnicos” |
| Presets obligatorios en percepción | **Sin plantilla aplicada** por defecto; presets opcionales |

## Stats frecuentes (emojis)

Capa visual en `item-effect-stat-quick-picks.ts` — resuelve EffectId contra `GET /api/admin/v1/item-effects/options`. Si no hay match: **No confirmado**, no se inserta automáticamente.

## Ejemplo UX — Dofus de los Hielos (sin publish)

Preset opcional `dofus-hielos-ux`:

| Stat | EffectId |
| --- | --- |
| +40 daños | 112 |
| +80 prospección | 176 |
| +50 sabiduría | 124 |
| +10 golpes críticos | 115 |

Solo referencia en UI; no crea en VPS ni publica cliente.

## Qué quedó técnico (oculto por defecto)

- EffectId, serializationTypeId, rowId, preservedSuffixHex
- Stacktrace / JSON de errores (modal, colapsado)
- DescriptionId / publish (sección Publicación enlaza a dashboards)

## Archivos clave

- `item-write-page.component.*` — flujo 5 secciones + modal de error
- `item-effects-editor.component.*` — tarjetas stats + búsqueda
- `item-effect-stat-quick-picks.ts` — alias y quick picks
- `item-save-error-modal.component.*` — modal profesional
