# Fase 3: Reparación del pipeline de efectos

Correcciones genéricas del motor Sunshine por **capa**, sin hacks por hechizo/clase. Ver [combat-fix-philosophy.md](../combat-fix-philosophy.md).

## Metadatos

| Campo | Valor |
|-------|--------|
| Duración estimada | ~6 h |
| Rama feature | `feature/effects-engine-fix-phase3` |
| Rama test VPS | `develop-build` |
| Documentación | `docs/effects-engine-fix-phase3/` |
| Fases previas | [Fase 1](../effects-audit-phase1/) · [Fase 2](../effects-catalog-phase2/) |

## Índice

| Archivo | Contenido |
|---------|-----------|
| [root-cause-analysis.md](./root-cause-analysis.md) | Causa raíz por capa + bloques `auditoria:` vs Rollback |
| [architecture-changes.md](./architecture-changes.md) | Diff `.cs` tocados; secuencias pendientes en `develop` |
| [validation-checklist.md](./validation-checklist.md) | Checklist in-game por categoría + lectura logs VPS |

## Modelo de ramas

Ver [BRANCHING.md](../BRANCHING.md): `feature/*` → merge local `develop-compile` → test `develop-build` (VPS) → PR `develop`.

## Rutas base

| Módulo | Ruta |
|--------|------|
| **game** (Sunshine) | `Sunshine net11.0/Sunshine net11.0/Sunshine.WorldServer/` |
| **Rollback** (referencia) | `2.0.0_v1_old/2.0.0/Rollback/Rollback.World/` |
| **multi** (cliente) | `Client2.3.7/as2invoker/` |

## Commits (orden de revisión)

| # | Capa | Mensaje |
|---|------|---------|
| 0 | Docs | `docs(effects): scaffold Fase 3 engine-fix` |
| 1 | DOT / robo HP | `fix(effects): DOT robo HP via TriggerBuff cuando Duration gt 0` |
| 2 | Muerte instantánea | `fix(effects): registrar handler generico Effect_Kill` |
| 3 | Castigos | `fix(effects): castigo reactivo AfterDamaged en PunishmentBuff` |
| 4 | Invocaciones | `fix(effects): invocaciones genericas Die y fin de turno` |
| 5 | Bosses Frigost | *(opcional — documentar si no entra)* |
| 6 | Diagnóstico | `feat(fights): FightCombatLogger para develop-build` |
| 7 | Cierre | `docs(effects): cerrar Fase 3 root-cause architecture validation` |

## Criterios de aceptación

- [ ] Causa raíz documentada por capa
- [ ] Sin `if (spellId == …)` en commits de fix
- [ ] Compila en VPS `develop-build`
- [ ] Checklist in-game + logs de pelea
- [ ] PR a `develop` con tabla commit → prueba

## Validación VPS

Registro: [20260530-develop-build-phase3-c646296.md](../vps-build-validation/20260530-develop-build-phase3-c646296.md)

## Alcance explícito

**Permitido:** handlers, buffs, triggers, `FightActor`, logger de combate en build test.

**Prohibido:** parches Golosón / Cil / Sacrógrito por ID; port completo `ActiveSequenceCount`/`ReadyChecker` a `develop` en este PR.
