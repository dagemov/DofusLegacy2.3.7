# Migración VPS — Rollblack Legacy (julio 2026)

Documentación del despliegue en la VPS nueva y cambios operativos aplicados en producción.

## Infraestructura

| Recurso | Valor |
|---------|--------|
| **VPS IP** | `34.46.208.124` |
| **Dominio** | `https://rollblack-legacy.onesv.online` |
| **Ruta deploy** | `/opt/dofus-2.0.0` |
| **Usuario SSH** | `sebas` |
| **Stack** | Traefik + MariaDB + Sunshine + OneLauncher API + Website |

## Contenedores Docker

```bash
cd /opt/dofus-2.0.0/docker
sudo docker compose --env-file ../.env \
  -f docker-compose.yml \
  -f docker-compose.vps.yml \
  -f docker-compose-onelauncher-api.yml \
  -f docker-compose-website.yml up -d
sudo docker compose -f docker-compose-traefik.yml up -d
```

| Contenedor | Rol |
|------------|-----|
| `traefik_proxy` | TLS / reverse proxy |
| `sunshine-db` | MariaDB |
| `sunshine-server` | Auth + World (2450 / 5557) |
| `onelauncher-api` | API launcher + parches + electron-updates |
| `rollblack-website` | Web registro / descarga |

## Puertos publicados

| Puerto | Servicio |
|--------|----------|
| 443 / 80 | Traefik (web + API) |
| 2450 | Auth Dofus |
| 5557 | World Dofus |
| 3306 | MySQL (Navicat) |

## OneLauncher — parches sin reiniciar Docker

### Rutas en VPS (WinSCP)

```
/opt/dofus-2.0.0/runtime/packages/
├── Updates.xml      ← manifiesto (version + archivo)
├── V0.rar           ← cliente base (~838 MB)
└── config.zip       ← parche config (IP/puerto auth)
```

### URLs HTTPS

- Manifiesto XML: `https://rollblack-legacy.onesv.online/api/launcher/updates.xml`
- Manifiesto JSON: `https://rollblack-legacy.onesv.online/api/launcher/manifest`
- Descarga parche: `https://rollblack-legacy.onesv.online/api/files/{archivo}`
- Auto-update launcher: `https://rollblack-legacy.onesv.online/api/launcher/electron-updates/latest.yml`

### Publicar parche nuevo

1. Subir `.zip` / `.rar` a `runtime/packages/`.
2. Editar `Updates.xml` con versión mayor.
3. Guardar — **no reiniciar contenedores**.

## Auto-update del launcher (electron-updater)

Tras compilar un launcher nuevo:

1. Subir instalador a `/opt/dofus-2.0.0/runtime/electron-updates/rollblack-legacy.exe`
2. Actualizar `ElectronUpdates__Version` en `docker/docker-compose-onelauncher-api.yml`
3. Reiniciar solo `onelauncher-api`

Versión en producción (jul 2026): **1.0.13**

## Web — descarga del launcher

Configuración en `docker/docker-compose-website.yml`:

```yaml
Website__LauncherDownloadUrl: https://drive.google.com/file/d/1HA6XjvVC4k6lM9c1ln15SUM1PKqNpvh4/view?usp=sharing
```

Instalador: **Onelauncher-Setup-1.0.13.exe**

## Correcciones BD aplicadas en VPS

### 1. Tabla `worlds` (congelamiento tras elegir servidor)

El cliente auth OK pero se congelaba al seleccionar **Helsephine** porque `worlds` apuntaba a la IP/puerto legacy.

```sql
UPDATE worlds SET Address = '34.46.208.124', Port = 5557 WHERE Id = 18;
```

Script: `docker/patches/fix-worlds-vps-new.sql`

### 2. Tiendas virtuales `.tienda` / `.tiendas`

Parche **unified9** (NPCs 9101–9109 + catálogo):

```bash
# En VPS
sudo docker cp /tmp/npc-shop-unified9-apply.sql sunshine-db:/tmp/
sudo docker exec sunshine-db mariadb -uroot -p"$MYSQL_ROOT_PASSWORD" sunshine < /tmp/npc-shop-unified9-apply.sql
sudo docker restart sunshine-server
```

Archivo repo: `database/patches/npc-shop-unified9-apply.sql`  
Script local: `scripts/vps/apply-npc-shop-unified9.ps1`

Verificar: logs deben mostrar `VirtualShopRegistry initialized count=9`.

## Variables `.env` (plantilla)

Copiar `.env.example` → `.env` y ajustar:

- `WORLD_PUBLIC_HOST=34.46.208.124`
- `AUTH_PORT=2450`
- `WORLD_PORT=5557`
- Contraseñas MySQL (`MYSQL_*`) — **no commitear `.env`**

## Deploy desde Windows

```powershell
.\scripts\deploy-vps.ps1 -VpsHost 34.46.208.124 -SshUser sebas -SshKey "..\ssh_key\Public_key_sebas.pem"
```

## Checklist post-deploy

- [ ] `curl https://rollblack-legacy.onesv.online/api/health`
- [ ] `curl https://rollblack-legacy.onesv.online/api/launcher/manifest` → `manifestSource: updates-xml`
- [ ] Puertos 2450 y 5557 accesibles desde cliente
- [ ] `SELECT Address, Port FROM worlds WHERE Id=18` → IP VPS + 5557
- [ ] `VirtualShopRegistry count=9` en logs sunshine
- [ ] Login web + launcher + entrada al world

## Notas

- Login launcher ≠ login in-game (credenciales en pantalla Dofus contra auth `:2450`).
- Rama **main** incluye fixes de gameplay posteriores a **Yaco**; esta doc refleja el estado operativo de la migración VPS.
