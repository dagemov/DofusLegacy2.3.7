# Launcher Style Adaptation

## Inventario revisado

UI auditada en:

- `Uplauncher net11.0/Uplauncher net11.0/Uplauncher/UI`

Assets reutilizados directamente:

- `win.png` como referencia visual de shell/panel.
- `state/on.png`
- `state/off.png`

## Adaptacion aplicada

No se hizo copia exacta del launcher. Se recreo una direccion visual propia basada en:

- fondos marron oscuro / madera quemada
- bordes metal/piedra
- acentos lima y dorado
- paneles redondeados con sombra profunda
- tipografia `Cinzel` para cabeceras
- tipografia `Work Sans` para lectura

## Marca del sitio

Se creo `wwwroot/images/branding/rollblack-mascot.svg` como brand mark del sitio y se usa para:

- logo del header
- hero art
- favicon SVG

## Atomic Design usado

- `Views/Shared/Atoms`
- `Views/Shared/Molecules`
- `Views/Shared/Organisms`
- `Views/Shared/Templates`

## Pendientes visuales

- enlazar una ilustracion final aprobada por branding
- sustituir placeholders de noticias y Discord
- incorporar estados reales del servidor
- refinar microanimaciones y transiciones HTMX
