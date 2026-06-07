# Fase 2: Clasificación de efectos

Catálogo taxonómico del motor Sunshine, preparación para correcciones por **capa** (ver [combat-fix-philosophy.md](../combat-fix-philosophy.md)).

## Metadatos

| Campo | Valor |
|-------|--------|
| Duración estimada | ~3 h |
| Rama | `feature/effects-catalog-phase2` |
| Documentación | `docs/effects-catalog-phase2/` |
| Fase anterior | [effects-audit-phase1](../effects-audit-phase1/) |
| Validación build | [vps-build-validation](../vps-build-validation/20260605-develop-build-4d12fde.md) (`develop-build`) |

## Índice

| Archivo | Contenido |
|---------|-----------|
| [effect-categories.md](./effect-categories.md) | Taxonomía: Directos, Estados, Buffs, Triggers, Invocaciones, Mecánicas |
| [effect-id-mapping.md](./effect-id-mapping.md) | `EffectsEnum` ↔ handler ↔ categoría ↔ gap Rollback |
| [execution-pipeline.md](./execution-pipeline.md) | Pipeline cast → handler → buff → trigger → mapa |

## Checklist categorías (brief)

- [x] Directos — Daño, Curas, Robo
- [x] Estados — Invisibilidad, Pesado, Indesplazable
- [x] Buffs — Stats, AP, MP
- [x] Triggers — Al recibir daño, inicio/fin turno
- [x] Invocaciones — Summon, Doble, Árbol, Sacrificada, Hinchable
- [x] Mecánicas especiales — Bosses, Invulnerabilidades, Casillas especiales

## Convención módulos

- **game** — `Sunshine.WorldServer` (handlers, buffs, fights)
- **multi** — `Client2.3.7/as2invoker/.../fight/` (solo verificación post-fix servidor)

## Alcance

- **Solo documentación** en Fase 2
- **Prohibido** parchear handlers o `Fight.cs` en esta rama
- Implementación: fases posteriores, una **capa** a la vez

## Prioridad implementación (tras catálogo)

1. DOT / robo HP (`HpSteal` + `TriggerBuff`) — afecta Cil y venenos
2. Castigos (`PunishmentBuff` / `PunishmentDamage`)
3. `Effect_Kill` handler
4. Secuencias combate
5. IA bosses / invocaciones suicidas
