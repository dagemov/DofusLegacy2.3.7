# Stitch MCP y el CMS

## Por que el agente no aplico diseño via Stitch

En Cursor, el servidor **stitch** puede aparecer como conectado pero mostrar:

> No tools, prompts, or resources

Sin herramientas expuestas, el agente **no puede** llamar a Stitch para generar pantallas. En ese caso el tema se aplica copiando tokens y assets de `OneLauncher-main/` (como en `launcher-theme.css`).

## Configuracion recomendada

1. En **Settings → MCP → stitch**, usar el flujo **Login** de Google Stitch (OAuth), no solo URL + API key en `mcp.json`.
2. Tras login correcto, debe listarse al menos una herramienta (p. ej. generacion de UI).
3. Reiniciar Cursor o recargar ventana si sigue en "No tools".

## Tema web actual (sin Stitch)

- Clase global `site-launcher` en el layout publico.
- Hoja [launcher-theme.css](../../RollblackLegacy.Website/wwwroot/css/launcher-theme.css) sobrescribe colores lime/Cinzel de `site.css`.
- Assets en `/images/launcher-branding/` desde el launcher.

## Despliegue

Tras cambiar CSS, reconstruir el contenedor web:

```bash
cd docker
docker compose --env-file ../.env \
  -f docker-compose.yml -f docker-compose.vps.yml \
  -f docker-compose-onelauncher-api.yml \
  -f docker-compose-website.yml \
  up -d --build website
```

En el navegador: recarga forzada (Ctrl+F5) para evitar cache de `site.css`.

## Seguridad

No guardes API keys de Google en el repositorio. Si una clave quedo en `mcp.json` local, rotala en Google Cloud y usa solo la configuracion OAuth de Cursor.
