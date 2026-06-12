# Fix: curas aliadas y explosión Sacrificada (spell 233)

Documentación del problema detectado en el informe de combate Osa vs Sadida (`docs/informe-combate-osa-sadida.html`) y de la corrección aplicada en el emulador Sunshine.

**Rama prevista:** `feature/spells-invos-venenos-fix`  
**Commit:** aislado (pendiente de commit por el maintainer)

---

## 1. Curas que afectan a enemigos (La Gonflable / Néctar)

### Síntoma

La muñeca **La Gonflable** (monstruo 117, caster `-1827`) lanza **Néctar** (spell **2251**) y cura a **todos** los actores en la zona, incluido el enemigo Osamodas (`379`) y rivales en la misma celda.

Evidencia en logs:

```
HEAL src=-1827 tgt=379 amount=34
HEAL src=-1827 tgt=-1827 amount=28
HEAL src=-1827 tgt=378 amount=34
```

El informe contabilizó **17 curas a enemigos** frente a 25 a aliados en la sesión analizada.

### Causa raíz

1. En BD, el efecto de cura de Néctar (`spells_levels` id **1566**) tiene `target = 0x7FFF` (`SpellTargetType.ALL`), es decir, cualquier luchador en la zona.
2. `EffectManager.GetAffectedActors` solo filtra por equipo en `ALLY_ALL` / `ENEMY_ALL`. Con `ALL` u otros valores cae en el `default`, que añade **cualquier** `FightActor` en la celda.
3. El handler `Heal.cs` aplicaba la cura a todos los actores devueltos sin comprobar alianza.

### Comportamiento esperado (Dofus 2)

- **Néctar / Gonflable / Soin Poupesque:** curan al invocador y aliados en zona, **nunca** al rival.
- **Ronce Apaisante (192):** excepción — puede curar enemigos ralentizados por diseño del hechizo.

### Solución implementada

**Archivo:** `Sunshine.WorldServer/Game/Effects/Spells/Heals/Heal.cs`

- Nuevo filtro `GetHealTargets()`: si el hechizo **no** está en la lista blanca de curas a enemigos, se omiten actores con los que el caster no es aliado (`Caster.IsFriendlyWith(actor)`).
- Lista blanca actual: spell **192** (Ronce Apaisante).

```csharp
private static bool AllowsEnemyHealing(Spell spell) => spell?.Id == 192;

private IEnumerable<FightActor> GetHealTargets()
{
    var actors = GetAffectedActors();
    if (AllowsEnemyHealing(Spell) || Caster == null)
        return actors;
    return actors.Where(actor => Caster.IsFriendlyWith(actor));
}
```

### Validación esperada

Tras el fix, un cast de Néctar (`2251`) desde Gonflable debe producir solo líneas `HEAL` con `tgt` aliado (Sadida, muñecas del mismo equipo, invocaciones propias). **No** debe aparecer `HEAL src=-1827 tgt=379`.

---

## 2. Sacrificada: explosión mata al enemigo con vida completa (spell 233)

### Síntoma

La muñeca **La Sacrifiée** (monstruo 116, invocada con spell **189**) se desplaza hacia el enemigo, lanza **Sacrifice** (spell **233**) y el rival muere al instante aunque el daño numérico del hechizo sea bajo (p. ej. ~48 PV).

Secuencia observada en logs:

```
CAST caster=-1825 spell=233 level=6 cell=344
DISPATCH effect=Effect_DamageAir dice=60-0
DAMAGE src=-1825 tgt=379 amount=48
DISPATCH effect=Effect_Kill dice=1-0
```

El jugador percibe que “la fórmula está corrupta” porque el enemigo pierde **toda** la vida; en realidad el `Effect_Kill` del mismo hechizo ejecuta muerte instantánea sobre los objetivos en zona.

En sesiones anteriores también se vio autodaño al Sadida en el cast de invocación (189); eso ya se corrigió eliminando el handler custom (`SacrifierHandler` en spell 189).

### Causa raíz

En BD (`spells_levels` id **1165**, nivel 6 de spell 233), el hechizo tiene **dos** efectos:

| Orden | Efecto | Rol oficial |
|-------|--------|-------------|
| 1 | `Effect_DamageAir` (dados ~31–50) | Daño en área al enemigo |
| 2 | `Effect_Kill` | Eliminar **solo la muñeca** |

El handler genérico `Kill.cs` (registrado en fase 3 del motor de efectos) aplicaba `Effect_Kill` a **todos** los actores devueltos por `GetAffectedActors()`, incluido el enemigo en la celda objetivo.

### Comportamiento esperado (Dofus 2)

- La explosión inflige daño en zona según los dados del efecto de daño.
- `Effect_Kill` del mismo cast **solo** destruye la muñeca invocadora, no one-shot al rival.

### Solución implementada

**Archivo:** `Sunshine.WorldServer/Game/Effects/Spells/Others/Kill.cs`

- Para spell **233**, `Effect_Kill` se limita al caster (la muñeca): `Caster.Kill(Caster)` y return.
- El resto de usos de `Effect_Kill` (glifos, trampas, etc.) mantienen el comportamiento anterior.

```csharp
if (Spell?.Id == 233)
{
    if (Caster != null && Caster.IsAlive && !Caster.DeathHandled)
        Caster.Kill(Caster);
    return;
}
```

**Nota:** `SacrificeDamage.cs` (`Effect_109`) sigue disponible para niveles bajos del hechizo que usan ese efecto en BD; el nivel 6 usa `DamageAir` + `Kill`, corregido en `Kill.cs`.

### Validación esperada

```
CAST spell=233
DISPATCH Effect_DamageAir → DAMAGE tgt=enemigo (cantidad acorde a dados)
DISPATCH Effect_Kill → SUMMON_DIE / muerte solo de la muñeca (caster)
```

El Sadida (`378`) y el enemigo deben **sobrevivir** salvo que el daño de `Effect_DamageAir` los deje en 0 PV.

---

## 3. Archivos tocados

| Archivo | Cambio |
|---------|--------|
| `.../Effects/Spells/Heals/Heal.cs` | Filtro aliados en curas |
| `.../Effects/Spells/Others/Kill.cs` | `Effect_Kill` de spell 233 solo mata al caster |
| `docs/fix-cura-sacrificada-combate.md` | Este documento |

## 4. Plan de prueba manual (tester)

1. **Gonflable:** invocar spell 190, dejar que lance Néctar con enemigo adyacente → enemigo **no** gana PV; Sadida/muñecas sí.
2. **Ronce Apaisante:** spell 192 sobre enemigo ralentizado → cura al enemigo sigue siendo posible (regresión).
3. **Sacrificada:** invocar 189, muñeca camina y explota con 233 cerca del rival → daño parcial al enemigo, muñeca muere, Sadida vivo.
4. Revisar logs: ausencia de `HEAL` enemigo desde Gonflable; ausencia de `KILL`/muerte instantánea del enemigo tras `Effect_Kill` en spell 233.

## 6. Fix contador invocaciones (stats.summoner) — jun 2026

### Síntoma
Tras invocar el máximo de muñecas (p. ej. 3/3 Sadida), al morir una (Sacrificada al explotar) el hechizo de invocación queda **no seleccionable** en cliente aunque haya hueco.

### Causa
`GameFightMinimalStats.summoner` se rellenaba con `SummonLimit` (3) en lugar del id del invocador (378). El cliente solo hace `removeSummonedCreature()` si `stats.summoner == player.id`.

### Fix
- `FightActor`: campo `summoner` en stats = `0` para no-invocaciones.
- `SummonedMonster`: override con `stats.summoner = Summoner.Id`.
- `Kill.cs` spell 233: `summoned.Die()` para `SUMMON_DIE` + liberar slot en cliente.

### Validación
1. Invocar 3 muñecas, matar una (Sacrificada 233).
2. Debe poderse volver a lanzar spell 189 u otra invocación.
3. Log: `SUMMON_DIE` al explotar; sin bloqueo de UI.


- Informe HTML: `docs/informe-combate-osa-sadida.html`
- Contexto fixes previos: `docs/informe-logs-combate-y-fix-hechizos.md` (§6.1, §6.3)
- Handler invocación 189: `Spells/Casts/Sadida/SacrifierHandler.cs` (solo comentario; flujo estándar)
