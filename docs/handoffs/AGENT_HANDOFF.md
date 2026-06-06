# Agent Handoff - Admin Tools Migration

Generated: `2026-06-06`

## Macro Combat Sanitization — Phase 1 + Lab

| Campo | Valor |
| --- | --- |
| Rama | **`feature/combat-sanitization-phase1-audit`** (base: `devp`) |
| Estado | **Phase 1 DONE** — auditoría docs; Phase 2 scaffolding scripts |
| Fixes código | **NINGUNO** — sin evidencia de logs aún |
| Referencia | `C:\Users\Hombr\source\repos\RollBlackServer\2.0.0\Rollback` |

### Hallazgo principal (auditoría)

Sunshine **no tiene `ReadyChecker`**. `HandleGameFightTurnReadyMessage` está vacío y `EndTurn` avanza turno **inmediatamente**. Rollback ya corrigió el bug ~35s (hand-off, no IA) en `feature/combat-readychecker-turn-advance-fix`.

### Documentación

- [combat-system-audit.md](../combat-sanitization/combat-system-audit.md)
- [combat-turn-flow-comparison.md](../combat-sanitization/combat-turn-flow-comparison.md)
- [combat-telemetry-plan.md](../combat-sanitization/combat-telemetry-plan.md)
- [combat-health-lab-plan.md](../combat-sanitization/combat-health-lab-plan.md)

### Lab

```txt
Infrastructure/artifacts/combat-health/
```

Scripts: `run-local-combat-lab.ps1`, `sync-vps-db-snapshot.ps1`, `collect-combat-logs.ps1`, `collect-vps-combat-logs.ps1`, `analyze-combat-telemetry.ps1`

### Siguiente acción exacta

1. Implementar `FightTelemetry` en Sunshine (Phase 2 código)
2. Migrar `CombatTelemetryAnalyzer` a `infrastructure/scripts/`
3. Reproducir síntoma en lab con DB snapshot + generar logs
4. **Solo entonces** Phase 3 port de `ReadyChecker`/`TryAdvanceTurn`

### Prohibiciones activas

```txt
no VPS primero
no fixes sin logs
no mezclar Admin items/spells
no dejar artifact como fuente final
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
