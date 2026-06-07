# Incidente — Cliente no conecta con servidor READY

**Fecha:** 2026-06-06  
**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**Relacionado:** [vps-telemetry-deploy-connection-incident.md](./vps-telemetry-deploy-connection-incident.md), [vps-client-port-host-diagnostic.md](./vps-client-port-host-diagnostic.md)

## Contexto

Tras resolver el crash loop de Items (`RESTORED_WITH_TELEMETRY_ON`), `sunshine-server` quedó **READY 100%** pero el cliente seguía mostrando *Conexión al servidor fracasó*.

## Causa

Doble desalineación en `/opt/dofus-2.0.0/.env`:

| Parámetro | Valor erróneo | Valor esperado (cliente + docs) |
| --- | --- | --- |
| `AUTH_PORT` | 446 | **2450** |
| `WORLD_PORT` | 3467 | **5557** |
| `WORLD_PUBLIC_HOST` | 127.0.0.1 | **174.138.35.107** |

El cliente nunca llegaba al auth porque **2450 no estaba publicado**. Aun con login hipotético en 446, el world anunciado por DB era **5557** (también cerrado mientras Docker publicaba 3467).

## Resolución

1. Corregir `.env` (backup `.env.bak-port-fix-20260606`).
2. `docker compose ... up -d sunshine` (sin `-v`, volúmenes intactos).
3. Verificar `Test-NetConnection` a 2450 y 5557.

**Clasificación:** `RESTORED_PORT_HOST_ALIGNMENT` — telemetría permanece **ON**.

## Validación operador (PASS completo)

```txt
[ ] Cliente conecta auth (2450)
[ ] Cliente entra world (5557 vía handshake)
[ ] Sin error en logs auth/world tras login
```

Automático ya OK: puertos, Config.xml, `worlds.Id=18`, READY logs.

## No aplicado

- Cambiar `connection.port` del cliente a 446 (incorrecto vs contrato repo).
- `docker compose down -v`.
- Restore DB.
