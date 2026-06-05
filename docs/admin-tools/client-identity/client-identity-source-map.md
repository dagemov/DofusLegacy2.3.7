# Client Identity Source Map

## Objetivo

Dejar trazado que archivo responde cada pregunta del audit.

## Source map

| Pregunta | Fuente primaria | Uso actual en Phase 1 |
| --- | --- | --- |
| El `ItemId` existe en cliente? | `Client2.3.7/data/common/Items.d2o` | lookup puntual por `ItemId` |
| El `DescriptionId` DB resuelve en ES? | `Client2.3.7/data/i18n/i18n_es.d2i` | lookup puntual por `DescriptionId` |
| El `DescriptionId` DB resuelve en EN? | `Client2.3.7/data/i18n/i18n_en.d2i` | lookup puntual por `DescriptionId` |
| El `nameId` cliente resuelve en ES? | `Client2.3.7/data/i18n/i18n_es.d2i` | lookup puntual por `nameId` |
| El `nameId` cliente resuelve en EN? | `Client2.3.7/data/i18n/i18n_en.d2i` | lookup puntual por `nameId` |
| El `typeId` cliente existe y como se llama? | `Client2.3.7/data/common/ItemTypes.d2o` + `i18n*.d2i` | lookup puntual por `typeId` |
| El `itemSetId` cliente existe y como se llama? | `Client2.3.7/data/common/ItemSets.d2o` + `i18n*.d2i` | lookup puntual por `itemSetId` |
| El `AppearanceId` existe? | `Client2.3.7/data/common/Appearances.d2o` | lookup puntual solo si `AppearanceId > 0` |
| Hay preview curado listo en Admin? | `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews` | filesystem check |
| Hay pack de iconos del cliente? | `Client2.3.7/content/gfx/items/bitmap0.d2p`, `bitmap1.d2p` | presencia de packs, sin extraccion |
| El item runtime existe en DB? | `sunshine.items` | query read-only por `Id` |

## Regla de interpretacion

El estado visible de un item no puede inferirse solo desde DB.

Se necesitan al menos estas dos capas:

1. runtime server-side
2. metadata cliente publicada

## Regla concreta

```txt
DB row + IconId + vendor != item visible
```

La pregunta determinante sigue siendo:

```txt
Existe el ItemId en Items.d2o?
```
