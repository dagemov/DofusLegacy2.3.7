# Item Skin Catalog by Category — Phase 6B

**Rama:** `feature/item-skin-catalog-by-category-phase6b`  
**Estado:** `DONE`

## Objetivo

Catálogo visual por categorías para Angular, fuente `bitmap*.d2p` + `Items.d2o` + i18n, pipeline C# (no JPEXS para D2P, no PyDofus obligatorio).

## Categorías exportables

`dofus`, `sombreros`, `capas`, `botas`, `mascotas`, `escudos`, `anillos`, `amuletos`, `cinturones`, `recursos`

## CLI

### Dry-run catálogo

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode item-skin-catalog-dry-run \
  --output "Infrastructure/temporal-artifacts/item-skin-catalog/by-category" \
  --exclude-types weapons
```

Salidas:

```txt
by-category/item-skin-catalog.json
by-category/item-skin-catalog.md
by-category/weapon-type-exclusions.json
../gallery/index.html
```

### Export curado

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode item-skin-catalog-export-curated \
  --category dofus --limit 50 --dry-run \
  --output "Infrastructure/temporal-artifacts/item-skin-catalog/by-category"
```

Copia PNG: añadir `--approve-curated-copy` y quitar `--dry-run`. Solo `dofus` puede copiar en 6B; otras categorías quedan en plan dry-run.

## Campos del catálogo

| Campo | Descripción |
| --- | --- |
| ItemId | índice Items.d2o |
| NameEs / NameEn | i18n |
| TypeId / TypeNameEs | tipo + enum |
| Category | slug carpeta Angular |
| IconId / AppearanceId | identidad visual |
| IconPreviewAvailable | admin by-icon o D2P PNG |
| IconSource | `admin-by-icon` \| `client-bitmap-d2p` \| `missing` |
| TargetAngularPath | `src/assets/item-previews/by-category/{cat}/{iconId}.png` |
| ExcludedReason | null si incluido; armas fuera del catálogo |

## Armas excluidas

Ver [item-skin-category-type-map.md](./item-skin-category-type-map.md) y `weapon-type-exclusions.json` del dry-run.

## Angular futuro

- Carpetas: `src/assets/item-previews/by-category/*`
- Componente planificado: `ItemSkinCatalogBrowserComponent` (no implementado en 6B)
- Stats UX: iconos en `src/assets/icons/` enlazados desde `item-effect-stat-quick-picks.ts`

## PyDofus

Auditoría auxiliar: [pydofus-compatibility-audit.md](./pydofus-compatibility-audit.md) — no es dependencia del build.
