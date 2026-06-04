# Appearance Preview — Feasibility Study (Macro 3 / Phase 5)

## Objetivo

Decidir qué puede hacer Angular Admin con `AppearanceId` sin renderizador 3D, sin Tiphon y sin extracción masiva del cliente.

## Matriz de superficies

| Superficie | Identidad | Fuente cliente | Pipeline Admin hoy | Preview automático |
| --- | --- | --- | --- | --- |
| Inventario | `IconId` | `bitmap*.d2p` | D2P extract + `by-icon/` | **Sí** (Phase 2–4) |
| Equipamiento | `AppearanceId` | `Appearances.d2o` + sprites | Solo validación índice | **No** |
| Personaje completo | `EntityLook` string / struct | Breed + skins + colors | Parse en servidor | **No** |
| Sets / mascotas | `AppearanceId` + subentities | Mixto | Parcial (warnings) | **No** |

## Opciones evaluadas

### A — PNG curado `by-appearance/{appearanceId}.png` (recomendada Phase 6)

**Descripción:** Misma filosofía que `by-icon/`: copia manual o captura puntual aprobada.

| Criterio | Valor |
| --- | --- |
| Esfuerzo | Bajo–medio |
| Fidelidad visual | Alta si la captura es correcta |
| Riesgo legal/ops | Bajo (sin extracción masiva) |
| Dependencias | Operador + opcional cliente lanzado para captura |

**UX Admin:** mostrar imagen si existe; si no, placeholder + link a `publication-status` / `APPEARANCE_UNKNOWN`.

### B — Resolver campos desde `Appearances.d2o` + `Items.d2o`

**Descripción:** Endpoint o modo audit que liste `id`, `type`, ítems de ejemplo.

| Criterio | Valor |
| --- | --- |
| Esfuerzo | Bajo (extensión de client identity) |
| Valor UX | Medio — diagnóstico, no imagen |
| Bloqueo | Pack recortado; muchos ids DB no están en D2O |

### C — Extracción gfx sprites (D2P personaje)

**Descripción:** Localizar skin en packs bajo `content/gfx/sprites/`.

| Criterio | Valor |
| --- | --- |
| Esfuerzo | Alto |
| Viabilidad Macro 3 | **Rechazada** (alcance Phase 5 prohibe extracción masiva y viewer) |
| Nota | Distinto al pipeline `bitmap*.d2p` de ítems |

### D — Renderer EntityLook en Angular (Canvas/WebGL/SWF)

**Descripción:** Reproducir bone + skins + colores.

| Criterio | Valor |
| --- | --- |
| Esfuerzo | Muy alto |
| Viabilidad | **No** en Macro 3 |
| Dependencias | Tiphon-equivalente, atlas, animaciones, breed tables |

### E — Reutilizar heurísticas legacy (`ItemAppearanceResolverService`)

**Descripción:** Inferir `AppearanceId` desde icono/hash PNG.

| Criterio | Valor |
| --- | --- |
| Uso | Solo referencia en `legacy-reference/` |
| Riesgo | Falsos positivos; no sustituye preview visual |
| Decisión | No portar como “preview”; como herramienta de sugerencia futura opcional |

## Respuestas directas (producto)

### ¿`AppearanceId` basta para preview?

**No.** Es un skin id. Un preview de equipamiento necesita al menos:

- contexto de personaje (breed, sex, colores), o
- una imagen ya compuesta (curada).

### ¿Angular puede renderizar algo útil?

**Sí, con alcance acotado:**

- Mostrar PNG curado `by-appearance/`
- Mostrar estado `AppearanceKnown` / `APPEARANCE_UNKNOWN`
- Mantener preview de inventario por `IconId` separado
- Mensajes explícitos: “equipped look ≠ inventory icon”

**No** sin assets curados o renderer EntityLook.

### ¿Se requiere pipeline Tiphon?

**No para Admin.** Tiphon (motor de composición del cliente Ankama) no aparece en el repo oficial. El cliente compone en runtime; Admin no debe replicarlo en Phase 6.

### ¿Preview de equipamiento viable?

| Enfoque | Viabilidad |
| --- | --- |
| Curado + validación D2O | **Viable** — Phase 6 |
| Auto desde `AppearanceId` solo | **No viable** |
| Auto desde gfx sprites | **Diferida** |
| EntityLook renderer | **Investigación futura** |

## Recomendación de roadmap

```txt
Macro 3 / Phase 6  →  DONE / PARTIAL (diagnósticos + UX by-appearance)
Macro 3 / Phase 7  →  OPTIONAL / DEFERRED (EntityLook renderer research)
```

Ver implementación: [appearance-preview-curated-workflow-phase6.md](./appearance-preview-curated-workflow-phase6.md).

No invertir Phase 6 en renderer completo: el ROI de `by-appearance/` es coherente con Macro 3 Phases 1–4 y con las prohibiciones del programa.

## Criterios de aceptación (Phase 6 propuesta)

1. Carpeta `src/assets/item-previews/by-appearance/` documentada y usada en UI write/detail.
2. Warning si `AppearanceId > 0` y no hay PNG curado ni `AppearanceKnown`.
3. Modo audit pipeline opcional: `--items` reporta `by-appearance` missing (espejo icon audit).
4. Sin commits de `temporal-artifacts` ni extracción masiva.

## Evidencia de probe (2026-06-03)

Cliente `Client2.3.7`:

```txt
Appearances.d2o: 130 índices (654–868)
AppearanceId 0, 458, 1004: no index
AppearanceId 740: Appearance { id=740, type=7 }
Items.d2o (1..20000): 230 ítems con appearanceId>0; 129/227 ids con índice en Appearances.d2o
Item 12616 DB appearance 1004 → APPEARANCE_UNKNOWN
```

## Referencias

- [appearance-identity-audit-phase5.md](./appearance-identity-audit-phase5.md)
- [entitylook-relationship-map.md](./entitylook-relationship-map.md)
- [sprite-preview-curated-workflow-phase4.md](./sprite-preview-curated-workflow-phase4.md)
