# Spell Builder Production Parity - Special Effects Map

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`

## Objetivo

Mapear los efectos especiales que no deben tratarse como un CRUD plano de filas, para separar claramente lo que pertenece a Spell Builder de lo que pertenece al motor de combate.

## Fuentes revisadas

- `legacy-reference/Rollback.Admin/Services/SpellAdminService.cs`
- `legacy-reference/Rollback.Admin/Services/GameEffectEditorService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellClientPublishService.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Marks/GlyphSpawn.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Marks/TrapSpawn.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Summon/Summon.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/States/AddState.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/States/RemoveState.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Damages/LoseHpByUsingAP.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/Buffs/APBuff.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/Spells/SpellEffectHandler.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/Game/Effects/EffectManager.cs`
- `Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/SpellManager.cs`

## Hallazgos por categoria

### Glifos

- Runtime:
  - `GlyphSpawn` usa `Effect.DiceNum` como `linkedSpellId`.
  - `Effect.DiceFace` se usa como nivel del spell enlazado.
  - El glifo ejecuta `spell.Effects[0]` del spell interno.
- Legacy:
  - `SpellAdminService` ya tenia sincronizacion especial de payload persistente.
- Conclusión:
  - un glifo no debe editarse como una fila aislada sin considerar el spell persistente enlazado.

### Trampas

- Runtime:
  - `TrapSpawn` sigue el mismo patron de spell enlazado.
  - si la celda ya tiene trigger, aplica reglas especiales.
- Legacy:
  - tambien queda cubierto por sincronizacion persistente.
- Conclusión:
  - trampas y glifos comparten el mismo riesgo estructural.

### Venenos

- Evidencia local:
  - `LoseHpByUsingAP` aplica buff si `Duration > 0`, o daño directo si no la tiene.
- Conclusión:
  - el mismo effect puede cambiar de semantica segun payload.
  - requiere validacion, no solo un input libre.

### Buffs

- Evidencia local:
  - `APBuff` y otros handlers agregan `StatsBuff` o buffs dedicados.
- Conclusión:
  - para buffs simples el write es mas viable, pero sigue dependiendo de `Duration`, `TargetType`, zona y effect id correcto.

### Invocaciones

- Evidencia local:
  - `Summon` valida template monstruo, grado, slots de invocacion, bombas y esclavos.
  - algunos summons agregan estados automaticamente.
- Conclusión:
  - editar una fila de invocacion mal puede romper runtime aunque el serializer sea correcto.

### Estados

- Evidencia local:
  - `StatesRequiredCSV` y `StatesForbiddenCSV` viven fuera del bucket de effects.
  - `AddState` y `RemoveState` ademas usan handlers de effects para aplicar o quitar estados en combate.
- Conclusión:
  - hay dos niveles distintos:
    - condiciones de lanzamiento por CSV
    - aplicacion de estado como effect del combate

### Conditions

- Evidencia local de esta fase:
  - no se encontro un campo dedicado de criterios de spell equivalente a `StringCriterion` de items.
  - las condiciones auditadas para spells son `StatesRequiredCSV` y `StatesForbiddenCSV`.
- Conclusión:
  - cualquier condicion adicional observada en combate pertenece hoy a handlers, estados o reglas del motor, no a un campo productivo identificado en Spell Builder.

### Effects normales vs criticalEffects

- Legacy:
  - buckets separados en modelo, serializer, guardado y publicacion cliente.
- Runtime actual:
  - `SpellManager` resuelve `Effects` y `CriticalEffects` por separado.
- Conclusión:
  - nunca deben fusionarse en un solo editor o payload.

## Mapa resumido

| Categoria | Representacion hoy | Riesgo principal | Dueño natural |
| --- | --- | --- | --- |
| Glifos | effect contenedor + spell enlazado | payload persistente ambiguo | Backend + motor |
| Trampas | effect contenedor + spell enlazado | desalineacion del trigger real | Backend + motor |
| Venenos | effect con duration y handler especifico | semantica cambia por duration | Backend |
| Buffs | effect + handler/buff | parametros incompletos rompen combate | Backend |
| Invocaciones | effect con monsterId/grado + handler complejo | runtime invalido o summon roto | Motor de combate |
| Estados CSV | listas separadas en nivel | bloqueo de lanzamiento mal configurado | Backend |
| Estado como effect | row + handler `AddState` / `RemoveState` | state id mal resuelto | Backend + motor |
| Critical effects | bucket separado | mezclarlo con normales | Backend + Angular |

## Riesgos

### Riesgos cerrables en Spell Builder

- separar buckets normal / critico
- bloquear edicion de categorias especiales sin soporte
- mostrar warnings funcionales claros al usuario

### Riesgos que requieren backend

- detectar glifos / trampas y decidir si se sincronizan o se bloquean
- validar invocaciones y estados
- diferenciar filas simples de filas especiales

### Riesgos que requieren motor de combate

- correccion de handlers
- bugs de invocaciones, bombas, trigger marks o estados
- diferencias de semantica entre payload valido y resultado real en pelea

## Decisiones recomendadas

1. Clasificar cada fila de effect antes de habilitar su write.
2. Considerar glifos y trampas como categoria especial bloqueada hasta tener sync dedicado.
3. No mezclar condiciones de lanzamiento con effects del bucket normal.
4. Tratar invocaciones y algunos estados como frontera de macro separada si aparece comportamiento roto en combate.

## Que NO implementar todavia

- Soporte de write para glifos y trampas persistentes
- Write libre de invocaciones
- Cambios de motor de combate dentro de la macro de Spell Builder

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, incluyendo cualquier warning o bloqueo asociado a glifos, trampas, estados, invocaciones, venenos y buffs.

## Proxima fase recomendada

`Fase 2 o macro separada - Clasificacion de effects por seguridad de write`

Alcance recomendado:

- etiquetar filas simples vs especiales
- definir que queda dentro de Spell Builder
- extraer a macro aparte lo que sea netamente motor de combate
