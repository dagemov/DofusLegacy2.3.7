# Plan de activación controlada VPS — spell effect telemetry

**Estado:** PR #49 mergeado → `devp` (`b6cc7f4`) → `main` (`4394b10` vía PR #50). **Deploy + ventana QA en curso.**  
**Regla:** no activación global permanente. No fixes de hechizos hasta primer reporte real.

**Commit deploy estable:** `4394b10` (`main`)  
**Reporte:** [runs/2026-06-16-first-lab-run.md](./runs/2026-06-16-first-lab-run.md)

---

## Pre-requisitos

1. ~~Merge PR spell telemetry en `devp`~~ **OK** (#49)
2. ~~Merge `devp` → `main`~~ **OK** (#50)
3. Backup de `docker-compose`, `.env`, `Config.xml` del contenedor sunshine
4. Deploy sunshine desde `main` (`4394b10`):

```powershell
.\scripts\deploy-vps.ps1 -SunshineOnly
echo 4394b10 > /opt/dofus-2.0.0/.deploy-commit   # en VPS, post-deploy
```

5. Montar logs combat en host (incluido en `docker-compose.vps.yml`):

```yaml
- /opt/dofus-2.0.0/logs/combat:/app/logs/combat
```

---

## Activación temporal (ventana QA)

Script automatizado:

```powershell
.\scripts\vps\spell-telemetry-qa.ps1 -Action backup
.\scripts\vps\spell-telemetry-qa.ps1 -Action activate
.\scripts\vps\spell-telemetry-qa.ps1 -Action verify
```

Variables aplicadas:

```bash
SPELL_EFFECT_TELEMETRY_ENABLED=true
COMBAT_HEALTH_LAB=1
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
FIGHT_TELEMETRY_ENABLED=false
FIGHT_COMBAT_LOG_ENABLED=false   # mirror OFF — JSONL es fuente principal
```

**Estado VPS previo a QA (2026-06-16):** `FIGHT_COMBAT_LOG_ENABLED=true` — se desactiva en ventana QA.

**Default producción sin env vars:** `SpellEffectTelemetryEnabled=false` → cero eventos layer.

---

## Ruta de logs

Dentro del contenedor:

```text
/app/logs/combat/spell-casts/spell-casts-YYYYMMDD-HHMMSS.jsonl
```

Recolección local (post-ventana):

```powershell
# Ajustar host/ruta según infra existente
scp -r user@vps:/opt/dofus-2.0.0/logs/combat/spell-casts/ `
  Infrastructure/temporal-artifacts/combat-telemetry/vps-run-YYYYMMDD/
```

---

## Casos mínimos (5 reproducciones)

| # | Caso | Spell ID | Capas esperadas |
| --- | --- | ---: | --- |
| 1 | Látigo Osamodas | 30 | V, M |
| 2 | Sacrificada Sadida | 233 | M, H, F |
| 3 | Sacrificio Muñequero | 198 | M, H |
| 4 | Desinvocación Anutrof | 46 | M, H |
| 5 | Veneno Sram/Sadida | varios | B, H, F |

Guía detallada: [PHASE_03_MANUAL_TEST_MATRIX.md](./PHASE_03_MANUAL_TEST_MATRIX.md)

---

## Post-ventana

1. Copiar JSONL:

```powershell
.\scripts\vps\spell-telemetry-qa.ps1 -Action collect -RunDate 20260616
```

2. Ejecutar analyzer:

```powershell
dotnet run --project infrastructure/scripts/CombatTelemetryAnalyzer -- `
  --input "Infrastructure/temporal-artifacts/combat-telemetry/vps-run-20260616" `
  --output "docs/spell-telemetry/runs/2026-06-16-layer-report.md"
```

3. Completar reporte: [runs/2026-06-16-first-lab-run.md](./runs/2026-06-16-first-lab-run.md)
4. **Desactivar:**

```powershell
.\scripts\vps\spell-telemetry-qa.ps1 -Action deactivate
```

5. Clasificar cada bug por capa (A/V/M/F/H/B/BD) — **no iniciar fixes** hasta revisión del reporte

---

## Plantilla reporte

Ver sección final de [PHASE_04_LAB_ANALYSIS_GUIDE.md](./PHASE_04_LAB_ANALYSIS_GUIDE.md).

Entregable: `docs/spell-telemetry/runs/YYYY-MM-DD-first-lab-run.md`
