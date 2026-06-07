# Changelog — Integración del parche del compañero en `devp`

Integración de las mejoras de `Parche_src_emu` (origen .NET 9) en la rama
`feature/patch-integration` sobre **.NET 11 preview**. Solo se portó lógica de
juego (`src` del game); no se tocó el launcher, el `Dockerfile`, `global.json`
ni el target del `.csproj`.

## Gate de compilación (build-gate)

Cada commit se validó con el build real de Docker en la VPS antes de
commitear, vía `scripts/sync-build-gate.ps1`:

```
docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml build sunshine
```

| Campo | Valor |
|-------|-------|
| VPS | `174.138.35.107` |
| Path build | `/opt/dofus-2.0.0-build` |
| Imagen | `sunshine-emu-sunshine:latest` |
| Rama | `feature/patch-integration` (desde `devp`) |

Resultado del gate en **todos** los commits: **Image sunshine-emu-sunshine Built (OK)**.

## Guardarraíles aplicados (legado preservado)

- Se conserva `FightCombatLogger.cs` y sus hooks en `FightActor`/`Fight`.
- Se conserva `LearnAllSpellsCommand.cs` y `LearnAllAvailableSpellsForQa()` en
  `SpellInventory.cs` (baseline QA fijado en commit previo a la integración).
- En archivos multi-feature (`FightActor.cs`, `Fight.cs`) se aplicaron solo los
  hunks de cada commit, no el archivo entero.
- No se adoptó el cambio del parche que convertía toda invocación jugable en
  `SlaveFighter` (rompía el comportamiento 2.x); se preservó la tipología
  existente (Bomb/Slave/Static/Monster).
- `Effect_Kill` se reconcilió ampliando nuestro `Kill.cs` en lugar de añadir el
  `DirectKill` del parche, evitando handlers duplicados.

## Migraciones de BD (deltas incrementales)

Se extrajeron como deltas (no el dump completo). Cada `.sql` viaja en el mismo
commit que su código y el servidor además las crea en runtime (bootstrap
idempotente):

- `database/migrations/2026-06-07_01_add_vip_to_accounts.sql` — columna `Vip`
  en `accounts` (commit VIP). Runtime: `AccountVipBootstrap`.
- `database/migrations/2026-06-07_02_create_characters_dopeul_cooldown.sql` —
  tabla `characters_dopeul_cooldown` (commit Dopeul). Runtime:
  `CharacterDopeulBootstrap`.

## Detalle por commit (orden cronológico)

| # | Commit | Tipo | Resumen | Build-gate |
|---|--------|------|---------|-----------|
| 0 | `41d6cd4` | chore(qa) | Fijar baseline LearnAll (preservación pre-integración) | OK |
| 1 | `1864886` | feat(combat) | Limitar usos de arma por turno (1; dagas 2) | OK |
| 2 | `36d0524` | fix(combat) | Placaje (tackle) al salir de zona de control | OK |
| 3 | `ee43f5a` | fix(combat) | Mostrar variación de PA/PM al propio lanzador | OK |
| 4 | `c4664ba` | feat(rates) | Ajustar fórmula de XP y drops de combate | OK |
| 5 | `1c5dfc6` | feat(items) | Usar pergaminos de característica por doble clic | OK |
| 6 | `64ebe26` | feat(ai) | IA de monstruos: búsqueda activa y desplazamiento | OK |
| 7 | `11414d3` | feat(vip) | Sistema VIP con beneficios x2 (+ migración + bootstrap) | OK |
| 8 | `a681c00` | feat(jobs) | Oficios 3+3 con especializaciones a nivel 61 | OK |
| 9 | `e44b071` | feat(effects) | Handlers faltantes: APSteal, StealKamas, RevealsInvisible, Dodge | OK |
| 10 | `16c4a68` | feat(combat) | DOT/HOT, SacrificeDamage y muerte directa (reconciliación) | OK |
| 11 | `2d18d32` | fix(summon) | Reubicar invocaciones a celda libre + registrar Effect_185 | OK |
| 12 | `f76f1d0` | chore(build-gate) | `scp -O` con ruta remota entrecomillada (tooling) | OK |
| 13 | `9810040` | feat(dopeul) | Combates contra Dopeul con recompensa de Doplones (+ migración + bootstrap) | OK |
| 14 | `6ff848e` | i18n(commands) | Traducir mensajes de comandos FR→ES | OK |

### Notas por feature

- **Armas por turno**: una arma por turno (dagas dos), alineado con balance del
  parche.
- **Placaje (tackle)**: corrige el cálculo al abandonar la zona de control en PvP.
- **Visual PA/PM**: el lanzador ve la variación de sus propios PA/PM.
- **Rates**: fórmula de XP y probabilidad/cantidad de drops de combate.
- **Pergaminos**: doble clic aplica el pergamino de característica.
- **IA de monstruos**: los monstruos se desplazan y buscan objetivo activamente.
- **VIP**: x2 XP, x2 kamas, x2 drop, +50% XP de oficio y x2 cantidad de recolección.
  Comandos `.add vip <jugador>` (admin) y `.vip` (jugador).
- **Oficios 3+3**: hasta 3 oficios base y 3 especializaciones; la especialización
  requiere oficio base nivel 61+. Comando `.oficio`.
- **Handlers de efectos**: robo de PA, robo de kamas, revelar invisibles y
  esquiva-teleport (antes fallaban en silencio por falta de handler).
- **DOT/HOT**: daño/cura por turno como buffs con `Tick()` al inicio del turno;
  `SacrificeDamage` (Effect_109) y fallback de muerte directa.
- **Invocaciones**: si la celda objetivo está ocupada, se reubica a la celda
  libre más cercana; soporte de `Effect_185`.
- **Dopeuls**: NPC (ReplyHandler 11) lanza combate PvM contra el Dopeul, con
  cooldown de 3h por personaje+monstruo y entrega de Doplones según el nivel.
- **i18n**: mensajes y descripciones de comandos traducidos al español
  (paneles xp/tp/dj, restat, parchotage, debugmap, stop, reload, mount equip,
  item, hp, god, bank).

## Pendientes heredados (documentados, fuera de v1)

- Barra de turnos con muerte de entidad (bug visual).
- Invocaciones: ajustes adicionales tras el fix parcial.
- Efectos Replay restantes.
- Sram: daño de algunos venenos (solo aplican los estados).
