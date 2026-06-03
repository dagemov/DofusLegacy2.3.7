# Agent Handoff - Client Identity Audit Tool

Generated: `2026-06-03`

Leer este archivo antes de cualquier implementacion.

## Regla obligatoria

No continuar implementacion si este handoff no existe o esta desactualizado.

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-identity-batch-report-phase4
```

Stack Admin: `Angular-tools/Admin/` (no `src/Admin/`)

## Estado Macro 2

```txt
Phase 1 DONE — CLI + scaffold
Phase 2 DONE — Admin API read-only
Stabilization gate PASSED
Phase 3 DONE — Angular diagnostics UI
Phase 4 DONE — Batch/report por lista explícita (máx. 100 IDs)
Macro 2 COMPLETE — pendiente QA navegador del operador
Macro 3 NEXT — Sprite Preview solo con aprobación explícita
```

## Ultimo trabajo (Phase 4)

Commit esperado:

```txt
feat: add client identity batch diagnostics report
```

Entregables:

```txt
ClientItemIdentityIdParser + ClientItemIdentityBatchLimits (máx. 100)
GET /api/admin/v1/client-identity/items/check?ids=...
CLI: --items, --input-file, --output, --format markdown|csv
Angular: client-identity-batch-check-panel (textarea, tabla, contadores, copiar CSV)
docs/admin-tools/client-identity/client-identity-batch-report-phase4.md
docs/admin-tools/client-identity/client-identity-batch-report-sample.md
```

## Validacion ejecutada

```txt
dotnet build Sunshine.sln /nr:false -> OK
dotnet run ClientIdentityAudit --items 7754,12616,12617,39 -> sample.md OK
npm run build (Admin Angular) -> OK (budget warning +589 bytes)
```

Browser QA pendiente (operador):

```txt
/admin/items/7754 -> expandir Auditoría batch controlada
ingresar 7754,12616,12617,39 -> ejecutar -> badges y contadores
probar >100 IDs -> error en español (UI) / 422 (API)
```

## Casos de control

```txt
7754  -> verde / CLIENT_KNOWN
12616 -> warning / NEEDS_CLIENT_PATCH / APPEARANCE_UNKNOWN
12617 -> warning / NEEDS_CLIENT_PATCH
39    -> verde / CLIENT_KNOWN
```

## Prohibiciones

```txt
no worktrees externos
no tocar Client2.3.7 write
no DB writes
no publish workflow
no 44k scan
no Macro 3 sin aprobación explícita
```

## Archivos ajenos — no tocar

```txt
Sunshine net11.0/.../WorldServerManager.cs
Client2.3.7/cliente*
config/Database*.xml (local)
```

## Siguiente agente

```txt
1. Leer este handoff.
2. Confirmar commit feat Phase 4 en feature/client-identity-batch-report-phase4.
3. Si el usuario pide Macro 3: requerir aprobación explícita antes de implementar.
4. Items Builder Macro 1 Phase 7C u otras fases: solo si el usuario lo pide.
```

Docs Phase 4: `docs/admin-tools/client-identity/client-identity-batch-report-phase4.md`
