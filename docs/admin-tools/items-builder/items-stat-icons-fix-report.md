# Items Builder — Stat icons fix report

**Fecha:** 2026-06-05  
**Rama:** `feature/items-preview-sets-polish-final`

## Síntoma

Los PNG en `src/assets/icons/` no se mostraban en el editor de stats (`/admin/items/new`, `/admin/items/:id/edit`). El usuario veía emoji o imagen rota.

## Causa raíz

`angular.json` solo publicaba la carpeta `public/`. Los assets bajo `src/assets/` (incluido `icons/` y `item-previews/`) **no se copiaban** al build de desarrollo/producción, por lo que las rutas `/assets/icons/*.png` devolvían 404.

## Corrección

En `angular.json`, sección `architect.build.options.assets`:

```json
{ "glob": "**/*", "input": "src/assets", "output": "/assets" },
{ "glob": "**/*", "input": "src/assets/manual-assets", "output": "/manual-assets" }
```

## Archivos de iconos (nombres reales en repo)

| Archivo | Uso en quick-picks |
| --- | --- |
| `fire.png` | Inteligencia |
| `widsom.png` | Sabiduría (typo histórico en disco) |
| `force.png` | Fuerza |
| `water.png` | Suerte |
| `Air.png` | Agilidad / daño aire |
| `hp.png` | Vitalidad |
| `neutral.png` | Daños |
| `Range.png` | Alcance |
| `PM.png` / `PA.png` | PM / PA |
| `Prospeccion.png` | Prospección |
| `iniciative.png` | Golpes críticos |
| `summon.png` | Invocaciones |

No se renombraron archivos en esta fase para evitar roturas en referencias externas; el mapping en `item-effect-stat-quick-picks.ts` apunta a los nombres exactos del disco.

## Rutas en código

- `resolveStatIconAssetPath()` normaliza a `/assets/icons/...`
- Plantillas usan `(error)` en `<img>` para volver al emoji del quick-pick sin imagen rota

## Validación

| Check | Resultado |
| --- | --- |
| `npm run build` | OK |
| Fallback emoji | Conservado en `item-effects-editor` |

## Referencias

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-effect-stat-quick-picks.ts`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/app/admin/items/item-effects-editor.component.html`
