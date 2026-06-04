# Macro 4 — Phase 2 Writer Research

Date: `2026-06-04`  
Status: `DONE` (research + PoC; **no** publisher)

## Objetivo

Responder con evidencia:

```txt
¿Sunshine ya sabe escribir D2O?
¿Sunshine ya sabe escribir D2I?
¿O solo sabe leerlos?
```

## Cierre Phase 2

| Pregunta | Conclusión |
| --- | --- |
| D2O read | Sí (genérico Admin + tipado Sunshine si existe clase C#) |
| D2O write | Código sí; **Items.d2o no** sin clase `Item` |
| D2I read | Solo Admin (no Sunshine) |
| D2I write | **No existe** en repo |
| D2P write | Sí para packs; no sustituye publicación de template |

## Entregables

| Documento | Contenido |
| --- | --- |
| [client-writer-capability-audit.md](./client-writer-capability-audit.md) | Inventario completo + tabla |
| [client-writer-proof-of-concept.md](./client-writer-proof-of-concept.md) | Resultados PoC staging |
| Este archivo | Plan/cierre Phase 2 |

## Herramienta añadida

```txt
infrastructure/scripts/ClientD2oWriterResearch/
```

## Cambio de roadmap (aprobación requerida para Phase 3)

Antes (manual):

```txt
DB → editar cliente a mano
```

Objetivo (sigue vigente):

```txt
Angular → DB → Publication Package → Cliente
```

**Aceleración posible en D2O** si Phase 3 implementa **A** o **B**:

| Opción | Esfuerzo | Reutiliza Sunshine D2OWriter |
| --- | --- | --- |
| **A** Generar `Item.cs` desde esquema | Medio | Sí |
| **B** Writer genérico Admin (RawD2o) | Medio-alto | No |

**D2I** sigue bloqueando publicación completa — Phase 3/4 debe incluir investigación D2I (herramienta externa o implementación mínima).

## Caso control 12617

| Item | Estado |
| --- | --- |
| DB | OK |
| Cliente Items.d2o | Falta índice `12617` |
| Sunshine D2OWriter directo | Bloqueado |
| Manifiesto Phase 1 | `BLOCKED_CLIENT_WRITER_MISSING` — sigue correcto |

## Validación

```powershell
dotnet build "Sunshine net11.0/Sunshine net11.0/Sunshine.sln" /nr:false
dotnet build "infrastructure/scripts/ClientD2oWriterResearch/ClientD2oWriterResearch.csproj"
dotnet run --project "infrastructure/scripts/ClientD2oWriterResearch/ClientD2oWriterResearch.csproj"
```

## Phase 3 — NO iniciar sin aprobación

Scope propuesto:

- Staging publisher prototype (opción A o B)
- Sin escribir `Client2.3.7` original
- Sin VPS / sin launcher hasta Phase 4+

## Reglas respetadas

- No modificar cliente real
- No VPS
- Staging bajo `Infrastructure/staging-client/`
