# Instalador del launcher (electron-updater)

Subir aquí el instalador Windows renombrado como `rollblack-legacy.exe`.

VPS: `/opt/dofus-2.0.0/runtime/electron-updates/rollblack-legacy.exe`

Tras subir, actualizar `ElectronUpdates__Version` en `docker/docker-compose-onelauncher-api.yml` y reiniciar `onelauncher-api`.

Feed: `https://rollblack-legacy.onesv.online/api/launcher/electron-updates/latest.yml`
