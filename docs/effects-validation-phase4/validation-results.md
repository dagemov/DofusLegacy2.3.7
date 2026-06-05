# Fase 4 — Resultados de validación

Completar tras pruebas en **`devp`** (VPS Docker). Estado inicial: **PENDING** en todos los escenarios.

## Entorno de ejecución

| Campo | Valor |
|-------|--------|
| Fecha test | *(completar)* |
| Rama / SHA | `devp` @ *(SHA)* |
| Tester | *(nombre)* |
| Cliente | 2.3.7 → `174.138.35.107:2450` / `:5557` |
| Logs | `/opt/dofus-2.0.0-build/docker/logs/fights/{fightId}.log` |

## Leyenda

| Resultado | Significado |
|-----------|-------------|
| PASS | Comportamiento alineado con Rollback / checklist |
| FAIL | Fallo reproducible; ver columna causa + hilo código |
| PENDING | No probado aún |
| N/A | Fuera de alcance Fase 3 (Ola 2) |

| Rollback parity | `confirmado en diff` \| `confirmado en juego` \| `inferido` |

---

## 1. Venenos / DOT

| ID | Escenario | Capa | Resultado | Evidencia | Rollback parity | Commit fix |
|----|-----------|------|-----------|-----------|-----------------|------------|
| V-01 | Tick inicio turno | HpSteal / TURN_BEGIN | PENDING | `fight=__.log` | confirmado en diff | Fase 3 `e85d26d` |
| V-02 | Robo 50% caster | HpSteal | PENDING | | confirmado en diff | Fase 3 |
| V-03 | Fin turno / duración | TriggerBuff | PENDING | | inferido | — |
| V-04 | Muerte por veneno | InflictDamage / Kill | PENDING | | inferido | — |

### Extracto log (pegar aquí)

```
# Ejemplo:
# [timestamp] fight=1 event=TRIGGER type=TURN_BEGIN ...
```

---

## 2. Invocaciones

| ID | Escenario | Capa | Resultado | Evidencia | Rollback parity | Commit fix |
|----|-----------|------|-----------|-----------|-----------------|------------|
| I-01 | Sacrificada | Sacrifice + Summon | PENDING | | inferido | — |
| I-02 | Hinchable / bomba | Summon + Bomb | PENDING | | inferido | — |
| I-03 | Árbol estático | SummonedStaticMonster | PENDING | | confirmado en diff | Fase 3 `8b32ee9` |
| I-04 | Doble | Double | PENDING | | inferido | — |
| I-05 | Suicida fin turno | DiesAtTurnEnd | PENDING | | confirmado en diff | Fase 3 |
| I-06 | Golosón (caso) | Misma capa I-05 | PENDING | | inferido | — |

---

## 3. Castigos Sacrógrito

| ID | Escenario | Capa | Resultado | Evidencia | Rollback parity | Commit fix |
|----|-----------|------|-----------|-----------|-----------------|------------|
| P-01 | Castigo Osado | PunishmentBuff | PENDING | | confirmado en diff | Fase 3 `d7529d6` |
| P-02 | Castigo Ágil | PunishmentBuff | PENDING | | confirmado en diff | Fase 3 |
| P-03 | Castigo Forzado | PunishmentBuff | PENDING | | confirmado en diff | Fase 3 |
| P-04 | Castigo Espiritual | PunishmentBuff | PENDING | | confirmado en diff | Fase 3 |
| P-05 | Tope por ronda | PerRoundCap | PENDING | | confirmado en diff | Fase 3 |
| P-06 | Daño % castigo | PunishmentDamage | PENDING | | inferido | — |

---

## 4. Bosses (Ola 2)

| ID | Escenario | Capa | Resultado | Evidencia | Rollback parity | Commit fix |
|----|-----------|------|-----------|-----------|-----------------|------------|
| B-01 | Invocaciones requeridas | FrigostBossMechanics | PENDING | | inferido | Ola 2 |
| B-02 | Vulnerabilidad | StateBuff | PENDING | | inferido | — |
| B-03 | IA fase 2 | MonsterAttackAI | PENDING | | inferido | Ola 2 |

---

## 5. Mapas / casillas

| ID | Escenario | Capa | Resultado | Evidencia | Rollback parity | Commit fix |
|----|-----------|------|-----------|-----------|-----------------|------------|
| M-01 | Casilla muerte | Effect_Kill | PENDING | | confirmado en diff | Fase 3 `b0a7b5f` |
| M-02 | Glifo al pisar | Glyph / MOVE | PENDING | | inferido | — |
| M-03 | Trampa kill | Trap | PENDING | | inferido | — |
| M-04 | Glifo veneno turno | Glyph TURN_* | PENDING | | inferido | — |

---

## 6. Empujes (Ola 2 parcial)

| ID | Escenario | Capa | Resultado | Evidencia | Rollback parity | Commit fix |
|----|-----------|------|-----------|-----------|-----------------|------------|
| E-01 | Colisión | Push | PENDING | | inferido | — |
| E-02 | Daño empuje | Push + stats | PENDING | | inferido | — |
| E-03 | Kill por colisión | Push + secuencias | PENDING | | inferido | Ola 2 secuencias |
| E-04 | Glifo mid-empuje | Push + marks | PENDING | | inferido | Ola 2 |

---

## Resumen ejecutivo

| Métrica | Valor |
|---------|--------|
| PASS | 0 |
| FAIL | 0 |
| PENDING | 28 |
| Compila (`devp-compile`) | **OK** @ `f6c79fc` |

## Fallos → acción

Si un escenario marca **FAIL**:

1. Identificar **capa** (no hechizo) en [test-scenarios.md](./test-scenarios.md).
2. Documentar causa en esta tabla (columna Evidencia + nota).
3. Si requiere código: commit por capa en rama nueva (no mezclar hechizos).
4. Re-test en `devp` (VPS) y actualizar fila a PASS.

## Enlaces

- Escenarios: [test-scenarios.md](./test-scenarios.md)
- Causa raíz Fase 3: [root-cause-analysis.md](../effects-engine-fix-phase3/root-cause-analysis.md)
- Filosofía: [combat-fix-philosophy.md](../combat-fix-philosophy.md)
