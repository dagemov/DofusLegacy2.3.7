# Item skin catalog — muestra operador

Artefacto reducido para revisión en Git. Catálogo completo:

```txt
Infrastructure/temporal-artifacts/item-skin-catalog/by-category/item-skin-catalog.json
```

Galería HTML (local, no commiteada):

```txt
Infrastructure/temporal-artifacts/item-skin-catalog/gallery/index.html
```

Abrir en navegador: `file:///` + ruta absoluta al `index.html`.

## Categorías (último dry-run)

Ver `item-skin-catalog-by-category-sample.json` — incluye hasta 3 items por categoría exportable.

## Export dofus (dry-run)

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode item-skin-catalog-export-curated \
  --category dofus --limit 50 --dry-run \
  --output "Infrastructure/temporal-artifacts/item-skin-catalog/by-category"
```

Copia real solo con `--approve-curated-copy` (sin `--dry-run`). Phase 6B: solo categoría `dofus`.
