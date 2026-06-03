# Agent Handoff - Client Identity Audit Tool

Generated: `2026-06-03`

Leer este archivo antes de cualquier implementacion.

## Regla obligatoria

No continuar implementacion si este handoff no existe o esta desactualizado.

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/client-identity-angular-diagnostics-phase3
```

Stack Admin: `Angular-tools/Admin/` (no `src/Admin/`)

## Estado Macro 2

```txt
Phase 1 DONE — CLI + scaffold
Phase 2 DONE — Admin API read-only
Stabilization gate PASSED
Phase 3 DONE — Angular diagnostics UI
Phase 4 NEXT — Batch/report diagnostics
```

## Ultimo trabajo (Phase 3)

Commit esperado:

```txt
feat: add client identity diagnostics to item admin
```

Angular:

```txt
client-identity.api.ts / models / status helpers
client-identity-diagnostic-card
client-identity-batch-check-panel (7754,12616,12617,39)
item-detail-page + item-publication-status-page integration
```

## Validacion ejecutada

```txt
dotnet build Sunshine.sln /nr:false -> OK
npm run build (Admin Angular) -> OK
```

Browser QA: operador debe confirmar visualmente:

```txt
/admin/items/7754
/admin/items/12616
/admin/items/12617
/admin/items/7754/publication-status
/admin/items/12617/publication-status
```

## Casos de control

```txt
7754  -> verde / CLIENT_KNOWN
12616 -> warning / NEEDS_CLIENT_PATCH / APPEARANCE_UNKNOWN
12617 -> warning / NEEDS_CLIENT_PATCH
39    -> verde / CLIENT_KNOWN (batch panel)
```

## Prohibiciones

```txt
no worktrees externos
no tocar Client2.3.7 write
no DB writes
no publish workflow
no 44k scan
no Macro 3 (Sprite Preview) hasta cerrar Macro 2 Phase 4+
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
2. Confirmar branch y commit feat Phase 3.
3. Si el usuario pide Phase 4: batch/report diagnostics (docs macro 2).
4. No abrir Macro 3 (Sprite Preview) sin cierre Macro 2.
```

Docs Phase 3: `docs/admin-tools/client-identity/client-identity-angular-diagnostics-phase3.md`
