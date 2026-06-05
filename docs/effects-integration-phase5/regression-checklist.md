# Fase 5 — Checklist de regresión

Ejecutar en entorno **VPS test** (`/opt/dofus-2.0.0-build`) con checkout de **`devp`**. Mapea escenarios de [test-scenarios.md](../effects-validation-phase4/test-scenarios.md).

## Entorno

| Campo | Valor |
|-------|--------|
| Rama código | `devp` @ *(SHA post migración)* |
| Path VPS | `/opt/dofus-2.0.0-build` |
| Puertos | 2450 / 5557 |
| Cliente | 2.3.7 → `174.138.35.107:2450` / `:5557` |
| Logger | `FIGHT_COMBAT_LOG_ENABLED=true` |
| Logs | `docker/logs/fights/{fightId}.log` |

---

## A. Compilación

- [ ] `devp-compile` merge Fase 3 → `docker compose build sunshine` **OK**
- [ ] Sin `.cs` nuevos sin `<Compile Include>` en `Sunshine.csproj` (lección Fase 3)
- [ ] Registro en `docs/vps-build-validation/`

---

## B. Arranque

- [ ] Contenedor `sunshine-server` **Up**
- [ ] Log: EffectsLoader ~**162** efectos
- [ ] Auth **2450** accesible
- [ ] World **5557** accesible
- [ ] Login cliente OK

---

## C. Combate PvM (smoke — capas Fase 3)

| Check | ID Fase 4 | Capa | Resultado | Evidencia |
|-------|-----------|------|-----------|-----------|
| [ ] DOT tick inicio turno | V-01, V-02 | `HpSteal` + `TURN_BEGIN` | | |
| [ ] Invocación suicida fin turno | I-05 | `DiesAtTurnEnd` | | |
| [ ] Glifo kill al pisar celda | M-01 | `Effect_Kill` | | |
| [ ] Castigo reactivo + tope ronda | P-01, P-05 | `PunishmentBuff` | | |

**Logs esperados (ejemplo):**

```
event=TRIGGER type=TURN_BEGIN effect=Effect_StealHP*
event=DAMAGE ...
event=KILL
event=SUMMON_DIE
```

Actualizar filas en [validation-results.md](../effects-validation-phase4/validation-results.md).

---

## D. Combate PvP

Repetir mínimo en pelea PvP (misma capa, sin fix por clase/hechizo):

| Check | ID Fase 4 | Resultado | Evidencia |
|-------|-----------|-----------|-----------|
| [ ] DOT tick PvP | V-01 | | |
| [ ] Castigo reactivo PvP | P-01 | | |

---

## E. Dungeons críticas (Frigost — smoke)

Bosses en `FrigostBossMechanics.cs`:

| Check | Boss | MonsterId | ID Fase 4 | Resultado | Notas |
|-------|------|-----------|-----------|-----------|-------|
| [ ] Invulnerabilidad / ventana | Royalmouth | 2854 | B-02 | | |
| [ ] Summon forzado Hamrack | Ben Le Ripate | 2877 | B-01 | | Ola 2 si FAIL |
| [ ] Estados timed | Obsidiantre | 2924 | B-02 | | |
| [ ] Fase invulnerabilidad | Kolosso | 2986 | B-02 | | |

**Criterio:** PASS en capas Fase 3 (summon/kill/DOT) aplicables. FAIL en B-01/B-03 → etiqueta **Ola 2**; no bloquea merge en `devp` si A–D pasan.

---

## F. Cierre integración

- [ ] `validation-results.md` actualizado (PASS/FAIL/PENDING por ID)
- [ ] Registro VPS: `docs/vps-build-validation/YYYYMMDD-devp-compile-phase3-{sha}.md`
- [ ] `BRANCHING.md` refleja: solo PRs a **`devp`**
- [ ] `origin/develop` eliminada
- [ ] PRs pipeline #26–#30: todas `base=devp`

---

## Orden de ejecución recomendado

1. Compile gate local (`devp-compile`)
2. VPS: backup → stop prod si necesario → `git checkout devp` → build/up
3. Smoke B (arranque)
4. Smoke C + D (PvM/PvP) — equipo in-game
5. Smoke E (dungeons) — equipo in-game
6. Actualizar `validation-results.md` y registro VPS
