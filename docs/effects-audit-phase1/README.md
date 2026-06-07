# Fase 1: Auditoría de efectos y combate

Índice de la auditoría comparativa **Sunshine (servidor actual)** vs **Rollback.World (referencia funcional)**. Esta fase es **solo documentación**; no se modificó código de combate.

## Documentos

| Archivo | Contenido |
|---------|-----------|
| [effect-engine-overview.md](./effect-engine-overview.md) | Arquitectura del motor de efectos, cast, buffs, triggers y hilos de combate |
| [rollback-vs-current-diff.md](./rollback-vs-current-diff.md) | Mapeo de clases, dispatchers y gaps handler por handler |
| [affected-systems.md](./affected-systems.md) | Matriz de sistemas afectados (8 síntomas) con rutas **game** / **multi** |

## Modelo de ramas (Fase 1 y siguientes)

| Rama | Rol |
|------|-----|
| `main` | Producción / línea estable remota |
| **`develop`** | Rama secundaria de **integración y desarrollo** — aquí se fusionan las features (launcher, VPS, combate, etc.) |
| **`feature/effects-audit-phase1`** | Estudio Fase 1: solo documentación en `docs/effects-audit-phase1/` (sin parches de combate) |

Flujo recomendado:

1. Trabajar auditoría y docs en `feature/effects-audit-phase1`.
2. Cuando el estudio esté listo: `git merge feature/effects-audit-phase1` en `develop`.
3. Subir features futuras a `develop`; `main` solo cuando el equipo acuerde release.

No mezclar commits del launcher (`yaco` / `Yaco`) con esta feature sin revisión explícita.

## Estado Git (pre-requisito)

| Campo | Valor |
|-------|--------|
| Rama de esta fase | `feature/effects-audit-phase1` |
| Rama de desarrollo | `develop` (misma base que la feature antes del commit de docs) |
| Sunshine en workspace | `Sunshine.csproj` + handlers bajo `Game/Effects/Spells/` (auditoría **game**) |
| Build local .NET | No requerido para Fase 1; despliegue Sunshine en **VPS** (Docker SDK 11) |

## Rutas base de la auditoría

| Rol | Ruta |
|-----|------|
| Servidor actual (Sunshine) | `c:\Dofus\2.0.0\Sunshine net11.0\Sunshine net11.0\Sunshine.WorldServer\` |
| Origen canónico (GitHub) | [Sunshine.WorldServer/Game](https://github.com/dagemov/DofusLegacy2.3.7/tree/main/Sunshine%20net11.0/Sunshine%20net11.0/Sunshine.WorldServer/Game) |
| Referencia Rollback | `C:\Dofus\2.0.0_v1_old\2.0.0\Rollback\Rollback.World\` |
| Docker build context | `c:\Dofus\2.0.0\docker\Dockerfile` → `COPY Sunshine net11.0/Sunshine net11.0/` |

### Sunshine en el repo unificado

- Tras el merge de `pr/rollblack-landing-cms`, el árbol **Sunshine net11.0** completo viene versionado en este repositorio (ya no hace falta el worktree `2.0.0-pr-landing`).
- `Sunshine.csproj` y `Sunshine.WorldServer/Game/Effects/` están en [DofusLegacy2.3.7](https://github.com/dagemov/DofusLegacy2.3.7).
- **Build local:** `dotnet build` falla en esta máquina (SDK 8.0; proyecto target **net11.0**). El **Dockerfile** usa `mcr.microsoft.com/dotnet/sdk:11.0-preview` — la auditoría de código no depende del build local.

## Resumen ejecutivo

| Métrica | Rollback | Sunshine (actual) |
|---------|----------|-------------------|
| Archivos handler de hechizo | 38 clases (`Handlers/Spells/`) | 55 archivos bajo `Game/Effects/Spells/` |
| Atributos de registro | ~38 `[Identifier(EffectId)]` | ~76 `[EffectHandler(EffectsEnum)]` (varios efectos por clase) |
| `Fight.cs` (líneas) | 789 | 1055 |
| `EffectManager.cs` | 523 (dispatch + handlers + zonas) | 227 (serialización + zonas; registro en `EffectsLoader`) |
| Clase `FightEffects` | No existe | No existe |
| `ActiveSequenceCount` / `ReadyChecker` | Sí | **No** |

### Top 5 riesgos (prioridad Fase 2)

1. **Venenos / DOT por turno** — Rollback usa `TriggerBuff` si `Duration != 0` en robo de vida; Sunshine aplica daño **instantáneo** siempre (`HpSteal.cs`).
2. **Castigos Sacrógrito** — Rollback: buff reactivo `AfterDamaged`; Sunshine: mezcla `PunishmentBoost`, `PunishmentBuff.OnDamaged` y `PunishmentDamage` con curva simplificada.
3. **Muerte instantánea (`Effect_Kill`)** — Rollback: `KillEffectHandler`; Sunshine: **sin handler** registrado (solo `case` en `StatsBoost`).
4. **Secuencias de combate** — Rollback: `ActiveSequenceCount`, `ReadyChecker`, `FightTelemetry`; Sunshine: ausente → riesgo de turnos colgados / mensajes fuera de orden.
5. **IA de bosses** — Arquitecturas distintas (`Game/Fights/AI/Brain` vs `Game/Actors/AI/` + `FrigostBossMechanics`).

## Convención de módulos

- **game** — `Sunshine.WorldServer` / `Rollback.World` (autoridad de combate).
- **multi** — Cliente `Client2.3.7` y launcher (sincronización visual; referencia secundaria en esta fase).

## Alcance explícito

- Auditar solo fuentes bajo `src` / proyectos `.cs` del servidor.
- **Prohibido** en Fase 1: parches en handlers, buffs o `Fight.cs`.
