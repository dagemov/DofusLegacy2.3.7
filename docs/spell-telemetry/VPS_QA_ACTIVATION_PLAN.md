# Plan de activación controlada VPS — spell effect telemetry

**Estado:** Pendiente merge PR → deploy → ventana QA  
**Regla:** no activación global permanente. No fixes de hechizos hasta primer reporte real.

---

## Pre-requisitos

1. Merge PR `feat(telemetry): add spell/effect layered diagnostics lab` en `devp`
2. Backup de `docker-compose`, `.env`, `Config.xml` del contenedor sunshine
3. Deploy del contenedor con la imagen que incluye Phase 3 telemetry

---

## Activación temporal (ventana QA)

Variables recomendadas **solo durante pruebas**:

```bash
SPELL_EFFECT_TELEMETRY_ENABLED=true
COMBAT_HEALTH_LAB=1
FIGHT_TELEMETRY_LOG_DIRECTORY=/app/logs/combat
```

Opcional mirror humano (sin cambios en este PR):

```bash
FIGHT_COMBAT_LOG_ENABLED=true
FIGHT_COMBAT_LOG_DIR=/app/logs/fights
FIGHT_COMBAT_LOG_CONSOLE=true
```

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

1. Copiar JSONL generados
2. Ejecutar analyzer:

```powershell
dotnet run --project infrastructure/scripts/CombatTelemetryAnalyzer -- `
  --input "Infrastructure/temporal-artifacts/combat-telemetry/vps-run-YYYYMMDD" `
  --output "Infrastructure/temporal-artifacts/combat-telemetry/vps-report.md"
```

3. Redactar reporte: `docs/spell-telemetry/runs/YYYY-MM-DD-first-lab-run.md`
4. **Desactivar** env vars y reiniciar contenedor
5. Clasificar cada bug por capa (A/V/M/F/H/B/BD) — **no iniciar fixes** hasta revisión del reporte

---

## Plantilla reporte

Ver sección final de [PHASE_04_LAB_ANALYSIS_GUIDE.md](./PHASE_04_LAB_ANALYSIS_GUIDE.md).

Entregable: `docs/spell-telemetry/runs/YYYY-MM-DD-first-lab-run.md`
