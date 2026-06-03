# Item Sprite Preview — Phase 1 Report

Estado: `DONE` — identidad y rutas curadas. Extracción D2P: ver [Phase 2](./sprite-preview-d2p-extractor-phase2.md) (`IconId 23012` extraíble).

Última generación: `2026-06-03 21:56:10 UTC`

Artefacto temporal: `Infrastructure/temporal-artifacts/item-sprite-preview-audit/audit-report.md`

## Tabla de casos

| Caso | ItemId | IconId | AppearanceId | ClientKnown | IconPreviewAvailable | AppearancePreviewAvailable | SourceFile | RecommendedNextStep |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
|Dofus Ocre | 7754 | 23012 | — | yes | no | no | Client D2P packs (bitmap0.d2p, bitmap1.d2p); index lookup pending Phase 2 | Phase 2: extraer IconId 23012 desde D2P o copiar manualmente a by-icon/23012.png (máx. 1–3 assets por fase).
|Petite Amulette du Hibou | 39 | 1001 | — | yes | yes | no | C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Angular-tools\Admin\RollblackLegacy.Admin.Angular\src\assets\item-previews\by-icon\1001.png | Mantener catálogo curado; validar runtime en Angular.
|Dofus Tester | 12617 | 23012 | — | no | no | no | Client D2P packs (bitmap0.d2p, bitmap1.d2p); index lookup pending Phase 2 | Publicar el template 12617 en Items.d2o y alinear i18n antes de declararlo visible.


## Aparición 458 (control)

- Hipótesis: `Sombrero Jalato (no verificada en sunshine.items)`
- Exists in Appearances.d2o (from identity layer): ``
- Curated PNG: `(missing)`
- Notas: Phase 1 no indexa Appearances.d2o por id; ver items-client-appearance-mapping-audit.md. No afirmar mapping sin item de prueba en DB.

Ver informe completo en temporal-artifacts y [sprite-preview-pipeline-phase1.md](./sprite-preview-pipeline-phase1.md).
