# Spell Builder Production Parity - Effects Write Strategy

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`

## Objetivo

Definir la estrategia segura para habilitar escritura real de `effects` y `criticalEffects` sin editar payloads a mano, sin romper el runtime y sin inventar serializers.

## Fuentes revisadas

- `legacy-reference/Rollback.Admin/Services/GameEffectEditorService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminSchemaService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellClientPublishService.cs`
- `legacy-reference/Rollback.Web/Components/Admin/EffectListEditor.razor`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Spells/SpellEffectsDecoder.cs`
- `Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/Spells/SpellsAdminReadRepository.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/EffectManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/SpellManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/World/Spells/SpellTemplate.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Marks/GlyphSpawn.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Marks/TrapSpawn.cs`

## Hallazgos

### 1. Legacy hacia round-trip real de effects

- `GameEffectEditorService.Deserialize(byte[])` convertia `BinaryEffects` o `BinaryCriticalEffects` a `GameEffectEditRow`.
- `GameEffectEditorService.Serialize(IEnumerable<GameEffectEditRow>)` reconstruia el payload binario.
- `SpellAdminSchemaService.ApplyLevel(...)` escribia:
  - `BinaryEffects`
  - `BinaryCriticalEffects`
  - `StatesRequiredCSV`
  - `StatesForbiddenCSV`
- El modelo legacy mantenia `Effects` y `CriticalEffects` como colecciones distintas.

### 2. Legacy no resolvia todo con un simple serializer

- `SpellAdminService` tenia logica especial para zonas persistentes.
- Si un spell tenia links de glifo o trampa, el servicio hacia sincronizacion adicional hacia el spell persistente interno.
- Eso prueba que el problema no es solo serializar filas: algunos payloads requieren reglas de negocio adicionales.

### 3. El runtime actual soporta dos representaciones

- Formato actual:
  - `Effects`
  - `CriticalEffects`
  - string hex serializado
- Fallback legacy:
  - `BinaryEffect`
  - `BinaryEffects`
  - `BinaryCriticalEffect`
  - `BinaryCriticalEffects`
- `SpellManager` prioriza el formato string actual y cae al binario solo si el string no existe.

### 4. El decoder actual del Admin solo lee

- `SpellEffectsDecoder` decodifica:
  - hex serializado actual
  - binario legacy
- Hoy no existe el paso inverso productivo en el stack Angular/API actual.

### 5. La publicacion cliente legacy tenia whitelist parcial

- `SpellClientPublishService` solo podia reconstruir expresiones cliente para un subconjunto controlado de effects.
- Si un effect no estaba soportado para tooltip cliente, el servicio intentaba conservar expresiones existentes.
- Eso confirma que no toda fila editable en runtime tiene publicacion cliente trivial.

## Estrategia recomendada

### Fase A - Cierre de contrato y preservacion

- Mantener `effects` y `criticalEffects` separados en API, dominio y UI.
- Introducir un write model que reciba filas estructuradas, no payload crudo.
- Guardar tambien metadatos de preservacion:
  - formato origen
  - source bucket
  - warnings de decode
  - flag de special handling requerido

### Fase B - Round-trip backend controlado

- Implementar serializer backend, no serializer en Angular.
- Soportar explicitamente:
  - formato serializado actual
  - formato binario legacy
- Rechazar en validacion cualquier fila que no pueda reconstruirse con seguridad.
- Bloquear el guardado si el nivel contiene un caso especial no soportado.

### Fase C - Casos especiales antes del editor final

- Glifos y trampas:
  - detectar links persistentes
  - impedir guardado parcial ambiguo
  - definir si el write actualizara tambien el spell persistente enlazado
- Estados:
  - mantener `StatesRequired` / `StatesForbidden` fuera del payload de effects
  - no mezclar reglas de lanzamiento con filas del bucket normal
- Invocaciones:
  - validar `monsterId`, grado y casos especiales de bombas / esclavos
- Venenos y buffs:
  - permitir edicion solo si el handler actual soporta la semantica sin cambio de motor

### Fase D - Editor Angular despues del backend

- Angular solo debe exponer filas que el backend marque como editables.
- La UI debe mostrar:
  - tipo de fila
  - bucket normal o critico
  - warning de caso especial
  - motivo de bloqueo si aplica
- Todo texto visible debe quedar 100% en español.

## Matriz de seguridad recomendada

| Categoria | Write recomendado | Estado recomendado |
| --- | --- | --- |
| Integer / Dice simples | Si, con serializer backend | Candidato temprano |
| Estados CSV | Si, por campos separados | Candidato temprano |
| Buffs estadisticos simples | Si, si el handler ya existe y no requiere sincronizacion extra | Candidato temprano |
| Venenos por buff conocido | Si, con validacion de duration / value | Candidato medio |
| Invocaciones | No hasta validar semantica runtime especifica | Bloqueado |
| Glifos / trampas persistentes | No hasta cerrar sincronizacion enlazada | Bloqueado |
| Filas sin round-trip determinista | No | Bloqueado |
| Publicacion tooltip cliente no soportada | No bloquear runtime write por si solo, pero si bloquear publish cliente automatico | Bloqueo parcial |

## Riesgos

### Riesgos de backend

- Perder el bucket original o mezclar normal con critico.
- Reescribir un formato cuando el runtime estaba leyendo el otro.
- Aceptar filas que el serializer no puede devolver de forma estable.

### Riesgos de round-trip

- Diferencias de orden de filas cambian el comportamiento del spell.
- Campos como `Random`, `Duration`, `TargetType`, `Shape` o `ZoneSize` pueden perderse si no se preservan completos.
- El formato legacy binario tiene `serializationId`; no todos los casos deben reserializarse a ciegas.

### Riesgos de producto

- Un editor visual sin regla de bloqueo clara induce a guardar cambios peligrosos.
- Un usuario final no debe ver ni editar hex, blobs o detalles de serializer.

## Decisiones recomendadas

1. El write de effects debe vivir solo en backend.
2. Angular no debe conocer payloads crudos.
3. Los casos especiales de glifos y trampas deben tener tratamiento dedicado o bloqueo duro.
4. `effects` y `criticalEffects` deben guardar y validarse por separado.
5. La primera version productiva no debe prometer cobertura total del catalogo legacy si el runtime especial no esta cerrado.

## Que NO implementar todavia

- Editor Angular libre para cualquier tipo de fila
- Envio de payload hex/binario desde frontend
- Write de glifos persistentes sin plan enlazado
- Publicacion cliente automatica basada en un effect write no probado

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, tambien en las pantallas de edicion, warnings, bloqueos, validaciones y confirmaciones de save.

## Proxima fase recomendada

`Fase 2 - Backend Round-Trip Contract Audit`

Alcance recomendado:

- definir DTO write de effects
- fijar reglas de preservacion de formato
- separar casos soportados y bloqueados
- preparar bateria de pruebas de round-trip antes de exponer editor Angular
