# Client Identity Audit Tool

Este folder concentra la documentacion oficial del `Client Identity Audit Tool`.

Estado actual:

- Macro 2: `COMPLETE` (pendiente confirmación QA navegador del operador)
- Phase 1: `DONE`
- Phase 2: `DONE`
- Phase 3: `DONE` (Angular diagnostics)
- Phase 4: `DONE` (batch/report por lista explícita, máx. 100 IDs)
- Stabilization gate before Phase 3: `PASSED`
- Scope actual: `scaffold + Admin API + Angular read-only diagnostics + batch controlado`
- Macro 3 (Sprite Preview): `NEXT` solo con aprobación explícita

Ruta del scaffold:

```txt
Infrastructure/scripts/ClientIdentityAudit
```

Comando validado:

```bash
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-batch-report-sample.md" --format markdown
```

Endpoints validados:

```txt
GET /api/admin/v1/client-identity/items/{itemId}
GET /api/admin/v1/client-identity/items/check?ids=7754,12616,12617,39
GET /api/admin/v1/items/{itemId}/publication-status
```

Documentos clave:

- [Phase 1 plan](./client-identity-audit-tool-phase1.md)
- [Phase 2 admin layer](./client-identity-admin-layer-phase2.md)
- [API contracts](./client-identity-api-contracts.md)
- [Stabilization gate before Phase 3](./client-identity-stabilization-gate-before-phase3.md)
- [Phase 3 Angular diagnostics](./client-identity-angular-diagnostics-phase3.md)
- [Phase 4 batch/report](./client-identity-batch-report-phase4.md)
- [Batch report sample](./client-identity-batch-report-sample.md)
- [Source map](./client-identity-source-map.md)
- [Item check report](./client-identity-item-check-report.md)

Reglas permanentes:

- read-only only
- no tocar `Client2.3.7/`
- no escribir DB
- no extraer `D2P` masivamente
- no auditar armas
- no recorrer 44k registros
- UI diagnostica solo en Angular (Phase 3); sin writes
