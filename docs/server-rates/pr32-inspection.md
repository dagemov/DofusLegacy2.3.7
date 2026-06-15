# PR #32 — Inspección de rates y límites hardcodeados

Referencia: PR **#32** (`devp-patch-integration`, mergeado 2026-06-07).

## Archivos tocados relevantes

| Archivo | Commit PR #32 | Qué cambió |
|---------|---------------|------------|
| `Sunshine.BaseServer/Configuration/GameRates.cs` | `c4664ba` | `DropQuantityMultiplier` leído de `Config.xml` |
| `Sunshine.WorldServer/Game/Fights/Results/FightFormulas.cs` | `c4664ba` | Fórmula XP de grupo, drops por prospección, VIP x2 drop |
| `Sunshine.WorldServer/Game/Fights/Results/FightResults.cs` | (VIP en commits posteriores) | `GameRates.ApplyXp`, kamas de combate, VIP x2 |
| `Sunshine.WorldServer/Game/Actors/Fighters/CharacterFighter.cs` | `1864886` | Límite de usos de arma por turno |

## Valores hardcodeados identificados

### XP (`GameRates.Xp`)

- **Origen actual:** `Config.xml` → clave `RateXp` (default `5` en código, `3` en Docker entrypoint).
- **Aplicación:** `FightResults.AddEarnedExperience` → `GameRates.ApplyXp(expAdded)`.
- **También:** quests (`QuestsCollection`), oficios (`JobsCollection` usa `RateJobXp`), monturas (`RateMountXp`).

### Drop / PP (`GameRates.Drop`, prospección)

- **Origen actual:** `Config.xml` → `RateDrop`, `DropQuantityMultiplier`.
- **Aplicación:** `FightFormulas.CalculateWinItems`:
  - `basePercent = GameRates.ApplyDrop(...)` (multiplica tasa base del monstruo).
  - `prospectionMultiplier = prospection / 100.0` (PP del personaje, no rate servidor).
  - Cantidad ganada escala con `GameRates.DropQuantityMultiplier`.

### Kamas (`GameRates.Kamas`)

- **Origen actual:** `Config.xml` → `RateKamas` + rangos `FightKamasLevel*`.
- **Aplicación:** `FightResults.AddEarnedKamas` → `GameRates.RollFightKamas`.

### Usos de arma por turno

- **Origen actual:** hardcode en `CharacterFighter.CanCastCloseCombat` (línea ~155):

```csharp
int maxWeaponUses = (weaponTemplate != null && (ItemTypeEnum)weaponTemplate.TypeId == ItemTypeEnum.DAGUE) ? 2 : 1;
```

- **Comportamiento:** dagas permiten 2 ataques/turno; resto de armas 1. Sin límite por combate.
- **Reset:** `_weaponUses = 0` en `ResetUsedPoints()` (fin de turno).

### Usos de arma por combate

- **Estado:** no implementado en PR #32. Campo reservado en `config_rates_Server.txt`.

### Hechizos (`MaxCastPerTurn`)

- **Origen actual:** plantilla del hechizo en BD (`SpellTemplate.MaxCastPerTurn`).
- **Lógica:** `SpellHistory.CanCastSpell` — si `MaxCastPerTurn <= 0`, ilimitado por turno.
- **Campo reservado:** `SPELL_USES_DEFAULT` para override servidor cuando plantilla no define límite.

## Mapa config_rates_Server.txt → código

| Clave | Reemplaza / complementa |
|-------|------------------------|
| `XP_RATE` | `GameRates.Xp` (prioridad sobre `RateXp` en `Config.xml`) |
| `DROP_RATE` | `GameRates.Drop` |
| `KAMAS_RATE` | `GameRates.Kamas` |
| `PP_RATE` | Multiplicador global de prospección en `FightFormulas` |
| `WEAPON_USES_PER_TURN` | `CharacterFighter` límite por turno (`0` = ilimitado) |
| `WEAPON_USES_PER_FIGHT` | `CharacterFighter` límite por combate (`0` = ilimitado) |
| `SPELL_USES_DEFAULT` | `SpellHistory` cuando `MaxCastPerTurn == 0` (`0` = ilimitado) |

## Compatibilidad

- Si `config_rates_Server.txt` no existe al arranque, se crea en `Config/` sembrando rates desde `Config.xml` (si ya cargado) y defaults de combate alineados con PR #32.
- `Config.xml` sigue vigente para red, BD, telemetría y rates no migrados (JobXp, MountXp, FightKamas ranges).
