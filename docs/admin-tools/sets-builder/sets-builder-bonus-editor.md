# Sets Builder — Editor de bonos por piezas

**Fecha:** 2026-06-05  
**Rama:** `feature/sets-builder-crud-and-pagination`

## UX

Componente `app-item-set-bonus-editor` agrupa efectos por cantidad de piezas:

- 2, 3, 4, 5 piezas (tiers por defecto)
- Set completo (último tier cuando `pieceCount >= itemCount`)
- Añadir/quitar tier
- Añadir/quitar efecto por tier
- Selector de efecto con labels de `GET /api/admin/v1/item-effects/options`
- Valor entero o dados (`defaultSerializationTypeId === 73`)

## Contrato de guardado

Cada tier envía:

```txt
pieceCount
effects[] → effectId, value, diceNum?, diceSide?, format
```

Backend codifica con `ItemSetEffectsCodec.EncodeTiers` hacia `items_sets.Effects`.

## Lectura

`GET /api/admin/v1/item-sets/{setId}` devuelve `bonusTiers[]` con:

```txt
effectId, label, value, diceNum, diceSide, format
```

Labels desde catálogo Admin (no inventados).

## Pantallas

- Detalle: tabla legible por tier
- Editar: `item-set-write-page` + bonus editor embebido

## Referencias

- `ItemSetEffectsCodec.cs`
- `item-set-bonus-editor.component.ts`
- `item-set-write-page.component.ts`
