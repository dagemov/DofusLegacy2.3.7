# Client Identity Audit Tool

Este folder concentra la documentacion oficial del `Client Identity Audit Tool`.

Estado actual:

- Macro 2: `IN_PROGRESS`
- Phase 1: `DONE`
- Phase 2: `DONE`
- Scope actual: `scaffold + capa reusable read-only + Admin API`

Ruta del scaffold:

```txt
Infrastructure/scripts/ClientIdentityAudit
```

Comando validado:

```bash
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md"
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
- [Source map](./client-identity-source-map.md)
- [Item check report](./client-identity-item-check-report.md)

Reglas permanentes:

- read-only only
- no tocar `Client2.3.7/`
- no escribir DB
- no extraer `D2P` masivamente
- no auditar armas
- no recorrer 44k registros
- no agregar UI hasta fase explicita
