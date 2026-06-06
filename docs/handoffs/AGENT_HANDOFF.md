# Agent Handoff - Admin Tools Migration

Generated: `2026-06-02`

## Macro Combat Sanitization — Gate Phase 3

| Campo | Valor |
| --- | --- |
| Rama | **`feature/combat-telemetry-phase2`** |
| Phase 2 | **DONE** (`b59a97c`, `232447b`, `09196c3`) |
| Gate telemetría real | **ABIERTO** — sin logs PvM en `Infrastructure/logs/combat/` |
| Phase 3 ReadyChecker | **BLOQUEADA** — hipótesis sin confirmar en combate real |
| Referencia | `C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback` |

### Entregables Phase 2

- `CombatTelemetry` — `Sunshine.WorldServer/Game/Fights/Telemetry/CombatTelemetry.cs`
- Logs JSONL: `combat-turn-flow-*.jsonl`, `spell-casts/*.jsonl`
- `GameFightTurnReadyMessageReceived` registrado; handler **sin lógica funcional**
- Analyzer: `Infrastructure/scripts/CombatTelemetryAnalyzer/` (JSONL + legacy `.log`)
- Lab scripts actualizados → salida `Infrastructure/temporal-artifacts/combat-telemetry/report.md`

### Validación gate (esta sesión)

| Check | Resultado |
| --- | --- |
| `dotnet build Sunshine.csproj` | OK |
| `dotnet build Sunshine.sln` | Bloqueado por lock Admin.Api (VS) — no bloquea lab combate |
| Logs reales `Infrastructure/logs/combat/` | **Vacío** |
| Analyzer | OK solo sobre sample sintético |

### Documentación gate

- [combat-real-telemetry-gate.md](../combat-sanitization/combat-real-telemetry-gate.md) ← **estado y decisión**
- [combat-telemetry-phase2.md](../combat-sanitization/combat-telemetry-phase2.md)
- [combat-log-schema.md](../combat-sanitization/combat-log-schema.md)

### Siguiente acción exacta (operador)

1. `sync-vps-db-snapshot.ps1` + `appsettings.Development.local.json` → `sunshine_lab`
2. `run-local-combat-lab.ps1` con vars del gate doc → **3 escenarios PvM**
3. `collect-combat-logs.ps1` → `analyze-combat-telemetry.ps1`
4. Actualizar `combat-real-telemetry-gate.md` con latencias reales
5. **Solo si report confirma hand-off roto:** abrir Phase 3 ReadyChecker

### Prohibiciones activas

```txt
no fix ReadyChecker sin logs reales
no VPS/deploy en esta fase
no mezclar Admin items/spells
```

---

## Gate final — Items Builder

| Campo | Valor |
| --- | --- |
| Rama | `feature/items-preview-sets-polish-final` |
| PR target | `devp` |
| **Items Builder** | **`COMPLETE`** |
| **Spell Builder** | **`NEXT, not started`** |

### Validación gate (2026-06-05)

| Check | Resultado |
| --- | --- |
| Lock API limpiado | OK (`Stop-Process RollblackLegacy.Admin.Api`, `dotnet build-server shutdown`) |
| `dotnet build` Admin.Api | OK |
| `npm run build` | OK (warning budget +1.13 kB) |
| `dotnet build` Sunshine.sln | OK (5 warnings, 0 errors) |
| Git hygiene | OK — sin commit de `Client2.3.7/`, `OneLauncher/`, `config/`, `temporal-artifacts/` |
| Spell Builder en rama PR | **Revertido** — commits `e5f0964` y `9031339` excluidos del scope Items |

### Browser QA

| Estado | Notas |
| --- | --- |
| `PENDING_OPERATOR` | Rutas mínimas documentadas; builds OK como precondición |

Rutas:

```txt
/admin/items/new
/admin/items/12616/edit
/admin/items/icon-selector
/admin/item-sets
/admin/item-sets/:setId
/admin/publication
```

Confirmar: stats icons, preview BY_CATEGORY, sets con preview, bonos por piezas, sin errores consola críticos.

### Entregables Items (rama)

- Preview reconciliation (`BY_CATEGORY`)
- Sets read UI + bonos por piezas
- Stat icons fix (`src/assets` en `angular.json`)
- Docs: preview reconciliation, stat icons, sets builder

### Merge flow

1. PR `feature/items-preview-sets-polish-final` → `devp` (creado en gate)
2. Tras aprobación: merge a `devp`
3. Luego `devp` → `main` (no borrar ramas hasta main estable)

### Siguiente

- Abrir **Spell Builder** en rama dedicada **después** de merge Items a `devp`/`main`
- Cherry-pick o re-aplicar trabajo Spell (`e5f0964`, `9031339`) en rama `feature/spell-builder-*` separada

### Prohibiciones

- No publicar cliente real, no VPS, no temporal-artifacts en git

## Repo

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/items-preview-sets-polish-final
```
