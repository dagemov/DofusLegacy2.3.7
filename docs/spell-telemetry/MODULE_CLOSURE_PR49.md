# Cierre módulo — spell/effect telemetry Phase 1

**Fecha cierre código:** 2026-06-16  
**PR:** [#49](https://github.com/dagemov/DofusLegacy2.3.7/pull/49) → `devp` → [#50](https://github.com/dagemov/DofusLegacy2.3.7/pull/50) → `main`

---

## Commits de referencia

| Rama | Commit | Descripción |
| --- | --- | --- |
| `devp` | `b6cc7f4` | Merge PR #49 (telemetría spell/effect) |
| `main` | `4394b10` | Merge PR #50 (`devp` estable → producción) |
| Telemetría base | `702da74`…`285a379` | 6 commits Phase 1–3 + docs |

**Deploy VPS objetivo:** `4394b10` (`main`) — incluye telemetría + resto de `devp` al cierre de #50.

---

## Flujo acordado (dos operadores)

1. Feature branch limpia desde `devp`
2. PR → `devp`
3. Merge `devp`
4. Validación / build
5. Merge `devp` → `main` cuando el módulo esté estable
6. Deploy VPS solo desde rama estable (`main`)
7. Documentar cierre en `docs/`

---

## Estado post-merge

| Item | Estado |
| --- | --- |
| Código telemetría en `main` | OK |
| Build local | OK (`dotnet build Sunshine.csproj`) |
| Default prod `SpellEffectTelemetryEnabled` | `false` |
| QA ventana VPS | Ver [VPS_QA_ACTIVATION_PLAN.md](./VPS_QA_ACTIVATION_PLAN.md) |
| Primer reporte real | Pendiente — [runs/2026-06-16-first-lab-run.md](./runs/2026-06-16-first-lab-run.md) |

---

## Ramas de fix futuras (no abrir hasta reporte)

- `fix/spell-target-mask-summon-only`
- `fix/spell-dot-lifecycle`
- `fix/spell-critical-formula-path`
- `fix/monster-ai-summon-validation`

---

## Scripts operador

```powershell
# Estado / backup / activar / desactivar / recolectar logs
.\scripts\vps\spell-telemetry-qa.ps1 -Action status
.\scripts\vps\spell-telemetry-qa.ps1 -Action backup
.\scripts\vps\spell-telemetry-qa.ps1 -Action activate
.\scripts\vps\spell-telemetry-qa.ps1 -Action collect -RunDate 20260616
.\scripts\vps\spell-telemetry-qa.ps1 -Action deactivate
```

Deploy sunshine desde repo local:

```powershell
.\scripts\deploy-vps.ps1 -SunshineOnly
```
