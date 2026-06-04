# Item skin category & weapon type map

## TypeId → categoría (export)

| TypeId | Enum | Categoría |
| --- | --- | --- |
| 23 | DOFUS | `dofus` |
| 16 | CHAPEAU | `sombreros` |
| 17 | CAPE | `capas` |
| 11 | BOTTES | `botas` |
| 18 | FAMILIER | `mascotas` |
| 82 | BOUCLIER | `escudos` |
| 9 | ANNEAU | `anillos` |
| 1 | AMULETTE | `amuletos` |
| 10 | CEINTURE | `cinturones` |
| 15 | RESSOURCES_DIVERSES | `recursos` |

Otros TypeIds legibles como `Item` → excluidos del catálogo exportable (`sin-categoria`).

## TypeIds arma (excluidos)

Estáticos (Admin `UnsupportedWeaponTypeIds`):

```txt
2, 3, 4, 5, 6, 7, 8, 19, 20, 21, 22, 83, 99, 102, 114
```

Más detección por nombre enum (`EPEE`, `DAGUE`, `ARC`, `MARTEAU`, …) y keywords (`sword`, `dagger`, `weapon`, …).

Lista completa generada en cada dry-run: `weapon-type-exclusions.json`.

## Fuentes

| Fuente | Uso |
| --- | --- |
| `Items.d2o` | items + IconId |
| `ItemTypes.d2o` | índice tipos (sin deserializar clase en pipeline) |
| `i18n_*.d2i` | nombres item |
| `content/gfx/items/bitmap*.d2p` | resolución PNG vía `CatalogD2pIconResolver` |
| `by-icon/` | previews ya curados en Admin |
