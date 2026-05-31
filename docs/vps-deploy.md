# Despliegue VPS (174.138.35.107)

## Requisitos

- Clave SSH: `SSH/private_key_sebas.pem`
- Usuario: `root`
- DNS: registro **A** `rollblack-legacy.onesv.online` → `174.138.35.107` (sin proxy Cloudflare en 80/443 si usas Let's Encrypt HTTP challenge)
- Opcional dashboard Traefik: **A** `traefik.1emu.fun` → `174.138.35.107`

## Conexión SSH

```powershell
icacls ".\SSH\private_key_sebas.pem" /inheritance:r /grant:r "$($env:USERNAME):(R)"
ssh -i ".\SSH\private_key_sebas.pem" root@174.138.35.107
```

## Variables

Copiar `.env.example` a `.env` y usar al menos:

```env
WORLD_PUBLIC_HOST=174.138.35.107
AUTH_PORT=2450
WORLD_PORT=5557
MYSQL_PUBLISH_HOST=0.0.0.0
```

## Despliegue automático (Windows)

```powershell
.\scripts\deploy-vps.ps1
```

Sincroniza el repo a `/opt/dofus-2.0.0`, instala Docker si falta, crea `traefik_web` y levanta el stack completo.

Solo Traefik:

```powershell
.\scripts\deploy-vps.ps1 -SkipSync -TraefikOnly
```

## Despliegue manual (en el VPS)

```bash
docker network create traefik_web 2>/dev/null || true
cd /opt/dofus-2.0.0/docker
docker compose --env-file ../.env \
  -f docker-compose.yml \
  -f docker-compose.vps.yml \
  -f docker-compose-onelauncher-api.yml \
  -f docker-compose-website.yml \
  up -d --build
docker compose -f docker-compose-traefik.yml up -d
```

## Puertos publicados

| Servicio | Puerto |
|----------|--------|
| Auth (Sunshine) | 2450 |
| World (Sunshine) | 5557 |
| MariaDB | 3306 |
| HTTP / HTTPS (Traefik) | 80 / 443 |

## Navicat (MySQL remoto)

Usuario dedicado en `.env`:

- `MYSQL_REMOTE_USER=sunshine_remote`
- `MYSQL_REMOTE_PASSWORD=change-me-remote` (cámbiala en producción)

Aplicar o actualizar permisos en el VPS:

```powershell
.\scripts\grant-navicat-remote.ps1
```

Conexión en Navicat:

| Campo | Valor |
|-------|--------|
| Host | `174.138.35.107` |
| Puerto | `3306` |
| Usuario | `sunshine_remote` |
| Contraseña | valor de `MYSQL_REMOTE_PASSWORD` en `.env` |
| Base de datos | `sunshine` |

Tipo: **MySQL** o **MariaDB**. SSL: desactivado (salvo que configures certificados).

Abre el puerto **3306** en el firewall del proveedor (DigitalOcean, etc.) si la conexión falla por timeout.

## OneLauncher API

- URL publica: `https://rollblack-legacy.onesv.online/api/health`
- Contenedor: `onelauncher-api`
- Documentacion: [onelauncher-api.md](onelauncher-api.md)

Variables en `.env`:

```env
ONELAUNCHER_API_PUBLIC_URL=https://rollblack-legacy.onesv.online
WEBSITE_API_BASE_URL=http://onelauncher-api:8080
```

El CMS ya no escribe directo en MySQL para registro/login; usa la API interna.

## Cliente y Uplauncher

- `Client2.3.7/config.xml`: `connection.host=174.138.35.107`, `connection.port=2450`
- Uplauncher se ejecuta en Windows del jugador; no hay contenedor en el VPS.

## Comprobaciones

```powershell
Test-NetConnection 174.138.35.107 -Port 2450
Test-NetConnection 174.138.35.107 -Port 5557
curl -I https://rollblack-legacy.onesv.online
```

En el VPS:

```bash
docker ps
docker logs sunshine-server --tail 50
docker logs rollblack-website --tail 30
docker logs onelauncher-api --tail 30
curl -s https://rollblack-legacy.onesv.online/api/health
```
