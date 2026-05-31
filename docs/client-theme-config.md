# Cliente Dofus 2.3.7: tema y config

## Error típico en consola

```
Unable to load file://data/common/data.meta
Unable to load file://data/i18n/data.meta
Theme "dofus1" loaded
Theme "black" loaded
```

Eso **no es un fallo del tema en sí**: faltan los índices `data.meta` que el cliente AIR necesita para cargar los `.d2o` / `.d2i`. Los temas pueden cargarse igual, pero el juego no arranca bien sin esos archivos.

## Reparación rápida

```powershell
.\scripts\repair-client-data.ps1
```

Descarga `data.meta` desde el CDN del Uplauncher y los descomprime si vienen en gzip.

Alternativa: abrir **Uplauncher** y dejar que actualice el cliente (usa `data/Launcher/VerInfo.rec`).

## Tema y `config.xml`

El cliente usa dos capas:

| Pieza | Ubicación | Rol |
|-------|-----------|-----|
| Tema activo | `config.xml` → `ui.skin` | Carpeta bajo `content/themes/` (assets, css, bitmap) |
| Colores UI | `content/themes/<tema>/colors.xml` | Claves `[colors.xxx]` usadas en XML de la UI |

Tema por defecto recomendado en este repo:

```xml
<entry key="theme.path">./content/themes/</entry>
<entry key="ui.skin">[config.theme.path]dofus1/</entry>
```

Para tema oscuro:

```xml
<entry key="ui.skin">[config.theme.path]black/</entry>
```

### Regla de Charly (colores)

Si modificas un tema (CSS, bitmaps, SWF):

1. Edita `content/themes/<tema>/css/*.css`
2. Actualiza las mismas claves en `content/themes/<tema>/colors.xml` (por ejemplo `grid.over`, `ui.bg.pregame`, `contextmenu.bg`)
3. Los XML de UI en `ui/` referencian `[colors.grid.line]` — si cambias colores solo en CSS pero no en `colors.xml`, la interfaz queda inconsistente

No mezcles assets de `dofus1` con `colors.xml` de `black` (o al revés).

## Conexión al servidor

En `config.xml` (raíz del cliente):

```xml
<entry key="connection.host">174.138.35.107</entry>
<entry key="connection.port">2450</entry>
```

Cuenta de prueba: usuario `rollblack`, contraseña `Rollblack2026`.

## Checklist antes de abrir el juego

1. Existe `data/common/data.meta` (XML, no gzip)
2. Existe `data/i18n/data.meta`
3. `ui.skin` apunta al tema que quieres (`dofus1/` o `black/`)
4. Carpeta del tema tiene `assets.swf`, `colors.xml`, `css/`, `bitmap/`
5. Ejecutar `Dofus.exe` desde la carpeta `Client2.3.7`
