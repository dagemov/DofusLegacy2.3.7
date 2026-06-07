# Spell Builder Production Parity - Effects Editor Safety Gates

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`
Estado: `SPEC_ONLY`

## Objetivo

Definir los safety gates que deben cumplirse antes de mostrar cualquier editor real de `effects` o `criticalEffects` en Angular.

## Fuentes revisadas

- `docs/admin-tools/spell-builder/spell-builder-effects-write-closure-spec.md`
- `docs/admin-tools/spell-builder/spell-builder-phase9-effects-editor.md`
- `docs/admin-tools/spell-builder/spell-builder-special-effects-map.md`
- `legacy-reference/Rollback.Web/Pages/Admin/Spells.razor`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.html`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/spells/spell-detail-page.component.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Contracts/Spells/SpellEffectRowDto.cs`

## Hallazgos

- La UI actual ya muestra el bloqueo explícito porque no existe write seguro.
- Legacy editaba spells forzando formato `Dice`.
- La fase siguiente no debe transformar el bloqueo en un editor libre; debe pasar por gates estrictos.

## Gating general

El editor real solo puede habilitarse si:

1. el backend expone preview/validate
2. el bucket es editable según la spec de cierre
3. la fila pertenece al subset soportado
4. no hay warnings bloqueantes
5. el diff semántico puede mostrarse sin payload crudo
6. existe plan de backup y rollback

## Gating por pantalla

### 1. Acceso a modo edición

Debe bloquearse si:

- el nivel no existe en runtime
- el bucket runtime no existe
- el decode devuelve warning bloqueante
- el formato fuente no es reconocido
- el bucket contiene glifos, trampas o invocaciones
- el bucket contiene rows no soportadas

### 2. Edición de filas

La UI debe permitir inicialmente solo:

- agregar fila `Dice` soportada
- editar fila `Dice` soportada
- reordenar filas soportadas
- eliminar fila soportada

La UI no debe permitir inicialmente:

- editar rows especiales
- editar payload crudo
- editar `serializationId`
- fusionar normal y crítico
- cambiar formato fuente

### 3. Preview antes de guardar

La UI debe obligar a ejecutar preview antes de permitir confirmar write.

El preview debe mostrar:

- bucket afectado
- filas agregadas
- filas eliminadas
- filas modificadas
- cambios por campo
- warnings no bloqueantes
- backup requerido

### 4. Confirmación final

La confirmación final debe requerir:

- preview exitoso
- ausencia de errores bloqueantes
- ausencia de conflicto de versión
- confirmación explícita del usuario

## Gating por categoría

| Categoría | Estado inicial | Motivo |
| --- | --- | --- |
| Dice simple | Editable | subset seguro |
| Integer legacy puro | Bloqueado | legacy spell editor no lo usaba como modo final |
| Base | Bloqueado | reconstrucción ambigua |
| Glifos | Bloqueado | sync persistente requerido |
| Trampas | Bloqueado | sync persistente requerido |
| Venenos | Bloqueado al inicio | semántica depende de duration |
| Buffs simples | Candidato futuro | requiere validación por handler |
| Invocaciones | Bloqueado | depende de monster template y lógica runtime |
| Estado como effect | Bloqueado al inicio | riesgo de motor |
| Conditions | Fuera de este editor | no son bucket de effects verificado |

## Mensajes que Angular debe mostrar

Todos los textos visibles deben quedar en español. El editor no debe reutilizar textos legacy o del estado actual en inglés.

Mensajes mínimos requeridos:

- bloqueo por categoría especial
- bloqueo por warning de decode
- bloqueo por formato no soportado
- bloqueo por conflicto de versión
- resultado de preview
- confirmación de backup lógico
- confirmación de rollback disponible

## Diff mínimo que debe ver el usuario

Por cada fila:

- índice visual
- nombre del effect
- bucket normal o crítico
- antes y después de:
  - valor
  - mínimo
  - máximo
  - delay
  - random
  - duration
  - targetType
  - zoneShape
  - zoneMinSize
  - zoneSize

No debe mostrarse:

- hex
- blob
- bytes
- `serializationId`

## Errores bloqueantes visibles en UI

- `Formato runtime no soportado para edición segura.`
- `El decode devolvió warnings bloqueantes; este bucket queda en solo lectura.`
- `Este nivel contiene glifos o trampas persistentes y requiere flujo dedicado.`
- `Este nivel contiene invocaciones o effects especiales fuera del subset inicial.`
- `El preview no pasó round-trip; no se puede guardar.`
- `El nivel cambió mientras estabas editando; recarga antes de continuar.`

## Warnings permitidos en UI

- `No hay referencia sana disponible para comparar este nivel.`
- `La publicación cliente de spells sigue fuera de alcance en esta fase.`
- `El tooltip cliente legacy no tiene garantía de reconstrucción total para este effect.`

## Riesgos

### Riesgos bloqueantes

- abrir el editor antes del preview backend
- dejar que el usuario edite filas no soportadas
- mostrar diff insuficiente antes del guardado

### Riesgos no bloqueantes

- deuda de copy en español
- ausencia de referencia en entorno local

## Decisiones recomendadas

1. El editor Angular debe nacer detrás de preview/validate, no delante.
2. La primera UX de edición debe ser conservadora y explícita.
3. La UI debe seguir mostrando un guard read-only completo para categorías bloqueadas.
4. Todo texto final visible debe quedar en español antes de liberar el editor a usuario final.

## Qué NO implementar todavía

- editor libre multi-kind
- save directo sin preview
- confirmación sin diff
- habilitación de glifos, trampas o invocaciones

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, incluyendo títulos, botones, labels, placeholders, errores, warnings, tooltips, confirmaciones y pantallas de preview.

## Criterio de paso a Fase 3

Fase 3 solo puede iniciar si el equipo acepta estos safety gates como reglas de producto y confirma que el editor inicial quedará limitado a `Dice-only` con preview obligatorio.

## Próxima fase recomendada

`Fase 3 - Preview backend + UX de diff para Dice-only`
