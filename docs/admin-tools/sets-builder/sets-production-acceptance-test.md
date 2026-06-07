# Acceptance test — Sets Builder (producción)

**Fecha:** 2026-06-06  
**Rama:** `feature/items-sets-production-acceptance-test`  
**Resultado:** `PARTIAL` — CRUD Admin OK; publicación cliente y QA in-game pendientes.

## Alcance Sets Builder

| Funcionalidad | Estado |
| --- | --- |
| Listado paginado `/admin/item-sets` | PASS |
| CRUD API `POST/PUT/DELETE /api/admin/v1/item-sets` | PASS |
| Editor bonus (`item-set-bonus-editor`) | PASS |
| Crear items del set con effects en create | PASS (Parte 1 items) |
| Publicación `ItemSets.d2o` | PARTIAL — ver plan cliente |

## Sets de prueba producción

### RollBlack Set

```txt
Name: RollBlack Set
Level: 200
Pieces: 3
Items: RollBlack Hat, RollBlack Cape, RollBlack Amulet
```

Bonus 3 piezas documentados en [items-sets-production-acceptance-test.md](../items-builder/items-final/items-sets-production-acceptance-test.md).

### Set Toady Floral (nombre alternativo)

```txt
Name: Set del gay (operador) / Set Toady Floral (sugerido público)
Pieces: Casco Toady, Capa del gay (12618), Varita de la Flor (BLOCKED)
```

## Flujo operador recomendado

```txt
1. POST /api/admin/v1/item-sets — crear set vacío o con bonus
2. Por cada pieza: POST /api/admin/v1/items con effects + setId
3. PUT /api/admin/v1/item-sets/{id} — vincular ItemsCSV / bonus tiers
4. Por cada item: stage-item-publication + validate
5. Opcional: copiar ItemSets.d2o al paquete (plan cliente)
6. CONFIRM_PUBLISH=1 + restart seguro
7. QA in-game
```

## EffectIds set bonus (referencia)

Usar solo IDs del catálogo `LegacyBlazorEffectLabelRegistry` / Admin effect options API.

Resistencias %: 214 (neutral), 210 (tierra), 213 (fuego), 211 (agua), 212 (aire).

## Bloqueos

| Bloqueo | Impacto |
| --- | --- |
| Sin `stage-item-set-publication` | Operador debe parchear `ItemSets.d2o` manualmente en paquete |
| Varita BLOCKED | Set de 3 piezas incompleto en cliente hasta soporte weapons |
| QA NOT_RUN | PASS final requiere operador in-game |

## Siguiente si PASS

```txt
Items Builder = COMPLETE
Sets Builder = COMPLETE (con nota ItemSets staging)
Publication Pipeline = COMPLETE (ItemSets opcional)
Siguiente macro = Combat Sanitization
```
