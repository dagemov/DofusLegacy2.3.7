# Build y auto-update del launcher

## Carpetas en el VPS

| Puerto | Carpeta | Contenido |
|--------|---------|-----------|
| **8090** | `runtime/parches-public/` | Juego: `Updates.xml`, `basic_client.zip` |
| **8091** | `runtime/launcher-releases/` | Launcher: `latest.yml`, `Onelauncher-Setup-x.y.z.exe` |

## Compilar instalador Windows

```powershell
cd OneLauncher\OneLauncher-main
npm install
npm run build:windows
```

Salida: `dist\Onelauncher-Setup-x.y.z.exe`, `dist\latest.yml`, `dist\*.blockmap` (ej. `1.0.5`)

## Publicar release en el VPS

```powershell
npm run publish:vps
```

Sube los artefactos a `http://174.138.35.107:8091/`.

## Flujo de actualizacion

1. **Launcher** (al abrir el .exe instalado): `electron-updater` lee `latest.yml` del puerto **8091**.
2. **Cliente Dofus** (despues): `Updates.xml` del puerto **8090** → `%APPDATA%\Onelauncher\cliente\`.

En `npm start` (desarrollo) no se aplica auto-update del launcher.

## Nueva version del launcher

1. Sube `version` en `package.json` (ej. `1.0.1`).
2. `npm run build:windows`
3. `npm run publish:vps`
4. Los jugadores reciben la actualizacion al abrir el launcher.
