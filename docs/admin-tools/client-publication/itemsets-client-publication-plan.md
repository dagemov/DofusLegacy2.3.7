# Plan — publicación de sets en cliente (ItemSets.d2o)

**Fecha:** 2026-06-06  
**Rama:** `feature/items-sets-production-acceptance-test`  
**Estado:** `PARTIAL` — validación y apply opcionales implementados; staging automático pendiente.

## Contexto

Los items publicados vía `apply-package-to-real-client` actualizan:

- `data/common/Items.d2o`
- `data/i18n/i18n_es.d2i`
- `data/i18n/i18n_en.d2i`

Para que el **nombre del set**, la **lista de piezas** y la **UI de set** reconozcan un set nuevo, el cliente también necesita una entrada en `data/common/ItemSets.d2o` (y textos i18n del `nameId` del set).

El legacy Blazor publicaba vía `ItemSets0.swf` (`SetClientPublishService`). Este repo 2.3.7 audita `ItemSets.d2o` en client identity.

## Implementado en esta rama

| Pieza | Estado |
| --- | --- |
| `PublicationPackagePaths.ItemSetsRelative` | OK |
| `PublicationPackagePatchFiles` — detecta `ItemSets.d2o` opcional en paquete | OK |
| `validate-publication-package` — valida `ItemSetId` si el archivo está en el paquete | OK |
| `apply-package-to-real-client` / sandbox — copia `ItemSets.d2o` si viene en el paquete | OK |
| `stage-item-set-publication` (generar ItemSets.d2o desde DB) | **PENDIENTE** |

## Flujo objetivo (operador)

```txt
1. Crear set + piezas + bonus en Admin (sunshine.items_sets)
2. Publicar cada item (Items.d2o + i18n) — flujo existente
3. Generar/copiar ItemSets.d2o parcheado al directorio del paquete
4. validate-publication-package --expected-set-id <SET_ID>
5. CONFIRM_PUBLISH=1 apply-package-to-real-client
6. validate-real-client
```

## Cómo incluir ItemSets.d2o en un paquete hoy

Copia supervisada al directorio del paquete staging:

```txt
Infrastructure/staging-client/publication-package-phase3c/<ITEM_ID>/data/common/ItemSets.d2o
```

Regenerar checksums:

```powershell
dotnet run --project infrastructure/scripts/ClientItemPublicationPipeline `
  --mode validate-publication-package `
  --package "Infrastructure/staging-client/publication-package-phase3c/<ITEM_ID>" `
  --target-item-id <ITEM_ID> `
  --clone-type-id <TYPE_ID>
```

Si el item tiene `itemSetId > 0`, el validador comprobará que ese id existe en el `ItemSets.d2o` del paquete.

## Próximo desarrollo (staging automático)

1. Modo CLI `stage-item-set-publication` que lea `items_sets` + miembros desde DB.
2. Clonar plantilla de set existente en `ItemSets.d2o` (misma clase D2O que client identity).
3. Escribir `nameId` vía `d2i-append-text` (reutilizar pipeline i18n).
4. Incluir `ItemSets.d2o` en `GeneratedFiles` del manifiesto.
5. Exponer estado en Admin `/admin/item-sets/:id/publication-status` (futuro).

## Riesgos

- Bonus de set **in-game** pueden seguir resolviéndose desde servidor (`BinaryEffects` en DB); `ItemSets.d2o` cubre identidad cliente.
- Sin `ItemSets.d2o` en paquete: items visibles, set UI puede mostrar "desconocido".
- No forzar publish sin `validate-publication-package` PASS.

## Referencias

- [client-publication-phase6-controlled-publish.md](./client-publication-phase6-controlled-publish.md)
- [items-final-production-acceptance-test.md](../items-builder/items-final-production-acceptance-test.md)
- Legacy: `legacy-reference/Rollback.Admin/Services/SetClientPublishService.cs`
