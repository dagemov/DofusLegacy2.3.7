# Website Setup

## Proyectos

- `RollblackLegacy.Website`
- `RollblackLegacy.Website.Application`
- `RollblackLegacy.Website.Domain`
- `RollblackLegacy.Website.Infrastructure`
- `RollblackLegacy.Website.Contracts`

Todos quedaron agregados a:

- `Sunshine net11.0/Sunshine net11.0/Sunshine.sln`

## Configuracion

Archivos principales:

- `RollblackLegacy.Website/appsettings.json`
- `RollblackLegacy.Website/appsettings.Development.json`
- `RollblackLegacy.Website/appsettings.Development.example.json`

Connection string usada:

```json
"ConnectionStrings": {
  "SunshineAuth": "Server=127.0.0.1;Port=3306;Database=sunshine;User ID=sunshine;Password=change-me-app;Allow User Variables=true;TreatTinyAsBoolean=true"
}
```

## SCSS

Carpeta fuente:

- `RollblackLegacy.Website/wwwroot/scss`

Compilacion:

```powershell
Set-Location .\RollblackLegacy.Website
npm install
npm run build:scss
```

Watch:

```powershell
npm run watch:scss
```

## Run local

1. Levantar MariaDB local:

```powershell
Set-Location .\docker
docker compose --env-file ..\.env -f docker-compose.yml -f docker-compose.local.yml up -d db
```

2. Ejecutar el website:

```powershell
dotnet run --project .\RollblackLegacy.Website\RollblackLegacy.Website.csproj --urls http://127.0.0.1:5081
```

## Pruebas recomendadas

1. Abrir `/`.
2. Abrir `/account/register`.
3. Registrar una cuenta valida.
4. Verificar fila en `accounts`.
5. Verificar correo en `website_account_contacts`.
6. Probar username duplicado.
7. Probar email invalido.

## Validacion ejecutada en esta fase

- `dotnet build Sunshine.sln`
- `npm run build:scss`
- GET Home
- GET Register
- POST HTMX valido
- comprobacion SQL del hash y del correo
- POST username duplicado
- POST email invalido
