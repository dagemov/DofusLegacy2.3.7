# Diagnóstico — Cliente 2450 vs VPS 446/3467

**Fecha:** 2026-06-06  
**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**VPS:** `174.138.35.107`  
**Estado:** **RESUELTO** — VPS alineado a `2450` / `5557` / `174.138.35.107`

## Síntoma

Cliente con `config.xml`:

```xml
<entry key="connection.host">174.138.35.107</entry>
<entry key="connection.port" type="int">2450</entry>
```

Mensaje: *Conexión al servidor fracasó* (con `sunshine-server` ya en READY tras fix Items).

## Caso 1 — Puertos (confirmado)

| Puerto | Windows `Test-NetConnection` (antes) | VPS `ss` (antes) |
| --- | --- | --- |
| **2450** (cliente auth) | **False** | no escuchaba |
| **446** (VPS auth legacy) | True | `0.0.0.0:446` |
| **3467** (VPS world legacy) | True | `0.0.0.0:3467` |
| **5557** (cliente world vía DB) | **False** | no escuchaba |

**Causa probable encontrada:** el cliente apunta al puerto canónico **2450**, pero el VPS publicaba auth en **446** (puertos de rollback legacy).

## Caso 2 — Puerto auth canónico (repo)

| Fuente | Auth | World |
| --- | --- | --- |
| `docs/vps-deploy.md` | **2450** | **5557** |
| `Client2.3.7/config.xml` | **2450** | (vía auth → DB) |
| `.env.example` | **2450** | **5557** |
| `docker/entrypoint.sh` defaults | 2450 | 5557 |
| `AuthServer.cs` code defaults | 446 | 3467 |

**Conclusión:** el cliente **no** debe cambiar a 446. Los defaults de código (`446`/`3467`) son legacy; el contrato operativo del proyecto es **2450**/**5557**.

## Caso 3 — Host anunciado (confirmado)

`.env` VPS **antes** del fix:

```env
WORLD_PUBLIC_HOST=127.0.0.1
AUTH_PORT=446
WORLD_PORT=3467
```

`Config.xml` generado:

```txt
AuthIp=127.0.0.1  AuthPort=446
WorldIp=127.0.0.1 WorldPort=3467
```

Logs IPC (antes): `announced as 127.0.0.1:446`.

`entrypoint.sh` escribe `AuthIp`/`WorldIp` desde `WORLD_PUBLIC_HOST` y sincroniza `worlds.Id=18`.

## Caso 4 — Tabla `worlds` (DB)

```sql
SELECT Id, Name, Address, Port FROM worlds WHERE Id=18;
```

| Id | Name | Address | Port |
| --- | --- | --- | --- |
| 18 | Helsephine | **174.138.35.107** | **5557** |

La DB ya tenía IP/puerto world correctos para el cliente. El auth entrega esta fila tras login. El bloqueo era **solo** el puerto auth inicial (2450 cerrado).

`WorldServerManager` / `ConnectionHandler` leen `worlds` al listar servidores.

## Fix aplicado (2026-06-06)

```bash
# /opt/dofus-2.0.0/.env
WORLD_PUBLIC_HOST=174.138.35.107
AUTH_PORT=2450
WORLD_PORT=5557

docker compose --env-file ../.env -f docker-compose.yml -f docker-compose.vps.yml up -d sunshine
```

Backup: `/opt/dofus-2.0.0/.env.bak-port-fix-20260606`

Script repo:

```powershell
.\infrastructure\artifacts\combat-health\fix-vps-client-ports.ps1 -SshKey "SSH\private_key_sebas.pem"
```

## Validación post-fix

| Check | Resultado |
| --- | --- |
| `docker ps` puertos | `0.0.0.0:2450->2450`, `0.0.0.0:5557->5557` |
| `Config.xml` | `AuthIp/WorldIp=174.138.35.107`, puertos 2450/5557 |
| Logs READY | `announced as 174.138.35.107:2450` / `:5557` |
| `Test-NetConnection` 2450 | **True** |
| `Test-NetConnection` 5557 | **True** |
| `worlds.Id=18` | `174.138.35.107:5557` |
| Telemetría | **ON** (`FIGHT_TELEMETRY_ENABLED=true`) |
| Login cliente | **OK** (confirmado operador 2026-06-06) |
| Combates + telemetría | **EN CURSO** — collect pendiente post-sesión |

## Prevención

Tras deploy `-SunshineOnly`, verificar que `.env` del VPS no conserve puertos legacy `446`/`3467` de migraciones anteriores (`docs/migration/rollback-to-sunshine-module-plan.md`). Usar siempre valores de `docs/vps-deploy.md`.
