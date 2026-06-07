# Spell Builder - Next Steps

Fecha: `2026-06-07`
Estado actual: `Macro 5 cerrada con decision PARTIAL`

## Prioridad 1 - Definir write de effects

Objetivo:

- decidir si el sistema va a reserializar payload hex
- decidir como preservar o reemplazar fallback binario legacy
- definir reglas para rows no soportadas o con warnings de decode

Entregable recomendado:

- ADR o documento tecnico de estrategia de write de effects

## Prioridad 2 - Exponer contrato backend de effects write

Objetivo:

- agregar endpoints reales solo despues de resolver la estrategia de serializacion

Incluye:

- request DTO
- validaciones
- estrategia de rollback
- warnings de preservacion

## Prioridad 3 - Revalidar con referencia sana disponible

Objetivo:

- repetir QA cuando el entorno tenga `referenceAvailable = true`

Minimo esperado:

- validar metadata de referencia visible
- validar `referenceRows`
- validar diferencias runtime vs referencia con caso real

## Prioridad 4 - QA manual o semiautomatico de navegacion SPA

Objetivo:

- reconfirmar que el click desde catalogo abre detalle en un navegador interactivo normal

Motivo:

- la ruta directa funciona
- los links existen
- el runtime automatizado usado en Phase 10 no confirmo ese click

## Prioridad 5 - Evaluar si Macro 5 debe reabrirse

Reabrir solo si:

- se exige paridad total con legacy
- se exige editor real de effects
- se exige validacion con referencia sana presente

No reabrir si:

- el objetivo inmediato es consumir el catalogo, detalle, levels y auditoria de effects read-only
- el write de effects queda expresamente fuera del release actual
