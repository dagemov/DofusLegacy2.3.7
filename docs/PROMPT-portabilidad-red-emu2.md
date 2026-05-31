# Portabilidad Docker Sunshine EMU (`red-emu2`)

Este repositorio ahora incluye una capa Docker portable basada en una red explícita `red-emu2`, un `entrypoint.sh` que genera `Config.xml` y `Database.xml` desde `.env`, y overrides separados para `local` y `vps`.

Notas específicas de este checkout:

- El código fuente real de Sunshine sigue viviendo en `Sunshine net11.0/Sunshine net11.0`.
- A fecha del 27 de mayo de 2026, ese proyecto apunta a `net11.0` y el runtime generado referencia `Microsoft.NETCore.App 11.0.0-preview.3`, por lo que el `Dockerfile` usa imágenes `11.0-preview`.
- `database/sunshine.sql` es un placeholder intencional: sustituirlo por el dump real del operador antes de esperar un arranque funcional completo.
