# Filosofía de corrección de combate

Criterio del equipo para Fase 2+ (implementación). No parchear hechizos o clases de jugador de forma aislada.

## Incorrecto

- Arreglar **Golosón** directamente (hechizo / invocación Sadida)
- Arreglar **Cil** directamente (veneno / DOT)
- Arreglar **Sacrógrito** directamente (castigos por clase)

Estos enfoques duplican lógica, rompen otros hechizos que comparten la misma capa y no escalan.

## Correcto

1. **Encontrar qué capa del motor provoca el fallo**
2. **Corregir la capa** (handler, buff, trigger, secuencia de combate)
3. **Validar todos los casos afectados** de esa categoría — no solo el hechizo que reportó el bug

## Mapeo síntoma → capa (desde Fase 1)

| Ejemplo jugador | Capa del motor | Archivos Sunshine (**game**) | Otros casos que heredan el fix |
|-----------------|----------------|------------------------------|--------------------------------|
| Golosón / invocaciones suicidas | Invocación + IA + `Die()` | `Effects/Spells/Summon/Summon.cs`, `Actors/Fighters/SummonedMonster.cs` | Bloqueadora, Loca, bombas Sadida, doubles |
| Cil / veneno sin tick | DOT por `Duration` + `TriggerBuff` | `Damages/HpSteal.cs`, `Fights/Buffs/Customs/TriggerBuff.cs` | Robo de vida multi-turno, glifos veneno |
| Sacrógrito / castigos | Buff reactivo `AfterDamaged` | `Buffs/Spells/PunishmentBuff.cs`, `Damages/PunishmentDamage.cs`, `FightActor.InflictDamage` | Todos los efectos `Effect_Punishment` y daño % castigo |
| Glifos de muerte | Handler `Effect_Kill` ausente | Gap: registrar handler como Rollback `KillEffectHandler` | Cualquier glifo/trampa con muerte instantánea |
| Turnos colgados | Secuencias de combate | Gap: `ActiveSequenceCount` / `ReadyChecker` (Rollback) | Empuje + glifo + muerte en misma cadena |

## Validación

- Probar en **`develop-build`** en VPS (puertos 2450/5557) antes de PR a `develop`
- Checklist por **categoría** en [effects-catalog-phase2/effect-categories.md](./effects-catalog-phase2/effect-categories.md)
- Referencia diff Rollback: [effects-audit-phase1/affected-systems.md](./effects-audit-phase1/affected-systems.md)

## Módulos

- **game** — única autoridad para correcciones de daño, buffs y triggers
- **multi** — solo si el servidor ya es correcto y persiste desync visual (cliente AS2)
