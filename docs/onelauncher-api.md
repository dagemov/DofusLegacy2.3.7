# OneLauncher API

API ASP.NET Core (`OneLauncher.Api`) que centraliza manifiesto de actualizaciones, descarga de paquetes y autenticacion de cuentas Sunshine.

## Endpoints

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| GET | `/api/health` | Estado del servicio |
| GET | `/api/launcher/manifest` | Manifiesto JSON de paquetes |
| GET | `/api/files/{packageName}` | Descarga de paquete configurado |
| POST | `/api/auth/register` | Alta de cuenta (`username`, `email`, `password`, `confirmPassword`) |
| POST | `/api/auth/login` | Validacion de credenciales |
| GET | `/api/auth/check-username?username=` | Disponibilidad de nombre |

## Hash de contrasena

Compatible con Sunshine: `accounts.Password = MD5(passwordPlano)` en ASCII hex minusculas.

## URLs publicas (VPS)

- Base: `https://rollblack-legacy.onesv.online`
- API: `https://rollblack-legacy.onesv.online/api/...`
- Traefik enruta `PathPrefix(/api)` al contenedor `onelauncher-api` (prioridad 100).

## Variables de entorno

```env
ONELAUNCHER_API_PUBLIC_URL=https://rollblack-legacy.onesv.online
WEBSITE_API_BASE_URL=http://onelauncher-api:8080
ONELAUNCHER_CORS_ORIGINS=https://rollblack-legacy.onesv.online
```

## Desarrollo local

```powershell
cd OneLauncher\OneLauncher.Api
$env:ConnectionStrings__SunshineAuth="Server=127.0.0.1;Port=3306;Database=sunshine;User ID=sunshine;Password=change-me-app;Allow User Variables=true;TreatTinyAsBoolean=true"
dotnet run
```

Launcher Electron (API local):

```powershell
$env:ONELAUNCHER_API_BASE_URL="http://localhost:5074"
cd OneLauncher\OneLauncher-main
npm start
```

## Docker

```bash
cd docker
docker compose --env-file ../.env \
  -f docker-compose.yml \
  -f docker-compose.vps.yml \
  -f docker-compose-onelauncher-api.yml \
  -f docker-compose-website.yml \
  up -d --build onelauncher-api
```

Paquetes reales: colocar archivos en `runtime/packages/` (montado en `/packages` del contenedor).

## Checklist de validacion

1. `curl https://rollblack-legacy.onesv.online/api/health`
2. `curl https://rollblack-legacy.onesv.online/api/launcher/manifest`
3. Registro web en `/account/register` crea fila en `accounts`
4. Login web en `/account/login` establece cookie de sesion
5. Launcher: pill API online y login/registro en panel Cuenta
6. Cliente Dofus: login en auth :2450 con la misma cuenta
