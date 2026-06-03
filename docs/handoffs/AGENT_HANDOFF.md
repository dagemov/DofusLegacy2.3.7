# Agent Handoff - Sprite Preview Pipeline (Macro 3)

Generated: `2026-06-03`

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-appearance-audit-phase5
```

## Estado Macro 3

```txt
Phase 1–4 DONE / PARTIAL
Phase 5 DONE — appearance identity audit + feasibility (documental)
Phase 6 NEXT — curated equipment preview (by-appearance/)
Phase 7 PLANNED — EntityLook renderer research (solo con aprobación)
```

## Ultimo trabajo (Phase 5)

Commit esperado:

```txt
docs: audit appearance identity and preview feasibility
```

Entregables:

```txt
docs/admin-tools/sprite-preview/appearance-identity-audit-phase5.md
docs/admin-tools/sprite-preview/appearance-preview-feasibility-study.md
docs/admin-tools/sprite-preview/entitylook-relationship-map.md
README + roadmap + este handoff
```

Evidencia probe (temporal, no commitear):

```txt
Infrastructure/temporal-artifacts/appearance-d2o-probe/ReflectProbe.csproj
```

Hallazgos:

```txt
AppearanceId → skin en EntityLook (Character.Look.AddSkin)
Appearances.d2o Client2.3.7: 130 índices (654–868); 0/458/1004 ausentes
Item 12616 AppearanceId 1004 → APPEARANCE_UNKNOWN
Angular: solo IconId auto; equipamiento = by-appearance curado
Tiphon: no en repo; no requerido para Admin
```

## Phase 6 (siguiente agente)

Objetivo: workflow curado `by-appearance/{appearanceId}.png` (espejo Phase 3–4 de iconos).

No hacer: renderer EntityLook, extracción masiva sprites, Tiphon.

## QA pendiente (operador)

```txt
/admin/items/7754
/admin/items/7754/publication-status
/admin/items/icon-selector?iconId=23012
```

## Prohibiciones

```txt
no import masivo
no commitear temporal-artifacts
no modificar Client2.3.7
```

Docs Phase 5: `docs/admin-tools/sprite-preview/appearance-identity-audit-phase5.md`
