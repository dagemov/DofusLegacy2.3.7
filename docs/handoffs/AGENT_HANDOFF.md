# Agent Handoff

Generated: `2026-06-06`

## Estado VPS — login desbloqueado (pendiente confirmación operador)

| Campo | Valor |
| --- | --- |
| Rama | **`feature/items-sets-visibility-and-vps-combat-telemetry`** |
| Incidente 1 | Crash Items ObjectEffect → `Effects='0000'` items 12618–12622 |
| Incidente 2 | Cliente `2450` vs VPS `446/3467` + `WORLD_PUBLIC_HOST=127.0.0.1` |
| Fix puertos | `.env`: `2450`/`5557`/`174.138.35.107` + `compose up -d sunshine` |
| Telemetría | **ON** |
| DB | Intacta (`worlds.Id=18` = `174.138.35.107:5557`) |

### Validación automática (2026-06-06, post-fix puertos)

```txt
sunshine-server Up — READY 100%
Puertos: 0.0.0.0:2450, 0.0.0.0:5557
Config.xml: AuthIp/WorldIp=174.138.35.107
Logs: announced as 174.138.35.107:2450 / :5557
Test-NetConnection 2450, 5557 → True (Windows)
FIGHT_TELEMETRY_ENABLED=true
```

### Operador — siguiente (orden estricto)

1. **Login cliente** con `config.xml` existente (`174.138.35.107:2450`) — no cambiar puerto.
2. Si OK → 1 combate smoke → `collect-vps-combat-logs.ps1 -RunAnalyzer`.
3. Re-aplicar effects Jalato cuando exista fix codec Admin↔runtime.
4. Phase 3 ReadyChecker **BLOQUEADA** hasta JSONL real.

### Scripts

```powershell
# Re-aplicar fix puertos si .env se corrompe de nuevo:
.\infrastructure\artifacts\combat-health\fix-vps-client-ports.ps1 -SshKey "SSH\private_key_sebas.pem"
```

### Docs

- [vps-client-port-host-diagnostic.md](../combat-sanitization/vps-client-port-host-diagnostic.md)
- [vps-client-connection-after-ready-incident.md](../combat-sanitization/vps-client-connection-after-ready-incident.md)
- [vps-telemetry-deploy-connection-incident.md](../combat-sanitization/vps-telemetry-deploy-connection-incident.md)

### Deuda técnica

- Codec `items.Effects` Admin vs `ItemsLoader` legacy.
- Evitar puertos legacy `446`/`3467` en `.env` VPS tras deploys parciales.
