# Agent Handoff - Admin Tools Migration

Generated: `2026-06-02`

## Sprint puente — Items/Sets visibility + VPS Combat Telemetry

| Campo | Valor |
| --- | --- |
| Rama | **`feature/items-sets-visibility-and-vps-combat-telemetry`** (base: `devp`) |
| Bloque A — Items/Sets | Sets CRUD cherry-picked; Jalato diagnóstico **OPERATOR_QUERY** |
| Bloque B — VPS telemetry | Scripts enable/disable/collect + `CombatTelemetry` cherry-picked |
| Publish real Jalato | **`READY_FOR_OPERATOR_PUBLISH`** |
| Combat Phase 3 | **BLOQUEADA** — gate logs reales abierto |

### Commits cherry-picked (sets)

- `dd21dff` — paginación sets
- `b9c1cce` — CRUD API sets
- `da758a3` — editor UI sets
- `89a7a76` — create item con effects
- `ce71d56` — validación ItemSets.d2o en package

### Siguiente acción operador

**Items Jalato:**

1. Completar [jalato-infernal-visibility-diagnostic.md](../admin-tools/items-builder/items-final/jalato-infernal-visibility-diagnostic.md) con ItemIds reales (Admin search o SQL).
2. `stage-item-publication` + `validate-publication-package` por item.
3. `CONFIRM_BACKUP=1` + `CONFIRM_PUBLISH=1` + `validate-real-client` si aprueba publish.

**VPS telemetry:**

```powershell
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem" -DryRun
$env:CONFIRM_RESTART='1'
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
# 30-50 combates
.\infrastructure\artifacts\combat-health\disable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem"
.\infrastructure\artifacts\combat-health\analyze-combat-telemetry.ps1
```

### Prohibiciones activas

```txt
no ReadyChecker / fixes combate
no publish/restart VPS sin CONFIRM_*
no commitear logs/backups/secrets
```

---

## Gate Items Builder (histórico)

| Campo | Valor |
| --- | --- |
| Items Builder | **COMPLETE** (browser QA parcial) |
| Spell Builder | **NEXT** — rama separada |

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/items-sets-visibility-and-vps-combat-telemetry
```
