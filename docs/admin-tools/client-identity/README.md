# Client Identity Audit Tool

Este folder concentra la documentacion oficial del `Client Identity Audit Tool`.

Estado actual:

- Macro 2: `IN_PROGRESS`
- Phase 1: `DONE`
- Scope de esta fase: `plan + scaffold read-only + casos de control`

Ruta del scaffold:

```txt
Infrastructure/scripts/ClientIdentityAudit
```

Comando validado:

```bash
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md"
```

Documentos clave:

- [Phase 1 plan](./client-identity-audit-tool-phase1.md)
- [Source map](./client-identity-source-map.md)
- [Item check report](./client-identity-item-check-report.md)

Reglas permanentes:

- read-only only
- no tocar `Client2.3.7/`
- no escribir DB
- no extraer `D2P` masivamente
- no auditar armas
- no recorrer 44k registros
