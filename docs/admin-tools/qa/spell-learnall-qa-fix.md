# QA fix — `.spell learnall` (bulk spells)

**Tipo:** comando temporal de validación manual (effects / combate).  
**Rol mínimo:** `Administrator` (5).  
**Fecha:** 2026-06-05

## Uso

```text
.spell learnall
```

Otorga todos los hechizos registrados en `SpellManager` al personaje actual, sin usar `.spell add <id>` uno por uno.

## Por qué no rompe el servidor

| Riesgo | Mitigación |
|--------|------------|
| Lag por miles de paquetes | **Un solo** `SpellListMessage` al final (no N × `SpellUpgradeSuccessMessage`) |
| Combate inconsistente | Bloqueado si el personaje está en pelea |
| Hechizos inválidos | Solo IDs presentes en `SpellManager.Spells` |
| Duplicados | `HasSpell` antes de añadir |
| Persistencia | Igual que `.spell add`: memoria hasta `.save` / auto-save |

## Archivos tocados

| Archivo | Cambio |
|---------|--------|
| `Sunshine.WorldServer/Commands/Administrator/LearnAllSpellsCommand.cs` | Comando nuevo |
| `Sunshine.WorldServer/Game/Actors/Characters/Spells/SpellInventory.cs` | `LearnAllAvailableSpellsForQa()` |
| `Sunshine.csproj` | `<Compile Include>` del comando |

## Eliminar en el futuro

1. Borrar `LearnAllSpellsCommand.cs`
2. Quitar `LearnAllAvailableSpellsForQa()` de `SpellInventory.cs` (o dejar el método si se reutiliza)
3. Quitar la línea `<Compile Include="...LearnAllSpellsCommand.cs" />` de `Sunshine.csproj`
4. Rebuild Docker / redeploy

## Mejoras posibles

- Filtro por clase (`breed`) o por categoría (solo hechizos jugables)
- Parámetro opcional de nivel base (`level 1` vs max)
- Persistir en DB inmediatamente tras bulk (hoy depende de `.save`)
- Mover a rama `feature/qa-tools` o config flag `QA_SPELL_LEARNALL_ENABLED`

## Relación con `.spell add`

```text
.spell add 1901     → un hechizo, un SpellUpgradeSuccessMessage
.spell learnall     → todos los del SpellManager, un SpellListMessage
```
