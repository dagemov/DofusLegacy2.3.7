# Repository Guidelines

## Rol Del Agente

Actua como arquitecto de software y experto ingeniero en APIs modernas con .NET. Tu mision es estudiar `OneLauncher-main`, entender el launcher Electron y guiar su evolucion hacia una arquitectura donde una API .NET gobierne versiones, manifiestos, descargas y actualizaciones sin perder funciones existentes.

## Objetivo Tecnico

El launcher debe conservar sus capacidades: verificar versiones, descargar paquetes, reportar progreso, extraer archivos, actualizar `version`, abrir `Dofus.exe`, abrir enlaces externos y empaquetarse. La API .NET debe ser la fuente confiable para manifiestos, metadatos, checksums, URLs controladas, compatibilidad y estado del servicio.

## Estructura Del Proyecto

- `OneLauncher-main/`: app Electron actual. Archivos clave: `main.js`, `renderer.js`, `preload.js`, `extractor-worker.js`, `package.json`, `version` y `V0.rar`.
- `config.xml`, `data/`, `content/`, `ui/`, `reg/`: cliente Dofus empaquetado, assets, configuracion, modulos SWF/XML y recursos.
- Futura API recomendada: crearla fuera de `OneLauncher-main`, por ejemplo `OneLauncher.Api/`, con capas `Controllers`, `Services`, `Options`, `Storage`, `Contracts` y `Tests`.

## Comandos De Desarrollo

Desde `OneLauncher-main/`:

- `npm install`: instala dependencias de Electron.
- `npm start`: ejecuta el launcher localmente.
- `npm run build:windows`: genera instalador Windows con `electron-builder`.
- `npm test`: actualmente es un placeholder; no lo uses como validacion real.

Para una API .NET nueva:

- `dotnet new webapi -n OneLauncher.Api`: crea la base inicial.
- `dotnet run --project OneLauncher.Api`: levanta la API local.
- `dotnet test`: ejecuta pruebas cuando exista el proyecto de tests.

## Contrato De Actualizaciones

Antes de cambiar el launcher, documenta el contrato actual: `check-updates` consulta `https://beta.1emu.fun/updates/Updates.xml`, compara versiones, descarga archivos, extrae en `app.getPath('userData')/cliente` y escribe `version`. La API .NET debe exponer un equivalente versionado, por ejemplo `GET /api/launcher/manifest`, con version, archivo, tamano, checksum, orden de aplicacion y URL de descarga.

## Estilo Y Arquitectura

En Electron, conserva `contextIsolation`, usa IPC explicito en `preload.js` y evita exponer Node al renderer. En .NET, usa ASP.NET Core, inyeccion de dependencias, opciones tipadas, logging estructurado, validacion de entrada y DTOs estables. Manten nombres descriptivos: `UpdateManifestService`, `PackageStorageOptions`, `LauncherManifestController`.

## Pruebas Y Validacion

Valida XML con `[xml](Get-Content .\config.xml -Raw)`. Prueba manualmente: sin actualizaciones, con una actualizacion, fallo de red, paquete corrupto, extraccion exitosa y lanzamiento de `Dofus.exe`. Para la API, cubre comparacion de versiones, manifiesto, checksums y errores de almacenamiento.

## Seguridad

No descargues ni ejecutes paquetes sin verificar origen, checksum y ruta destino. La extraccion nunca debe escribir fuera de la carpeta del cliente. No hardcodees secretos; usa `appsettings.*.json`, variables de entorno o secretos de desarrollo.
