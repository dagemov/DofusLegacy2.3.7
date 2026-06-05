# Macro 2 / Phase 4 — Batch & Report Client Identity Diagnostics

Estado: `DONE` (pendiente QA visual del operador en navegador)

## Objetivo

Permitir auditorías batch controladas por lista explícita de ItemIds (máximo 100), sin escanear el catálogo completo (~44k).

## Alcance entregado

### 1. API batch seguro

Endpoint:

```txt
GET /api/admin/v1/client-identity/items/check?ids=7754,12616,12617,39
```

Reglas (`ClientItemIdentityIdParser` + `ClientItemIdentityBatchLimits`):

- Máximo 100 IDs por request (HTTP 422 si se excede).
- Rechazo de entrada vacía (HTTP 400).
- Rechazo de tokens no numéricos o IDs ≤ 0 (HTTP 400).
- Sin escaneo de tabla completa; solo IDs solicitados.
- Errores como `ProblemDetails` con `traceId` vía `AdminApiExceptionHandler`.

### 2. CLI batch report

Proyecto: `Infrastructure/scripts/ClientIdentityAudit`

Opciones:

```bash
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- \
  --items 7754,12616,12617,39 \
  --input-file path/to/itemids.txt \
  --output docs/admin-tools/client-identity/client-identity-batch-report-sample.md \
  --format markdown
```

- `--format markdown|csv`
- Sin scan global; reutiliza el mismo servicio de lectura que la API.

### 3. Angular batch panel

Componente: `client-identity-batch-check-panel`

- Textarea con IDs por coma, espacio o salto de línea.
- Botón **Ejecutar auditoría**.
- Tabla de resultados con badges de estado.
- Contadores por `primaryStatus`.
- **Copiar CSV** al portapapeles.
- Validación cliente: máximo 100 IDs (mensaje en español antes de llamar a la API).

Integrado en detalle de ítem (`/admin/items/:id`).

### 4. Reporte de muestra

- [client-identity-batch-report-sample.md](./client-identity-batch-report-sample.md)

Casos QA:

| ItemId | Resultado esperado |
| --- | --- |
| 7754 | Verde / cliente conoce |
| 12616 | Warning / needs patch + appearance |
| 12617 | Warning / needs patch |
| 39 | Verde / cliente conoce |

## Validación técnica

```bash
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-batch-report-sample.md" --format markdown
cd Angular-tools/Admin/RollblackLegacy.Admin.Angular && npm run build
```

## QA navegador (operador)

1. Abrir `/admin/items/7754`.
2. Expandir **Auditoría batch controlada**.
3. Ingresar `7754,12616,12617,39` y ejecutar.
4. Verificar badges, contadores y tabla.
5. Probar >100 IDs y confirmar mensaje de error en español (UI) o 422 (API).

## Prohibiciones respetadas

- No tocar cliente (`Client2.3.7` write).
- No escribir DB.
- No publish / Macro 3.
- No scan 44k.

## Rama

```txt
feature/client-identity-batch-report-phase4
```

Commit esperado:

```txt
feat: add client identity batch diagnostics report
```
