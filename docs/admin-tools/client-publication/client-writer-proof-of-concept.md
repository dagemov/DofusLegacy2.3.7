# PoC — D2O writer research (Phase 2)

Date: `2026-06-04`  
Tool: `infrastructure/scripts/ClientD2oWriterResearch`

## Objetivo

Probar en **copia staging** si `Sunshine.Protocol.D2OWriter` puede hacer round-trip de `Items.d2o` sin tocar el cliente real.

## Setup

```txt
Source:  Client2.3.7/data/common/Items.d2o
Staging: Infrastructure/staging-client/data/common/Items.d2o
Report:  Infrastructure/temporal-artifacts/client-d2o-writer-research/research-results.json
```

## Comando

```powershell
dotnet run --project "infrastructure/scripts/ClientD2oWriterResearch/ClientD2oWriterResearch.csproj"
```

## Resultados ejecutados

### Integridad binaria (sin Sunshine)

| Métrica | Valor |
| --- | --- |
| Entradas índice | `11067` |
| `7754` en índice | Sí |
| `12617` en índice | No |
| Copia staging SHA256 | Igual al original |

### Sunshine D2OReader sobre Items.d2o

```
InvalidOperationException: Sequence contains no elements
```

**Causa:** `FindType("Item")` no encuentra clase C# — el archivo D2O declara clase `Item`, pero el repo solo define `Breed`.

Clases detectadas en archivo (probe binario):

```txt
Item, Weapon, EffectInstance, EffectInstanceDice, EffectInstanceInteger
```

### Sunshine D2OWriter round-trip

**No llegó a reescribir.** El constructor `D2OWriter(path)` llama `OpenWrite()` → `ResetMembersByReading()` → mismo fallo que el reader.

**Conclusión PoC:** round-trip con `D2OWriter` **no validado** para `Items.d2o` en el estado actual del repo.

### Caso 12617

No se añadió entrada al índice. Publicar `12617` requiere Phase 3 (writer genérico o generación de `Item.cs`).

## Qué sí funcionaría hoy

| Archivo | Condición |
| --- | --- |
| `Breeds.d2o` | Si existiera en workspace + clase `Breed` ya definida |
| Cualquier D2O cuya clase C# esté en `Tools/D2o/Classes/` | Mismo patrón que BreedsLoader |

## Próximo experimento (Phase 3 — con aprobación)

1. Extraer esquema de campos de `Item` desde `RawD2oFile` (Admin) para objeto `7754`.
2. Generar stub `Item.cs` con `[D2OClass("Item", "...")]`.
3. Repetir PoC round-trip en `Items.roundtrip.d2o` staging.
4. Solo entonces intentar `Write(itemClone, 12617)`.

## Riesgos observados

- Abrir `D2OWriter` sin `EndWriting` deja handles bloqueados en Windows.
- `StartWriting` trunca el archivo — siempre usar staging + backup `.bak`.
