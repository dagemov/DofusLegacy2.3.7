# Stabilization Gate - Admin API before Client Identity Phase 3

## Estado

- Macro 2: `IN_PROGRESS`
- Phase 2: `DONE`
- Stabilization gate before Phase 3: `PASSED`

## Objetivo

Cerrar el gate de compilación y clasificar los warnings antes de abrir:

```txt
Macro 2 / Phase 3 - Angular client identity diagnostics integration
```

## Hallazgo principal

El problema real no era un bug del código de `Client Identity`, sino un bloqueo temporal de artefactos de compilación en:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Application/obj/Debug/net8.0/RollblackLegacy.Admin.Application.dll
```

Error reproducido:

```txt
CSC error CS2012: cannot open ... RollblackLegacy.Admin.Application.dll for writing because it is being used by another process
```

Síntoma relacionado reportado por el usuario:

```txt
MSB3027 / MSB3021 / MSB3026
archivo bloqueado por RollblackLegacy.Admin.Api
```

## Root cause

La causa raíz observada en esta sesión fue:

- procesos persistentes de `MSBuild.dll` con `nodeReuse:true`
- `VBCSCompiler.exe` vivo
- build aislado del Admin API intentando recompilar mientras el compiler server retenía outputs intermedios

No había `dotnet run` vivo de `RollblackLegacy.Admin.Api` al inicio del gate.

## Resolución aplicada

1. confirmación de que no había proceso `RollblackLegacy.Admin.Api`
2. inspección de procesos `dotnet.exe`, `MSBuild.dll` y `VBCSCompiler.exe`
3. ejecución de:

```powershell
dotnet build-server shutdown
```

4. ejecución de builds con:

```powershell
/nr:false
```

5. clean explícito:

```powershell
dotnet clean "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false
```

## Regla operativa fijada

Para evitar repetir este gate:

```txt
No ejecutar dotnet build del Admin API mientras dotnet run del Admin API siga vivo.
Si reaparece un lock de obj/bin:
1. cerrar dotnet run
2. ejecutar dotnet build-server shutdown
3. repetir build con /nr:false
```

## Warnings clasificados

| WarningCode | Project | File | Line | Message | Category | Decision | Action |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `CS2012` | `RollblackLegacy.Admin.Application` | `obj/Debug/net8.0/RollblackLegacy.Admin.Application.dll` | n/a | archivo bloqueado por otro proceso | `CRITICAL` | `FIX_NOW` | resuelto cerrando compiler server y usando `/nr:false` |
| `CA1416` | `Sunshine` | `Sunshine.BaseServer/FirewallManager.cs` | `87, 89, 90` | APIs Windows-only alcanzables en build cross-platform | `KNOWN_EXTERNAL` | `DEFER` | no tocar en este gate porque no es parte del Admin API ni del trabajo reciente |
| `NETSDK1057` | múltiple | SDK targets | n/a | uso de SDK preview | `KNOWN_EXTERNAL` | `DEFER` | mensaje informativo del entorno local, no warning funcional del Admin API |

## Resultado final del gate

Validación ejecutada:

```powershell
dotnet clean "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj" /nr:false
```

Resultado:

- `0` errores `MSB3027/MSB3021/MSB3026`
- `0` errores `CS2012`
- build del `Admin.Api.csproj`: `OK`
- build de `Sunshine.sln`: `OK`

Smoke test posterior:

```txt
GET /api/admin/v1/client-identity/items/7754 -> OK
GET /api/admin/v1/client-identity/items/12617 -> OK
GET /api/admin/v1/items/7754/publication-status -> OK
```

## Decisión

```txt
Phase 3 autorizada
```

Macro 2 / Phase 3 puede empezar cuando el usuario la pida explícitamente.
