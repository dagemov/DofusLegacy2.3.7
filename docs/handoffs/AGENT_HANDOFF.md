# Agent Handoff

Generated: `2026-06-02`

## Sprint — Items/Sets visibility + VPS Combat Telemetry

| Campo | Valor |
| --- | --- |
| Rama | **`feature/items-sets-visibility-and-vps-combat-telemetry`** |
| Sets CRUD/bonuses | **DONE** (cherry-pick `dd21dff`…`deffa0d`) |
| Item create + effects | **DONE** (`05f40c8`) |
| ItemSets.d2o package | **DONE** (`ce71d56`) |
| Jalato visibility | **OPERATOR_QUERY** — [jalato-infernal-visibility-diagnostic.md](../admin-tools/items-builder/items-final/jalato-infernal-visibility-diagnostic.md) |
| Publish Jalato | **READY_FOR_OPERATOR_PUBLISH** |
| VPS telemetry scripts | **DONE** — enable/disable/collect + `report.html` |
| Combat Phase 3 | **BLOQUEADA** |

### Builds (sesión)

| Check | Resultado |
| --- | --- |
| `dotnet build Sunshine.csproj` | OK |
| `dotnet build Admin.Api` | OK |
| `npm run build` | OK |
| VPS scripts `-DryRun` | OK |

### Operador — Items Jalato

```powershell
# 1. Obtener ItemIds en Admin search "Jalato"
# 2. Completar tabla en jalato-infernal-visibility-diagnostic.md
.\infrastructure\artifacts\items-publication\stage-item-package.ps1 -ItemIds <ids> -TemplateItemId 7754
# 3. Publish controlado solo con CONFIRM_BACKUP=1 + CONFIRM_PUBLISH=1
```

### Operador — VPS combates

```powershell
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem" -DryRun
$env:CONFIRM_RESTART='1'
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
# 30-50 combates — ver combat-vps-test-matrix.md
.\infrastructure\artifacts\combat-health\disable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
start Infrastructure\temporal-artifacts\combat-telemetry\report.html
```

### Docs sprint

- [vps-combat-telemetry-operations.md](../combat-sanitization/vps-combat-telemetry-operations.md)
- [sets-builder-final-acceptance.md](../admin-tools/sets-builder/sets-builder-final-acceptance.md)

### Prohibiciones

```txt
no ReadyChecker
no publish/restart VPS sin CONFIRM_*
no commitear logs/secrets
```
