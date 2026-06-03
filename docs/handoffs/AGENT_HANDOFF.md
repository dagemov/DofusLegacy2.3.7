# Agent Handoff - Admin Tools Migration

Generated: `2026-06-03`

## Repo y rama

```txt
C:\Users\Hombr\source\repos\DofusLegacy2.3.7
feature/item-sprite-preview-final-qa-phase7
```

## Macro 3 — Sprite Preview: COMPLETE

```txt
Phases 1–7 DONE
EntityLook renderer DEFERRED (not required for Items Builder MVP)
```

Últimos commits Macro 3:

```txt
02ad65a feat: add curated appearance preview diagnostics
022e992 docs: audit appearance identity and preview feasibility
(+ commits phases 1–4 en ramas anteriores)
```

Phase 7 entrega:

```txt
docs/admin-tools/sprite-preview/sprite-preview-final-qa-phase7.md
UX labels ES (Icon / Appearance / Client Identity / Publication Status)
API smoke PASS (7754, 12616, 12617, 39)
```

## Cuatro superficies (operador)

```txt
Icon Preview        → inventario (IconId / by-icon)
Appearance Preview  → equipamiento (AppearanceId / by-appearance)
Client Identity     → ItemId en Items.d2o
Publication Status  → visible / patch / assets
```

## Browser QA pendiente (operador)

```txt
/admin/items/7754
/admin/items/7754/publication-status
/admin/items/12616
/admin/items/12616/edit
/admin/items/12617/publication-status
/admin/items/icon-selector
```

API smoke ya PASS en sesión 2026-06-03.

## Siguiente macro

```txt
NO iniciar sin aprobación explícita.
Candidato documentado: Macro 4 Spells Builder (DEFERRED en roadmap).
```

## Builds

```txt
dotnet build Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj — OK
npm run build — OK
```

Docs: `docs/admin-tools/sprite-preview/sprite-preview-final-qa-phase7.md`
