# Legacy Website Roadmap

## Objetivo de esta fase

Levantar el primer modulo web publico del servidor con:

- `ASP.NET Core MVC`
- `HTMX`
- `Bootstrap`
- `SCSS`
- separacion por capas (`Website`, `Application`, `Domain`, `Infrastructure`, `Contracts`)

## Alcance entregado

- Home publica funcional.
- Layout principal inspirado en el launcher.
- Registro de cuenta en `/account/register`.
- Conexion a la base auth real.
- Hash compatible con Sunshine.
- Validacion server-side y HTMX para el submit parcial.
- Documentacion base del modulo.

## Arquitectura elegida

La fase usa una Clean Architecture ligera:

- `RollblackLegacy.Website`: MVC, Razor, HTMX, composicion visual y DI.
- `RollblackLegacy.Website.Application`: caso de uso de registro.
- `RollblackLegacy.Website.Domain`: modelo de alta de cuenta y capacidades del esquema.
- `RollblackLegacy.Website.Infrastructure`: Dapper + MySQL + hash compatible Sunshine.
- `RollblackLegacy.Website.Contracts`: DTOs, view models y contratos de UI.

## Siguientes pasos sugeridos

1. Reemplazar placeholders de noticias por contenido real.
2. Enlazar estado auth/world a una telemetria o health endpoint.
3. Agregar login web y gestion de cuenta.
4. Formalizar recuperacion/cambio de respuesta secreta.
5. Valorar migracion de `accounts.Password` a un esquema mas fuerte cuando el cliente/emulador lo permitan.
