# Agent Handoff - Client Identity Audit Tool

Generated: `2026-06-03`

Leer este archivo antes de cualquier implementación.

## Regla obligatoria

No continuar implementación si este handoff no existe o está desactualizado.

El siguiente agente debe:

1. leer este handoff completo
2. confirmar repo, branch, fase y último commit
3. solo después continuar

Si el presupuesto operativo entra en el último `15%`, detener implementación, actualizar este archivo, hacer commit `docs: update agent handoff` y terminar la sesión.

## Repo oficial

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
```

Sin worktrees externos.
Sin repos paralelos.
Sin implementación fuera del repo oficial.

## Rama actual

```txt
feature/client-identity-admin-layer-phase2
```

## Stack Admin real

```txt
Angular-tools/Admin/
```

Rutas canónicas:

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

## Últimos commits relevantes

```txt
2a7c402 feat: expose client identity audit through admin api
2a7c402 promueve la tool Phase 1 a capa reusable de Application + Infrastructure + Admin API
c606976 feat: add client identity audit runtime scaffold
d122434 feat: add client identity audit tool scaffold
944ff0c docs: update agent handoff
```

## Fase exacta actual

Estamos aquí:

```txt
Macro 2 - Client Identity Audit Tool
Phase 2 - Admin API reusable read-only layer
Status: CLOSED
```

## Qué entregó Phase 2

Closed scope:

```txt
1. contratos read-only bajo RollblackLegacy.Admin.Contracts/ClientIdentity
2. servicio reusable bajo Application/Services/ClientIdentity
3. repository DB read-only contra sunshine.items
4. source reader D2O/D2I read-only con cache
5. endpoints GET /api/admin/v1/client-identity/items/{itemId}
6. endpoint GET /api/admin/v1/client-identity/items/check?ids=...
7. publication-status ya reutiliza la misma auditoría
8. la tool CLI ClientIdentityAudit quedó como wrapper/report writer
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
docs/admin-tools/client-identity/client-identity-item-check-report.md
docs/roadmap/admin-tools-migration-master-plan.md
docs/roadmap/admin-tools-migration-master-plan.html
```

## Validación ejecutada

Builds:

```txt
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" -> OK
dotnet run --project "Infrastructure/scripts/ClientIdentityAudit/ClientIdentityAudit.csproj" -- --items 7754,12616,12617,39 --output "docs/admin-tools/client-identity/client-identity-item-check-report.md" -> OK
```

Smoke test API ejecutado localmente:

```txt
GET /api/admin/v1/health -> OK
GET /api/admin/v1/client-identity/items/7754 -> OK
GET /api/admin/v1/client-identity/items/12617 -> OK
GET /api/admin/v1/client-identity/items/check?ids=7754,12616,12617,39 -> OK
GET /api/admin/v1/items/7754/publication-status -> OK
GET /api/admin/v1/items/12617/publication-status -> OK
```

La API local usada para smoke test ya quedó apagada al cerrar la validación.

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
publication-status ya usa la auditoría reusable, no una lógica aparte
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
escribir producción sin backup
commitear secretos
copiar bin/obj/node_modules/dist/logs/artifacts
arrancar Macro 3 antes de cerrar Macro 2
```

## Archivos ajenos que no son tuyos

No revertir ni stagear:

```txt
Sunshine net11.0/Sunshine net11.0/Sunshine.MySql/Database/Managers/WorldServerManager.cs
```

También dejar intactos los locales no trackeados:

```txt
Client2.3.7/cliente.rar
Client2.3.7/cliente/
Client2.3.7/version
config/Database.local.xml
config/Database.runtime.backup.xml
config/Database.team.xml
```

## Siguiente acción exacta

Si eres el siguiente agente:

```txt
1. Lee este handoff completo.
2. Confirma branch = feature/client-identity-admin-layer-phase2.
3. Confirma último commit = 2a7c402 o más nuevo.
4. No abras Macro 3 todavía.
5. Arranca Macro 2 / Phase 3 solamente si el usuario la pide explícitamente.
```

Siguiente paso técnico recomendado:

```txt
Macro 2 / Phase 3
exponer esta auditoría en Angular como pantalla read-only
sin tocar Client2.3.7 en write mode
sin abrir Sprite Preview Pipeline todavía
```
