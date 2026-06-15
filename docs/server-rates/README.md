# Configuración de rates del servidor

Configuración centralizada y editable **sin recompilar** para los multiplicadores y límites de combate introducidos en PR #32.

## Archivo

| Propiedad | Valor |
|-----------|-------|
| Nombre | `config_rates_Server.txt` |
| Ubicación runtime | `{directorio_ejecución}/Config/config_rates_Server.txt` |
| Plantilla en repo | `config/config_rates_Server.txt` |

En Docker/VPS el directorio de ejecución es `/app`, por lo que la ruta efectiva es `/app/Config/config_rates_Server.txt`.

## Contenido inicial

```ini
XP_RATE=2
DROP_RATE=1
KAMAS_RATE=1
PP_RATE=1
WEAPON_USES_PER_TURN=2
WEAPON_USES_PER_FIGHT=0
SPELL_USES_DEFAULT=0
```

## Claves

| Clave | Efecto | `0` significa |
|-------|--------|---------------|
| `XP_RATE` | Multiplicador de XP de combate (y `GameRates.ApplyXp`) | Rate 0 (sin bonus) |
| `DROP_RATE` | Multiplicador de probabilidad base de drop | Rate 0 |
| `KAMAS_RATE` | Multiplicador de kamas de combate | Rate 0 |
| `PP_RATE` | Multiplicador global de prospección en drops | Rate 0 |
| `WEAPON_USES_PER_TURN` | Máximo de ataques con arma por turno | **Ilimitado** |
| `WEAPON_USES_PER_FIGHT` | Máximo de ataques con arma por combate | **Ilimitado** |
| `SPELL_USES_DEFAULT` | Límite por turno si el hechizo no define `MaxCastPerTurn` | **Ilimitado** |

Líneas que empiezan por `#`, `//` o `;` son comentarios.

## Comportamiento al arranque

1. `GameConfig.Load()` lee `Config.xml` (red, BD, rates legacy).
2. `ServerRatesProvider.Reload()` lee o **crea** `Config/config_rates_Server.txt`.
3. `GameRates.Reload()` aplica rates del archivo (prioridad sobre `RateXp`/`RateDrop`/`RateKamas` en `Config.xml`).

### Si el archivo no existe

Se crea automáticamente en `Config/` con valores sembrados desde `Config.xml` cuando está disponible, o defaults seguros.

### Si una línea es inválida

El servidor escribe `[ Warning ]` en consola/logs y usa el default de esa clave. **No crashea.**

### Logs esperados

```
[ Info ] Server rates loaded from '/app/Config/config_rates_Server.txt'
[ Info ] Server rates applied: XP_RATE=2, DROP_RATE=1, KAMAS_RATE=1, PP_RATE=1, WEAPON_USES_PER_TURN=2, WEAPON_USES_PER_FIGHT=0, SPELL_USES_DEFAULT=0
```

## Cómo cambiar rates

1. Editar `Config/config_rates_Server.txt` en el servidor (o volumen montado).
2. Reiniciar el contenedor/servicio Sunshine.
3. Verificar logs de arranque (líneas `Server rates applied`).
4. Probar en juego: XP tras combate, usos de arma, drops si aplica.

### Ejemplo: XP x3

```ini
XP_RATE=3
```

Reiniciar → logs deben mostrar `XP_RATE=3`.

### Ejemplo: 1 ataque con arma por turno

```ini
WEAPON_USES_PER_TURN=1
```

## Código relacionado

| Clase | Rol |
|-------|-----|
| `ServerRatesConfig` | Modelo de valores |
| `ServerRatesConfigLoader` | Lectura/escritura del archivo |
| `IServerRatesProvider` / `ServerRatesProvider` | Acceso en runtime |
| `GameRates` | Aplica multiplicadores XP/Drop/Kamas/PP |
| `CharacterFighter` | Límites de arma por turno/combate |
| `SpellHistory` | `SPELL_USES_DEFAULT` |

Inspección de hardcodes de PR #32: [pr32-inspection.md](./pr32-inspection.md).

## Tests

```powershell
dotnet test "Sunshine net11.0/Sunshine net11.0/Sunshine.BaseServer.Tests/Sunshine.BaseServer.Tests.csproj"
```

Cubre: archivo válido, archivo faltante, valores inválidos, y resolución de `WEAPON_USES_PER_TURN`.

## Compatibilidad

- `Config.xml` sigue siendo necesario para puertos, BD, JobXp, MountXp y rangos de kamas de combate.
- Si `config_rates_Server.txt` no se ha cargado aún, `GameRates` usa solo `Config.xml` (comportamiento anterior).
