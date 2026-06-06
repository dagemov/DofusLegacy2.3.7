# Reporte paquete staging — Item 12617

**Generado:** 2026-06-04 (Phase 3C)  
**Ruta:** `Infrastructure/staging-client/publication-package-phase3c/12617/`  
**ValidationStatus:** `READY_FOR_CONTROLLED_PUBLISH`

## Resumen

| Campo | Valor |
| --- | --- |
| PackageId | `staging-publication-12617-20260604021640` |
| SourceTemplateItemId | 7754 |
| TargetItemId | 12617 |
| nameId | 63079 |
| descriptionId | 63080 |
| TypeId | 23 |
| IconId | 23012 |
| AppearanceId | 0 (N/A) |

## Validación (extracto)

- ItemId 12617 presente en `Items.d2o` del paquete
- TypeId 23 válido en `ItemTypes.d2o` de referencia
- nameId / descriptionId resueltos en ES y EN
- IconId 23012: `CURATED_BY_ICON` (`by-icon/23012.png`)
- Checksums manifest ↔ archivos: OK

## Checksums (SHA-256)

```txt
d91b27c978ed0f7ec3f43ec939d5d1e8168cb2bbe46eb0e328f5c949884a6e02  data/common/Items.d2o
c085c478f60a7f7ba5bed9788952ac9a78056ed29094741eeda887de7f038ef5  data/i18n/i18n_es.d2i
558e78093ee181b0d49ecba664dd976a7bebbd56757192feefe6a259b960f6a7  data/i18n/i18n_en.d2i
```

## Textos i18n (caso control)

| Locale | nameId 63079 | descriptionId 63080 |
| --- | --- | --- |
| ES | Dofus de los Hielos | Dofus de los Hielos creado para pruebas controladas del pipeline de publicación. |
| EN | Ice Dofus | Ice Dofus created for controlled publication pipeline testing. |

## Próximos pasos manuales

1. No copiar este paquete a `Client2.3.7` original.
2. Phase 4: aplicar patch en copia backup del cliente.
3. Regenerar lane launcher (`data.meta`, `VerInfo.rec`) tras QA.

## Comandos de reproducción

```bash
dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode stage-item-publication --item-id 12617 \
  --output "Infrastructure/staging-client/publication-package-phase3c/12617"

dotnet run --project "Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj" -- \
  --mode validate-publication-package \
  --package "Infrastructure/staging-client/publication-package-phase3c/12617"
```
