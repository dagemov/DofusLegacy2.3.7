# Plantilla de entrega por fase

Contrato para documentar cada fase del motor de efectos / combate (y features relacionadas). Basado en el patrón validado en [effects-audit-phase1](./effects-audit-phase1/).

## Estructura de carpeta

```
docs/
  PHASE-DELIVERY-TEMPLATE.md     ← este archivo
  BRANCHING.md
  combat-fix-philosophy.md
  effects-audit-phase1/          ← Fase 1 (referencia)
  effects-catalog-phase2/        ← Fase 2
  vps-build-validation/          ← registros develop-build @ VPS
```

## Archivos por fase (mínimo)

| Rol | Nombre sugerido | Contenido |
|-----|-----------------|-----------|
| **Hub** | `README.md` | Metadatos, índice, ramas, rutas, resumen, riesgos, alcance |
| **Arquitectura** | `*-overview.md` o `execution-pipeline.md` | Flujos, mermaid, subsistemas |
| **Técnico / diff** | `*-diff.md` o `effect-id-mapping.md` | Mapeo clases/IDs, gaps, bloques `auditoria:` |
| **Accionable** | `affected-systems.md` o `effect-categories.md` | Matriz o taxonomía con rutas **(game)** / **(multi)** |

## Secciones obligatorias del README de fase

```markdown
# Fase N: [Título]

## Metadatos
- Duración estimada: X h
- Rama: feature/nombre-phaseN
- Documentación: docs/nombre-phaseN/

## Índice de documentos
(tabla archivo → contenido)

## Modelo de ramas
(enlace a BRANCHING.md)

## Rutas base
(game Sunshine, Rollback, multi cliente)

## Resumen ejecutivo
(métricas o hallazgos clave)

## Prioridades / riesgos siguiente fase

## Convención módulos
- game — servidor autoritativo
- multi — cliente AS2 / launcher (referencia)

## Alcance explícito
(qué está permitido y prohibido en esta fase)

## Validación VPS (si aplica)
(enlace a docs/vps-build-validation/…)
```

## Bloque estándar `auditoria:`

Usar en documentos técnicos cuando se cite código:

```text
auditoria:
ruta/rollback/<archivo relativo Rollback.World>
ruta/actual/<archivo relativo Sunshine.WorldServer>
LINEAS: <inicio>-<fin>
Módulo: game | multi
```

## Evidencia

Marcar cada hallazgo como:

- **confirmado en diff** — contrastado en código
- **inferido** — hipótesis sin prueba en juego en esta fase

## PR al validador (plantilla corta)

1. **Qué es** — duración, rama, carpeta docs
2. **Archivos a revisar** — checklist
3. **Cobertura** — síntomas o categorías
4. **Preguntas de merge** — 3 ítems concretos
5. **Alcance** — sin `.cs` de combate (fase doc) o build VPS OK (fase código)

## Flujo Git

1. Trabajo en `feature/*-phaseN`
2. Sandbox local `develop-compile` acumula merges
3. `develop-build` → test compile/runtime en VPS ([vps-deploy.md](./vps-deploy.md))
4. PR `feature/*` → `develop`
5. Release: `develop` → `main`
