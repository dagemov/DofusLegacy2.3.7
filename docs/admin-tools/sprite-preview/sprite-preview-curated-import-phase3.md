# Macro 3 / Phase 3 — Curated Icon Import + Angular Preview Integration

Estado: `DONE`

## Objetivo

Promover `IconId 23012` (Dofus Ocre, item `7754`) al catálogo curado Angular para eliminar el placeholder de preview en Admin.

## Import ejecutado

```bash
dotnet run --project "Infrastructure/scripts/ItemSpritePreviewPipeline/ItemSpritePreviewPipeline.csproj" -- \
  --mode extract-icon --icon-id 23012 \
  --output "Infrastructure/temporal-artifacts/item-sprite-preview-audit/extracted" \
  --approve-curated-copy
```

| Paso | Resultado |
| --- | --- |
| Extracción D2P | `bitmap0.d2p` → `23012.png` (3881 bytes, PNG válido) |
| Catálogo curado | `Angular-tools/Admin/.../item-previews/by-icon/23012.png` |
| Artefactos temporales | Gitignored (`Infrastructure/temporal-artifacts/`) |

## Validación técnica

| Check | Estado |
| --- | --- |
| `by-icon/23012.png` trackeado en git | OK |
| `temporal-artifacts/` ignorado | OK (`.gitignore`) |
| Pipeline audit item 7754 | `IconPreviewAvailable: yes`, `BY_ICON` |
| `dotnet build` ItemSpritePreviewPipeline | OK |
| `dotnet build` Sunshine.sln | OK |
| `npm run build` Admin Angular | OK |

## Validación API / navegador (operador)

Con Admin API + Angular en ejecución:

```txt
GET /api/admin/v1/items/7754
GET /api/admin/v1/client-identity/items/7754
GET /api/admin/v1/items/7754/publication-status
```

Esperado:

```txt
previewState: FOUND / BY_ICON → /assets/item-previews/by-icon/23012.png
client identity: SAFE_EXISTING_TEMPLATE / CLIENT_KNOWN / ICON_PREVIEW_FOUND
publication: PUBLISHED (sin cambio de workflow)
```

Rutas UI:

```txt
/admin/items/7754
/admin/items/7754/publication-status
```

## Alcance respetado

- Un solo PNG curado commiteado (`23012.png`).
- Sin extracción masiva.
- Sin modificar `Client2.3.7` ni D2P/D2O/D2I.
- Sin writes DB.

## Workflow formalizado (Phase 4)

Ver [sprite-preview-curated-workflow-phase4.md](./sprite-preview-curated-workflow-phase4.md): `--dry-run-curated-copy`, `--approve-curated-copy`, `--overwrite-curated`.

## Rama

```txt
feature/item-sprite-preview-curated-import-phase3
```

Commit:

```txt
feat: import curated dofus ocre icon preview
```
