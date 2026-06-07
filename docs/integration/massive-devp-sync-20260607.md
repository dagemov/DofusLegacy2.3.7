# Massive devp integration sync — 2026-06-07

**Repo:** `DofusLegacy2.3.7`  
**Base:** `devp` (includes merged [PR #32](https://github.com/dagemov/DofusLegacy2.3.7/pull/32) — parche compañero .NET 11)  
**`main`:** intact — no merge a `main` en esta pasada

---

## Paso 1 — Estado local al inicio

- Rama activa: `feature/spell-builder-api-migration`
- Cambios locales **no commiteados** (items, Client2.3.7, OneLauncher, Sunshine inventory, etc.) → **`git stash`** (`gate-integration: unrelated local wip`)
- Prohibido commitear: `Client2.3.7/`, `OneLauncher/`, `backups/`, `temporal-artifacts/`, logs pesados

---

## Paso 2 — Ramas documentadas

| Área | Rama | vs `origin/devp` | Remote antes | Acción |
| --- | --- | ---: | --- | --- |
| Items/Sets polish | `feature/items-preview-sets-polish-final` | 0 | ya remoto | **Ya en devp** (PR #15 merged) |
| Sets CRUD | `feature/sets-builder-crud-and-pagination` | 5 | no | push + PR #34 |
| Sets acceptance | `feature/items-sets-production-acceptance-test` | 8 | no | push + PR #35 |
| Client pub phase6 | `feature/client-publication-controlled-publish-phase6` | 0 | — | **Ya en devp** |
| Client pub 6b–6d | `feature/item-skin-catalog-by-category-phase6b` etc. | 0 | — | **Ya en devp** |
| Sprint visibility + telemetry VPS | `feature/items-sets-visibility-and-vps-combat-telemetry` | 18 | no | push + PR #36 |
| Combat Phase 1 | `feature/combat-sanitization-phase1-audit` | 2 | no | push + PR #37 |
| Combat Phase 2 | `feature/combat-telemetry-phase2` | 7 | no | push + PR #38 |
| Combat Phase 3 | `feature/combat-readychecker-phase3` | 24 | no | push + PR #39 (+ cherry-pick `f6f8e09` analyzer) |
| Spell Builder | `feature/spell-builder-api-migration` | 12 | no | push + PR #40 |

---

## Paso 3 — Builds (`devp` limpio, post-stash)

| Target | Resultado |
| --- | --- |
| `dotnet build Sunshine.csproj` | **OK** (4 CA1416 warnings) |
| `dotnet build RollblackLegacy.Admin.Api.csproj` | **OK** (tras `Stop-Process RollblackLegacy.Admin.Api`) |
| `npm run build` (Angular) | **OK** (budget +1.13 kB; exit code anómalo en PS, bundle generado) |
| `dotnet build Sunshine.csproj` en `feature/combat-readychecker-phase3` | **OK** |

---

## Paso 4 — Push

Todas pusheadas con `-u origin`:

```txt
feature/sets-builder-crud-and-pagination
feature/items-sets-production-acceptance-test
feature/items-sets-visibility-and-vps-combat-telemetry
feature/combat-sanitization-phase1-audit
feature/combat-telemetry-phase2
feature/combat-readychecker-phase3
feature/spell-builder-api-migration
```

---

## Paso 5 — PRs hacia `devp`

| PR | Rama | Mergeable (GitHub) | Notas |
| ---: | --- | --- | --- |
| [#34](https://github.com/dagemov/DofusLegacy2.3.7/pull/34) | `feature/sets-builder-crud-and-pagination` | **MERGEABLE** | Merge primero en cadena Items/Sets |
| [#35](https://github.com/dagemov/DofusLegacy2.3.7/pull/35) | `feature/items-sets-production-acceptance-test` | **MERGEABLE** | Tras #34 recomendado |
| [#36](https://github.com/dagemov/DofusLegacy2.3.7/pull/36) | `feature/items-sets-visibility-and-vps-combat-telemetry` | **CONFLICTING** | Solapamiento Admin/items + scripts; merge tras #34/#35 |
| [#37](https://github.com/dagemov/DofusLegacy2.3.7/pull/37) | `feature/combat-sanitization-phase1-audit` | **MERGEABLE** | Cadena combat — merge primero |
| [#38](https://github.com/dagemov/DofusLegacy2.3.7/pull/38) | `feature/combat-telemetry-phase2` | **CONFLICTING** | Merge tras #37 |
| [#39](https://github.com/dagemov/DofusLegacy2.3.7/pull/39) | `feature/combat-readychecker-phase3` | **CONFLICTING** | Ver conflictos PR #32 abajo |
| [#40](https://github.com/dagemov/DofusLegacy2.3.7/pull/40) | `feature/spell-builder-api-migration` | **CONFLICTING** | Paralelo a combat; resolver tras Items si aplica |

**Orden de merge recomendado:** #34 → #35 → #36 → #37 → #38 → #39 (combat) y #40 en paralelo cuando Admin esté estable.

---

## Paso 6 — Conflictos documentados

### PR #39 / combat vs [PR #32](https://github.com/dagemov/DofusLegacy2.3.7/pull/32) (ya en `devp`)

Archivos **changed in both** (merge-tree local):

```txt
Sunshine.WorldServer/Game/Actors/AI/MonsterAttackAI.cs
Sunshine.WorldServer/Game/Actors/Fighters/CharacterFighter.cs
Sunshine.WorldServer/Game/Actors/Fighters/FightActor.cs
Sunshine.WorldServer/Game/Effects/EffectDispatcher.cs
Sunshine.WorldServer/Game/Fights/Fight.cs
Sunshine.WorldServer/Handlers/Context/ContextHandler.cs
Sunshine.csproj
```

**Política:** conservar IA/rates/DOT/Sacrifice/summon del parche #32; integrar ReadyChecker y telemetría Phase 3 encima. **No** `--theirs`/`--ours` masivo. Si el comportamiento de turno diverge, validar con operador antes de merge.

### PR #36, #38, #40

- Conflictos GitHub `DIRTY` — resolver tras merges previos de la misma cadena o rebase sobre `devp` actualizado.

---

## Paso 7 — Qué NO se mergeó

```txt
main — sin cambios
Client2.3.7/ — no commiteado
OneLauncher/ — no commiteado
temporal-artifacts/ — no commiteado
stash local WIP — no commiteado
```

---

## Pendientes operador

```txt
Items Builder: COMPLETE — pending operator publish only
Sets Builder: COMPLETE — merge PRs #34/#35
Client Publication: COMPLETE — operator controlled publish
Combat Telemetry: ACTIVE (scripts en PR #36+)
ReadyChecker: PASS functional — residual timers classified (Phase 3.1 en PR #39)
Merge PR #39 conflictos con PR #32 — revisión manual combat/IA
Next: Combat Phase 4 Spell/Summon telemetry analysis
```

---

## Commits principales por rama (top)

| Rama | Commits destacados |
| --- | --- |
| sets CRUD | `b9c1cce` sets CRUD API, `da758a3` editor UI |
| sets acceptance | `89a7a76` item creation effects, `32df513` set publication validation |
| visibility sprint | `fc038c3` VPS telemetry scripts, `a1b6c3e` auth port fix |
| combat P1 | `29ba723` audit, `b491e6d` lab scripts |
| combat P2 | `b59a97c` event logging, `232447b` analyzer |
| combat P3 | `ff832db` ReadyChecker, `6ba6640` analyzer Phase 3.1 |
| spell builder | `9031339` spell detail API … `6816c2b` items angular audit |

---

## Paso 9 — Post-merge `devp` (pendiente)

Ejecutar tras mergear PRs:

```powershell
git checkout devp
git pull origin devp
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.csproj" /nr:false
dotnet build "Angular-tools/Admin/RollblackLegacy.Admin.Api/RollblackLegacy.Admin.Api.csproj" /nr:false
cd Angular-tools/Admin/RollblackLegacy.Admin.Angular; npm run build
```

---

## Stash local

```powershell
git stash list
# gate-integration: unrelated local wip
# Recuperar solo si el operador confirma: git stash pop
```
