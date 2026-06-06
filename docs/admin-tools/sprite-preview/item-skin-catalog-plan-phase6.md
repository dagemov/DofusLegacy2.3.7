# Item Skin Catalog — Plan Phase 6

**Estado:** `PLANNED` (dry-run implementado; sin extracción masiva PNG)  
**Rama:** `feature/client-publication-controlled-publish-phase6`

## Objetivo

Replicar la experiencia de elegir icono/skin (como Dofus Ocre → Dofus Tester) para **todas** las categorías útiles del cliente en Angular, organizadas por tipo, **sin armas**.

## Flujo futuro en Items Builder

```txt
Elegir tipo de item
  → Galería por categoría (by-category/)
  → Buscar por NameEs / NameEn / ItemId / IconId / TypeName
  → Crear item con IconId visible (sin pelear visibilidad)
```

## Source of truth

| Fuente | Uso |
| --- | --- |
| `Items.d2o` | ItemId, TypeId, IconId, AppearanceId, nameId |
| `ItemTypes.d2o` | existencia de TypeId |
| `i18n_es.d2i` / `i18n_en.d2i` | NameEs, NameEn |
| `bitmap*.d2p` | extracción futura (Macro 3 pipeline) |
| `by-icon/{iconId}.png` | preview ya curado en Admin |

## Categorías iniciales

Ver [item-skin-category-map.md](./item-skin-category-map.md).

Excluido: **armas** (TypeIds en `WeaponTypeFilter`). En este cliente muchas entradas del índice `Items.d2o` no deserializan como clase `Item` (p. ej. plantillas arma en otra clase D2O); el dry-run las omite sin leer `Weapons.d2o`.

## Dry-run (Phase 6)

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode item-skin-catalog-dry-run \
  --output "Infrastructure/temporal-artifacts/item-skin-catalog" \
  --client "Client2.3.7" \
  --exclude-types weapons
```

Salida (gitignored):

```txt
Infrastructure/temporal-artifacts/item-skin-catalog/item-skin-catalog.json
Infrastructure/temporal-artifacts/item-skin-catalog/item-skin-catalog.md
```

## Angular (estructura planificada)

```txt
src/assets/item-previews/by-category/{categoria}/.gitkeep
```

Sin copia masiva en Phase 6.

## Fases siguientes (fuera de 6)

1. Aprobación operador para import curado por categoría
2. API `GET /api/admin/v1/item-icons/catalog?category=dofus&q=...`
3. Selector modal por categoría en `/admin/items/new`
