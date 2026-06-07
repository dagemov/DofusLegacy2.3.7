# Agent Handoff — Phase 3 QA VPS Deployed (smoke pendiente)

Generated: `2026-06-07`  
Rama: **`feature/combat-readychecker-phase3`**

## Estado VPS

```txt
sunshine-server UP — ReadyChecker build desplegado
puertos 2450/5557 OK (corregidos post-rebuild)
telemetría ON (QA en curso)
smoke 5 combates: PENDIENTE OPERADOR
```

| Campo | Valor |
| --- | --- |
| VPS | `174.138.35.107` |
| Backup DB | `/root/backups/sunshine/sunshine-pre-restart-20260607T015107Z.sql` |
| Backup inventario | `backups/vps/20260606-215057/` |
| `.env` backup | `/opt/dofus-2.0.0/.env.bak-phase3qa-20260607` |
| Telemetría | **ON** |
| ReadyChecker | **ON** |

## Acción inmediata — operador

1. Conectar cliente (`174.138.35.107:2450`).
2. Realizar **5 combates smoke** — [combat-readychecker-phase3-vps-qa.md](../combat-sanitization/combat-readychecker-phase3-vps-qa.md).
3. Avisar → `collect-vps-combat-logs.ps1 -RunAnalyzer`.
4. Tras análisis → `disable-vps-combat-telemetry.ps1` (`CONFIRM_RESTART=1`).

## Collect

```powershell
.\infrastructure\artifacts\combat-health\collect-vps-combat-logs.ps1 -SshKey "SSH\private_key_sebas.pem" -RunAnalyzer
```

## Rollback rápido

```txt
CombatReadyCheckerEnabled=false + restart sunshine
```

## Cierre Phase 3 QA

- [x] Deploy sunshine OK
- [x] TCP 2450/5557 OK
- [ ] 5 combates smoke
- [ ] Collect + métricas
- [ ] Telemetría OFF post-QA
