# Spell Builder - Known Limitations

Fecha: `2026-06-07`
Macro: `Macro 5 / Spell Builder`
Estado de cierre: `PARTIAL`

## Limitaciones funcionales

### 1. No existe write de effects

Estado:

- vigente

Impacto:

- `effects` y `criticalEffects` solo pueden auditarse en lectura
- no existe editor funcional de effects

Razon:

- el stack actual no tiene estrategia segura de reserializacion de payloads
- Sunshine soporta payload hex y fallback binario legacy

### 2. No existe validacion positiva de referencia sana en este entorno

Estado:

- vigente en QA local

Impacto:

- solo se valido el fallback con `reference = null`
- no se pudo validar UI con `referenceAvailable = true`

Evidencia:

- se escanearon `40` paginas de `100` spells por API sin encontrar un caso positivo

### 3. La clasificacion visible depende de `typeId`

Estado:

- vigente en la muestra QA revisada

Impacto:

- `typeLabel` puede aparecer como `Sin dato`
- la UI sigue operativa, pero la semantica de categoria no es rica como en un catalogo completamente enriquecido

### 4. QA automatizado no confirmo la transicion SPA por click desde el catalogo

Estado:

- vigente como limite de tooling de QA

Impacto:

- no se marca como bug confirmado
- la ruta directa `/admin/spells/:spellId` si funciona
- el catalogo si expone links correctos a detalle

## Limitaciones tecnicas

### 1. Schema actual sin identidad por fila de effect

Impacto:

- cualquier write serio de effects implicaria reserializacion completa del payload

### 2. Schema actual sin `Id` por nivel en `spells_levels`

Impacto:

- Phase 4 resuelve write de levels con estrategia por orden runtime
- esto es suficiente para admin puntual, pero menos robusto que una identidad fuerte por fila

### 3. Warnings de decode no se pudieron validar con caso positivo

Impacto:

- la UI existe para mostrarlos
- el backend los soporta
- pero el caso QA muestreado no emitio warnings reales

## Limites de alcance aceptados

- no se toco base de datos
- no se toco cliente
- no se toco publicacion
- no se tocaron Items/Sets
- no se tocaron NPC, monstruos, glifos, D2O, D2I ni D2P
- no se implementaron APIs nuevas en Phase 10

## Lectura operativa

Estas limitaciones no invalidan el catalogo, el detalle, el editor de levels ni la auditoria read-only de effects. Si invalidan, en cambio, cualquier afirmacion de paridad total 1:1 con el Spell Builder legacy.
