# Phase 3 QA — ReadyChecker (VPS beta)

**No deploy automático.** Ejecutar solo cuando el operador apruebe.

## Pre-requisitos

- Rama `feature/combat-readychecker-phase3` mergeada o desplegada en VPS beta.
- Backup DB + inventario (scripts `backup-before-restart`, `backup-vps-state`).
- Cliente apuntando a `174.138.35.107:2450`.

## Paso 1 — Backup

```powershell
$env:CONFIRM_BACKUP = "1"
.\infrastructure\artifacts\combat-health\# o scripts/vps según runbook existente
```

Documentar ruta del backup en handoff.

## Paso 2 — Deploy sunshine-only

```powershell
.\infrastructure\scripts\deploy-vps.ps1 -SunshineOnly -SshKey "SSH\private_key_sebas.pem"
```

Verificar `.env` VPS:

```txt
AUTH_PORT=2450
WORLD_PORT=5557
WORLD_PUBLIC_HOST=174.138.35.107
CombatReadyCheckerEnabled=true
CombatReadyCheckerTimeoutMs=5000
```

## Paso 3 — Activar telemetría

```powershell
$env:CONFIRM_RESTART = "1"
.\infrastructure\artifacts\combat-health\enable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
```

## Paso 4 — Smoke (5 combates mínimo)

| # | Escenario | Objetivo |
| ---: | --- | --- |
| 1 | 1v1 monstruo simple | Sin timer 35 s |
| 2 | 1v1 con invocación | ReadyChecker tras cadena IA |
| 3 | 2+ monstruos | Cola turnos |
| 4 | Turno jugador manual (pasar turno) | `ReadyCheckerAck` |
| 5 | Combate con aliado (si hay) | Múltiples waiters |

Registrar hora inicio/fin.

## Paso 5 — Collect + analyzer

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
```

Si `scp` falla en Windows: `docker cp` remoto + `scp -i key host:/tmp/...` (ver handoff).

## Métricas de éxito (vs baseline 2026-06-06)

| Métrica | Baseline | Objetivo post-fix |
| --- | ---: | --- |
| `TimerElapsed` ~35 s en `CharacterFighter` | 6 | **0** (o ≤1 marginal) |
| `ReadyCheckerStarted` | 0 | **> 0** por combate |
| `ReadyCheckerAck` / `Timeout` | 0 | Presentes |
| `AiFinished` → turno jugador >30 s | 4 | **0** |
| Spell cast failed | 0 | Sin regresión |
| Subjetivo operador | stalls visibles | Combate más fluido |

## Paso 6 — Desactivar telemetría

```powershell
$env:CONFIRM_RESTART = "1"
.\infrastructure\artifacts\combat-health\disable-vps-combat-telemetry.ps1 -SshKey "SSH\private_key_sebas.pem"
```

## Rollback operativo (sin código)

```txt
CombatReadyCheckerEnabled=false
docker compose ... restart sunshine
```

Restaura avance inmediato de turno (comportamiento pre-Phase 3).

## Cierre QA

Phase 3 QA cerrada solo si:

1. Logs reales post-fix analizados.
2. `TimerElapsed` 35 s baja claramente.
3. Sin regresiones graves (crash, desconexión masiva).
4. Operador confirma fluidez mejorada.

Documentar en `combat-vps-telemetry-analysis-YYYYMMDD-post-phase3.md`.
