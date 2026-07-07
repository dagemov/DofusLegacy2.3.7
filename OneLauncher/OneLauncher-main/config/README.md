# Configuracion del launcher (VPS Rollblack)

Variables de entorno opcionales (proceso Electron / API):

| Variable | Valor por defecto |
|----------|-------------------|
| `ONELAUNCHER_API_BASE_URL` | `https://rollblack-legacy.onesv.online` |
| `ONELAUNCHER_LEGACY_UPDATE_BASE_URL` | `http://174.138.35.107:8090/` |

El launcher intenta primero `GET /api/launcher/manifest`. Si la API no responde, usa `Updates.xml` en el puerto **8090** (carpeta WinSCP: `/opt/dofus-2.0.0/runtime/parches-public/`).

Ejemplo en PowerShell antes de abrir el launcher:

```powershell
$env:ONELAUNCHER_LEGACY_UPDATE_BASE_URL = "http://174.138.35.107:8090/"
```
