# Stabilization Gate — antes de Phase 7C

Date: `2026-06-03`  
Branch: `feature/items-builder-vps-qa-stabilization`  
Scope: entorno Admin API + builds (sin implementar 7C)

## Veredicto

| Criterio | Estado |
| --- | --- |
| Sin proceso bloqueando build | **PASSED** |
| `dotnet build Sunshine.sln` | **PASSED** (0 errors) |
| Warnings clasificados | **PASSED** |
| Admin API → VPS (`isRemote=true`) | **PASSED** |
| Endpoints mínimos JSON | **PASSED** |
| `npm run build` Angular | **PASSED** |
| Phase 7C iniciada | **NO** |

**Decisión: Phase 7C puede iniciarse.**

---

## 1. Build lock diagnostic

### Síntoma

```txt
MSB3027: Could not copy ... RollblackLegacy.Admin.Api.exe
The file is locked by: RollblackLegacy.Admin.Api (57660)
```

### Causa

Instancia viva de `RollblackLegacy.Admin.Api` (o Visual Studio con el proceso adjunto) mantiene bloqueado `bin\Debug\net8.0\RollblackLegacy.Admin.Api.exe` y `apphost.exe`.

### Mitigación (operativa)

```powershell
Get-Process RollblackLegacy.Admin.Api -ErrorAction SilentlyContinue
Stop-Process -Name RollblackLegacy.Admin.Api -Force
# validar vacío:
Get-Process RollblackLegacy.Admin.Api -ErrorAction SilentlyContinue
```

Ejecutado `2026-06-03`: PID `57660` detenido; build Admin API posterior sin lock.

### Regla de equipo

No ejecutar `dotnet build` sobre `RollblackLegacy.Admin.Api` mientras `dotnet run` del mismo proyecto sigue activo. Detener API antes de rebuild o usar `Ctrl+C` en la terminal del host.

---

## 2. Warnings classification

### Sunshine.sln (`dotnet build`, 2026-06-03)

| Warning | Project | File | Category | Decision | Action |
| --- | --- | --- | --- | --- | --- |
| CA1416 `WindowsIdentity.GetCurrent()` | Sunshine | `Sunshine.BaseServer/FirewallManager.cs:87` | Platform-specific API | **IGNORED_KNOWN** | Servidor Windows; fuera de Admin Items |
| CA1416 `WindowsPrincipal` | Sunshine | `FirewallManager.cs:89` | Platform-specific API | **IGNORED_KNOWN** | Idem |
| CA1416 `WindowsPrincipal.IsInRole` | Sunshine | `FirewallManager.cs:90` | Platform-specific API | **IGNORED_KNOWN** | Idem |
| CA1416 `WindowsBuiltInRole.Administrator` | Sunshine | `FirewallManager.cs:90` | Platform-specific API | **IGNORED_KNOWN** | Idem |

**Total solution:** 4 warnings, 0 errors.

### Admin API (`RollblackLegacy.Admin.Api.csproj`)

| Result | Decision |
| --- | --- |
| 0 warnings, 0 errors (con API detenida) | **PASSED** |

### MSB3026 / MSB3027 (file copy retry)

| Warning | Category | Decision | Action |
| --- | --- | --- | --- |
| MSB3026/3027 apphost/DLL locked | Build hygiene | **CRITICAL** (cuando ocurre) | `Stop-Process` Admin API; no es defecto de código |

### Angular (`npm run build`)

| Warning | Category | Decision | Action |
| --- | --- | --- | --- |
| Initial bundle 506.79 kB > budget 500 kB (+6.79 kB) | Bundle size | **CAN_DEFER** | Phase 7C puede abordar si crece; no rompe build |

### No observados en este gate

- Nullable reference warnings en Admin: 0 en build limpio.
- Secrets en archivos tracked: no detectados (password solo en `appsettings.Development.local.json` ignorado).

---

## 3. VPS DB target validation

### Archivo activo (no commiteable)

`Angular-tools/Admin/RollblackLegacy.Admin.Api/appsettings.Development.local.json`

Verificado (sin exponer password):

| Campo | Valor esperado | Observado |
| --- | --- | --- |
| Server | `174.138.35.107` | OK |
| Port | `3306` | OK |
| Database | `sunshine` | OK |
| User ID | `sunshine_remote` | OK |

Plantilla tracked: `appsettings.Development.vps.example.json`  
Guía: [vps-database-connection.md](../../infrastructure/vps-database-connection.md)

### `GET /api/admin/v1/health/db` (2026-06-03)

```json
{
  "status": "ok",
  "database": "sunshine",
  "host": "174.138.35.107",
  "port": 3306,
  "user": "sunshine_remote",
  "isRemote": true
}
```

---

## 4. Endpoint validation

Base: `http://127.0.0.1:5248/api/admin/v1` (API levantada solo para probe; detenida al cerrar gate).

| Endpoint | HTTP | JSON válido | Notas |
| --- | ---: | --- | --- |
| `GET /items?page=1&pageSize=2` | 200 | Sí | `totalCount=6654` (VPS) |
| `GET /items/12616` | 200 | Sí | `ADMIN TEST`, `iconId=1003`, `appearanceId=1004` |
| `GET /items/12616/effects/edit` | 200 | Sí | 1 fila en VPS al momento del probe |

**Nota VPS:** en el probe, `GET /items/12616` devolvió `effects: []` mientras `effects/edit` tenía 1 fila. Puede deberse a estado distinto del read model vs columna `Effects` en VPS respecto al QA local A.8. No bloquea el gate; revisar en 7C/QA si persiste.

---

## 5. Build commands (referencia)

```powershell
cd C:\Users\Hombr\source\repos\DofusLegacy2.3.7
Stop-Process -Name RollblackLegacy.Admin.Api -Force -ErrorAction SilentlyContinue
dotnet clean "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln"
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj"
cd Angular-tools/Admin/RollblackLegacy.Admin.Angular
npm run build
```

---

## 6. Phase 7C — autorización

| Gate | Listo |
| --- | --- |
| Build estable | Sí |
| VPS confirmado | Sí |
| A.8 QA | Sí (`items-builder-a8-qa-item-12616.md`) |
| 7C implementado | **No** (correcto) |

**Siguiente slice:** Phase 7C — Form UX Polish (toasts, layout split, conditions hints, preview warnings, numeric formatting).
