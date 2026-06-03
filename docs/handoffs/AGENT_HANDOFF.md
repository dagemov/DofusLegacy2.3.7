# Agent Handoff - Client Identity Audit Tool

Generated: `2026-06-03`

Leer este archivo antes de cualquier implementacion.

## Regla obligatoria

No continuar implementacion si este handoff no existe o esta desactualizado.

El siguiente agente debe:

1. leer este handoff completo
2. confirmar repo, branch, fase y ultimo commit
3. solo despues continuar

Si el presupuesto operativo entra en el ultimo `15%`, detener implementacion, actualizar este archivo, hacer commit `docs: update agent handoff` y terminar la sesion.

## Repo oficial

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
```

Sin worktrees externos.
Sin repos paralelos.
Sin implementacion fuera del repo oficial.

## Rama actual

```txt
feature/client-identity-admin-layer-phase2
```

## Stack Admin real

```txt
Angular-tools/Admin/
```

Rutas canonicas:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Angular
Angular-tools/Admin/RollblackLegacy.Admin.Api
Angular-tools/Admin/RollblackLegacy.Admin.Application
Angular-tools/Admin/RollblackLegacy.Admin.Contracts
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure
Angular-tools/Admin/RollblackLegacy.Admin.Domain
```

No usar `src/Admin/`.

## Estado del roadmap

Macro 1:

```txt
Phase 1 DONE
Phase 1.5 DONE
Phase 2 DONE
Phase 3 DONE
Phase 4 DONE
Phase 5 DONE
Phase 6 DONE
Phase 6.5A DONE
Phase 7A DONE
Phase 7B DONE
Phase 7C DONE
Phase 7D DOCS DONE
Phase 8 DONE
```

Macros siguientes:

```txt
Macro 2 - Client Identity Audit Tool: IN_PROGRESS (Phase 1 DONE, Phase 2 DONE)
Macro 3 - Sprite Preview Pipeline: PENDING
Macro 4 - Spells Builder: DEFERRED
Macro 5 - Glyph Builder: DEFERRED
Macro 6 - Maps Builder: DEFERRED
```

## Ultimos commits relevantes

```txt
142ac1e docs: record admin api stabilization gate before client identity phase3
2a7c402 feat: expose client identity audit through admin api
e1d33fd docs: update agent handoff
2a7c402 promueve la tool Phase 1 a capa reusable de Application + Infrastructure + Admin API
```

## Fase exacta actual

Estamos aqui:

```txt
Macro 2 - Client Identity Audit Tool
Stabilization gate before Phase 3
Status: PASSED
```

## Que entrego Phase 2

Closed scope:

```txt
1. contratos read-only bajo RollblackLegacy.Admin.Contracts/ClientIdentity
2. servicio reusable bajo Application/Services/ClientIdentity
3. repository DB read-only contra sunshine.items
4. source reader D2O/D2I read-only con cache
5. endpoints GET /api/admin/v1/client-identity/items/{itemId}
6. endpoint GET /api/admin/v1/client-identity/items/check?ids=...
7. publication-status ya reutiliza la misma auditoria
8. la tool CLI ClientIdentityAudit quedo como wrapper/report writer
```

## Que cerro el stabilization gate

```txt
1. no habia dotnet run vivo de RollblackLegacy.Admin.Api
2. si existia VBCSCompiler.exe reteniendo outputs intermedios
3. dotnet build-server shutdown libero el compiler server
4. dotnet clean/build con /nr:false dejo el build reproducible
5. Admin.Api.csproj volvio a compilar en verde
6. los warnings restantes quedaron clasificados
```

## Archivos nuevos o clave

Backend reusable:

```txt
Angular-tools/Admin/RollblackLegacy.Admin.Api/Controllers/ClientIdentityAdminController.cs
Angular-tools/Admin/RollblackLegacy.Admin.Application/Abstractions/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Application/Models/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Application/Services/ClientIdentity/ClientItemIdentityReadService.cs
Angular-tools/Admin/RollblackLegacy.Admin.Contracts/ClientIdentity/
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Configuration/AdminClientIdentityOptions.cs
Angular-tools/Admin/RollblackLegacy.Admin.Infrastructure/Services/ClientIdentity/
```

CLI:

```txt
infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj
infrastructure/scripts/ClientIdentityAudit/Program.cs
```

Docs:

```txt
docs/admin-tools/client-identity/README.md
docs/admin-tools/client-identity/client-identity-admin-layer-phase2.md
docs/admin-tools/client-identity/client-identity-api-contracts.md
docs/admin-tools/client-identity/client-identity-stabilization-gate-before-phase3.md
docs/admin-tools/client-identity/client-identity-item-check-report.md
docs/roadmap/admin-tools-migration-master-plan.md
docs/roadmap/admin-tools-migration-master-plan.html
```

## Validacion ejecutada

Builds:

```txt
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md" -> OK
dotnet clean "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false -> OK
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false -> OK
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj" /nr:false -> OK
```

Smoke test API ejecutado localmente:

```txt
GET /api/admin/v1/health -> OK
GET /api/admin/v1/client-identity/items/7754 -> OK
GET /api/admin/v1/client-identity/items/12617 -> OK
GET /api/admin/v1/items/7754/publication-status -> OK
```

La API local usada para smoke test ya quedo apagada al cerrar la validacion.

## Casos de control actuales

```txt
7754  -> SAFE_EXISTING_TEMPLATE / CLIENT_KNOWN / ICON_PREVIEW_MISSING
12616 -> CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH / ICON_PREVIEW_FOUND / APPEARANCE_UNKNOWN
12617 -> CLIENT_UNKNOWN / NEEDS_CLIENT_PATCH / ICON_PREVIEW_MISSING
39    -> SAFE_EXISTING_TEMPLATE / CLIENT_KNOWN / ICON_PREVIEW_FOUND
```

Hechos validados:

```txt
7754 existe en Items.d2o
12616 no existe en Items.d2o
12617 no existe en Items.d2o
DescriptionId 50090 resuelve en ES y EN
DescriptionId 50091 resuelve en ES y EN
IconId solo no publica un template cliente
publication-status ya usa la auditoria reusable, no una logica aparte
el lock reproducido del build aislado se resolvio con build-server shutdown + /nr:false
```

## Warnings clasificados

```txt
CS2012 -> CRITICAL / FIX_NOW -> resuelto
CA1416 FirewallManager -> KNOWN_EXTERNAL / DEFER
NETSDK1057 preview SDK -> KNOWN_EXTERNAL / DEFER
```

## Prohibiciones activas

```txt
crear worktrees externos
crear repos paralelos
tocar client files en write mode
modificar d2o/d2i/d2p
auditar armas
recorrer 44k registros
tocar gameplay
escribir produccion sin backup
commitear secretos
copiar bin/obj/node_modules/dist/logs/artifacts
arrancar Macro 3 antes de cerrar Macro 2
```

## Archivos ajenos que no son tuyos

No revertir ni stagear:

```txt
Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/WorldServerManager.cs
```

Tambien dejar intactos los locales no trackeados:

```txt
Client2.3.7/cliente.rar
Client2.3.7/cliente/
Client2.3.7/version
config/Database.local.xml
config/Database.runtime.backup.xml
config/Database.team.xml
```

## Siguiente accion exacta

Si eres el siguiente agente:

```txt
1. Lee este handoff completo.
2. Confirma branch = feature/client-identity-admin-layer-phase2.
3. Confirma ultimo commit = 142ac1e o mas nuevo.
4. Confirma que el stabilization gate ya esta PASS.
5. No abras Macro 3 todavia.
6. Arranca Macro 2 / Phase 3 solamente si el usuario la pide explicitamente.
```

Siguiente paso tecnico recomendado:

```txt
Macro 2 / Phase 3
exponer esta auditoria en Angular como pantalla read-only
sin tocar Client2.3.7 en write mode
sin abrir Sprite Preview Pipeline todavia
```
