# Spell Builder Production Parity - Effects Roundtrip Test Plan

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`
Estado: `PLAN_ONLY`

## Objetivo

Definir el plan mínimo de pruebas que debe pasar cualquier futura implementación de write de `effects` y `criticalEffects` antes de habilitar un editor real.

## Fuentes revisadas

- `docs/admin-tools/spell-builder/spell-builder-effects-write-closure-spec.md`
- `docs/admin-tools/spell-builder/spell-builder-phase5-effects-api.md`
- `docs/admin-tools/spell-builder/spell-builder-phase9-effects-editor.md`
- `legacy-reference/Rollback.Admin/Services/GameEffectEditorService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminService.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/SpellEffectsDecoder.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/EffectManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/SpellManager.cs`

## Hallazgos

- La seguridad del write depende de round-trip completo por bucket.
- El runtime actual soporta `current-serialized-hex` y `legacy-binary`.
- Legacy forzaba `Dice` para spells.
- Existen warnings de decode que no deben ignorarse.

## Alcance del plan

Este plan no ejecuta pruebas destructivas en esta fase. Solo define qué debe probarse y con qué criterio de aprobación.

## Suite mínima obligatoria

### Grupo A - Decode de runtime actual

1. `decode_current_hex_empty_container`
   - entrada: bucket `0000` o equivalente vacío verificado
   - esperado: cero filas, cero errores bloqueantes

2. `decode_current_hex_single_dice_row`
   - entrada: un payload actual con una fila `Dice`
   - esperado: todos los campos leídos correctamente

3. `decode_current_hex_multiple_rows_order_preserved`
   - entrada: varias filas
   - esperado: orden estable

### Grupo B - Decode de fallback legacy

4. `decode_legacy_binary_serialization_1`
   - esperado: fila `Base` auditada y marcada no editable si aplica

5. `decode_legacy_binary_serialization_4`
   - esperado: fila `Dice`

6. `decode_legacy_binary_serialization_6`
   - esperado: fila `Integer`

7. `decode_legacy_binary_unknown_serialization_id`
   - esperado: warning bloqueante y bucket no editable

### Grupo C - Roundtrip de subset soportado

8. `roundtrip_current_hex_dice_single_row`
   - secuencia: decode -> encode -> decode
   - esperado: equivalencia semántica total

9. `roundtrip_current_hex_dice_multiple_rows`
   - esperado: orden y valores preservados

10. `roundtrip_legacy_binary_dice_single_row`
    - esperado: equivalencia semántica total en formato legacy

11. `roundtrip_legacy_binary_dice_multiple_rows`
    - esperado: equivalencia semántica total en formato legacy

### Grupo D - Buckets separados

12. `roundtrip_normal_effects_only`
    - esperado: `criticalEffects` intacto

13. `roundtrip_critical_effects_only`
    - esperado: `effects` intacto

14. `roundtrip_both_buckets_no_cross_contamination`
    - esperado: ningún row cambia de bucket

### Grupo E - Safety gates

15. `block_glyph_linked_payload`
    - esperado: save bloqueado

16. `block_trap_linked_payload`
    - esperado: save bloqueado

17. `block_summon_payload`
    - esperado: save bloqueado

18. `block_decode_warning_payload`
    - esperado: save bloqueado

19. `block_format_migration_attempt`
    - esperado: save bloqueado

20. `block_bucket_version_conflict`
    - esperado: save bloqueado

### Grupo F - Preview y diff

21. `preview_no_changes_returns_cansave_false_or_noop`
    - esperado: sin write necesario

22. `preview_single_field_change_reports_semantic_diff`
    - esperado: diff humano por fila y campo

23. `preview_preserves_fallback_metadata`
    - esperado: la respuesta informa si existe fallback preservado

### Grupo G - Backup y rollback

24. `backup_plan_generated_before_write`
    - esperado: referencia de backup lógico lista

25. `rollback_restores_original_bucket`
    - esperado: payload original restaurable

## Criterio de comparación semántica

La comparación entre `decode(original)` y `decode(reencodeado)` debe validar:

- mismo bucket
- misma cantidad de filas
- mismo orden
- mismo `effectId`
- mismos `value`, `minValue`, `maxValue`
- mismo `delay`
- mismo `random`
- mismo `duration`
- mismo `targetType`
- mismo `zoneShape`
- mismo `zoneMinSize`
- mismo `zoneSize`

No es obligatorio en primera fase de write que el payload textual bruto sea idéntico byte a byte si la semántica es igual y el formato sigue siendo el mismo. Sí es obligatorio que el bucket resultante vuelva a decodificar sin warnings bloqueantes.

## Criterio de aprobación por grupo

| Grupo | Bloquea Fase 3 si falla | Motivo |
| --- | --- | --- |
| A | Sí | Sin decode confiable no existe write seguro |
| B | Sí | El fallback legacy no puede tratarse a ciegas |
| C | Sí | El roundtrip es el núcleo de la seguridad |
| D | Sí | No se pueden mezclar buckets |
| E | Sí | Los safety gates son obligatorios |
| F | Sí | El usuario final necesita preview semántico |
| G | Sí | No se debe escribir sin backup y rollback |

## Estrategia de laboratorio no destructivo

Si en una fase posterior se necesita un lab técnico, debe vivir en:

- `Infrastructure/temporal-artifacts/spell-effects-roundtrip-lab/`

Reglas del lab:

- leer snapshots de payload
- decodificar
- reencodificar en memoria o artifacts temporales
- no tocar base de datos real
- no tocar payloads reales de producción

## Riesgos

### Riesgos bloqueantes

- no disponer de datasets representativos por formato
- hallar payloads especiales dentro del subset aparentemente simple
- diferencias de orden de fila entre decode y encode

### Riesgos no bloqueantes

- ausencia de referencia sana en el entorno local
- ausencia de spell sample para algunos grupos especiales, si igualmente quedan bloqueados en primera entrega

## Decisiones recomendadas

1. No abrir Fase 3 sin automatizar al menos los grupos A, B, C, D y E.
2. Mantener un subconjunto inicial pequeño si eso permite una suite de roundtrip totalmente cerrada.
3. Separar los casos de motor especial del primer lote de write.

## Qué NO implementar todavía

- pruebas destructivas contra DB real
- publish cliente real
- lab que modifique payloads reales

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, incluido el resultado visible de preview, diff, errores, warnings y rollback.

## Criterio de paso a Fase 3

Fase 3 solo puede arrancar si el owner acepta este plan como criterio de validación mínimo y confirma que la primera cobertura será `Dice-only`, sin glifos, trampas ni invocaciones.

## Próxima fase recomendada

`Fase 3 - Implementar preview/validación backend para subset Dice-only`
