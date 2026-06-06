# Acceptance test — Items + Sets + publicación (producción)

**Fecha:** 2026-06-06  
**Rama:** `feature/items-sets-production-acceptance-test`  
**Base:** `feature/sets-builder-crud-and-pagination`  
**Resultado global:** `PARTIAL` — código listo; QA in-game y publish requieren operador.

## Criterios de éxito

| # | Criterio | Estado |
| --- | --- | --- |
| 1 | Crear item con stats en un solo flujo | **PASS** (código) |
| 2 | Crear sets con partes y bonuses | **OPERATOR_REQUIRED** |
| 3 | Publication packages validan | **OPERATOR_REQUIRED** (sin `Items.d2o` local en repo) |
| 4 | Cliente conoce items publicados | **OPERATOR_REQUIRED** |
| 5 | NPC vendedor QA muestra items | **OPERATOR_REQUIRED** |
| 6 | Reinicio seguro no rompe server | **OPERATOR_REQUIRED** |
| 7 | QA in-game confirma visibilidad | **NOT_RUN** |

## Parte 1 — Create item + effects (PASS)

### Cambios

- `ItemCreateRequest.Effects` — efectos opcionales en create.
- `ItemsAdminWriteService` — codifica efectos en transacción de insert.
- Angular `/admin/items/new` — editor de efectos en modo `draftMode`; guardado unificado.
- `preview-state` acepta `typeId` para resolver `BY_CATEGORY`.

### Validación manual

```txt
1. /admin/items/new
2. Identidad + icono + tipo + set opcional
3. Añadir stats (quick picks o catálogo)
4. Guardar item
5. Abrir detalle → effects persisten
```

## Parte 2 — Item 12618 (Capa del gay)

| Campo | Valor |
| --- | --- |
| ItemId | 12618 |
| IconId | 17039 |
| DescriptionId | 50092 |
| AppearanceId | null |
| Preview PNG | `src/assets/item-previews/by-category/capas/17039.png` (**existe**) |
| Cliente | `CLIENT_UNKNOWN` — requiere publication package |

### Acciones operador

```powershell
# Staging (ajustar textos si hace falta)
dotnet run --project infrastructure/scripts/ClientItemPublicationPipeline `
  --mode stage-item-publication `
  --target-item-id 12618 `
  --source-item-id <PLANTILLA_CAPE> `
  --clone-type-id 17 `
  --clone-icon-id 17039 `
  --clone-appearance-id 0 `
  --es-name "Capa del gay" `
  --es-description "<descripcion>" `
  --en-name "..." `
  --en-description "..."

dotnet run --project infrastructure/scripts/ClientItemPublicationPipeline `
  --mode validate-publication-package `
  --package "Infrastructure/staging-client/publication-package-phase3c/12618" `
  --target-item-id 12618 `
  --clone-type-id 17
```

No marcar visible hasta `validate-publication-package` PASS.

## Parte 3 — RollBlack Set

| Campo | Valor |
| --- | --- |
| Nombre | RollBlack Set |
| Level | 200 |
| Piezas | 3 |

### Items

| Nombre | TypeId |
| --- | --- |
| RollBlack Hat | 16 (sombrero) |
| RollBlack Cape | 17 (capa) |
| RollBlack Amulet | 1 (amuleto) |

### Stats por pieza (EffectIds catálogo)

| Stat | EffectId |
| --- | --- |
| +30 daños | 118 |
| +60 fuerza | 54 |
| +60 inteligencia | 62 |
| +60 suerte | 59 |
| +60 agilidad | 55 |
| +400 vitalidad | 61 |
| +1 alcance | 53 |
| +1 invocación | 114 |

### Bonus 3 piezas

| Stat | EffectId |
| --- | --- |
| +30 daños | 118 |
| +250 prospección | 176 |
| +10% resist. neutral | 214 |
| +10% resist. tierra | 210 |
| +10% resist. fuego | 213 |
| +10% resist. agua | 211 |
| +10% resist. aire | 212 |

Crear vía Admin: `/admin/item-sets/new` + `/admin/items/new` (con effects en create).

## Parte 4 — Set del gay / Set Toady Floral

Nombre público sugerido: **Set Toady Floral** (confirmar con operador).

| Pieza | Notas |
| --- | --- |
| Casco Toady | crear con mismos stats que Capa del gay |
| Capa del gay | 12618 |
| Varita de la Flor | ver Parte 5 |

### Bonus set (3 piezas)

| Stat | EffectId |
| --- | --- |
| +500 iniciativa | 174 |
| +1 PA | 111 |
| +350 vitalidad | 61 |
| +100 sabiduría | 60 |
| +120 prospección | 176 |

## Parte 5 — Varita de la Flor

**Estado:** `BLOCKED_WEAPON_PUBLICATION`

| Requisito | Soporte Admin |
| --- | --- |
| tipo varita | TypeId arma → bloqueado Phase 7 (`items_weapons`) |
| 3 PA, daño int+aire | requiere fila `items_weapons` + D2O weapon model |

No publicar varita hasta auditar serialización weapon en DB + `Items.d2o`. Documentar en handoff; no romper cliente.

## Parte 6 — Sets client visibility

Ver [itemsets-client-publication-plan.md](../../client-publication/itemsets-client-publication-plan.md).

Sin `ItemSets.d2o` en paquete → **PARTIAL** para bonus/UI set in-game.

## Parte 7 — NPC vendedor QA

| Campo | Valor |
| --- | --- |
| NpcId | **1053** (Vendeur de Dofus) |
| Uso | QA únicamente — no vendedor crítico |

```sql
INSERT INTO npcs_items (NpcId, Item, Price, Token)
VALUES (1053, <ITEM_ID>, 500000, 0);
```

Validar: NPC muestra items, compra OK, iconos visibles, relog persiste.

## Parte 8 — Backup + publish + restart

Backups confirmados:

```txt
DB VPS: /root/backups/sunshine-focused-20260606-004715.sql
VPS inventory: backups/vps/20260606-004658/
Cliente local: backups/client/pre_creation_new_items_20260605_0732
```

Secuencia (solo tras package PASS):

```powershell
$env:CONFIRM_PUBLISH = "1"
dotnet run --project infrastructure/scripts/ClientItemPublicationPipeline `
  --mode apply-package-to-real-client `
  --package "Infrastructure/staging-client/publication-package-phase3c/<ITEM_ID>"

dotnet run --project infrastructure/scripts/ClientItemPublicationPipeline `
  --mode validate-real-client `
  --target-item-id <ITEM_ID>

$env:CONFIRM_RESTART = "1"
./scripts/vps/restart-world-safe.sh
```

## Parte 9 — QA in-game checklist

```txt
[ ] RollBlack Hat / Cape / Amulet visibles
[ ] RollBlack Set visible + bonus 3p
[ ] Set Toady Floral visible
[ ] Casco Toady + Capa visibles
[ ] Varita — N/A si BLOCKED_WEAPON
[ ] NPC 1053 — compra OK
[ ] Relog persiste
[ ] Servidor online post-restart
```

## Commits de esta rama

```txt
feat: support item creation with effects
feat: add item set client publication validation
docs: record items sets production acceptance test
```

`feat: create production acceptance sets and items` — datos en DB vía operador (no commitear backups ni SQL con secrets).
