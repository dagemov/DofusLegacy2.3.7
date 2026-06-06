# Item skin category map

Mapeo TypeId → slug de carpeta Angular (`by-category/`).

| TypeId | Enum (referencia) | Categoría slug |
| --- | --- | --- |
| 1 | AMULETTE | `amuletos` |
| 9 | ANNEAU | `anillos` |
| 10 | CEINTURE | `cinturones` |
| 11 | BOTTES | `botas` |
| 15 | RESSOURCES_DIVERSES | `recursos` |
| 16 | CHAPEAU | `sombreros` |
| 17 | CAPE | `capas` |
| 18 | FAMILIER | `mascotas` |
| 23 | DOFUS | `dofus` |
| 82 | BOUCLIER | `escudos` |
| *itemSetId > 0* | — | `sets` |
| otros (no arma) | — | `sin-categoria` |

## Armas excluidas

TypeIds: `2,3,4,5,6,7,8,19,20,21,22,83,99,102,114` (+ alineado con Admin `UnsupportedWeaponTypeIds`).

## Búsqueda planificada

| Campo | Fuente |
| --- | --- |
| NameEs | `i18n_es.d2i` + nameId |
| NameEn | `i18n_en.d2i` + nameId |
| ItemId | índice Items.d2o |
| IconId | campo item |
| TypeName | `ItemTypeEnum` o `Type{typeId}` |

Implementación: `ItemSkinCatalogDryRunner` + futuro endpoint Admin.
