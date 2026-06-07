# Comandos de chat y admin — Sunshine WorldServer

Guía local para probar comandos in-game. El servidor carga los comandos desde `Sunshine.WorldServer/Commands/` al arrancar (~27 comandos).

## Cómo se activan

| Canal | Prefijo | Handler |
|-------|---------|---------|
| **Chat del juego** (principal) | `.` (punto) | [`ChatHandler.cs`](../Sunshine%20net11.0/Sunshine%20net11.0/Sunshine.WorldServer/Handlers/Chat/ChatHandler.cs) → `CommandDispatcher` |
| Consola debug cliente | `/` | Solo cliente (no envía al servidor) |
| Admin quiet (panel) | sin punto | [`AdminHandler.cs`](../Sunshine%20net11.0/Sunshine%20net11.0/Sunshine.WorldServer/Handlers/Admins/AdminHandler.cs) — ej. `moveto <mapId>` |

### Uso en el chat

1. Escribe en el **chat general** (o el canal activo que acepte mensajes normales).
2. El mensaje debe **empezar con `.`** (punto).
3. Ejemplo: `.help`, `.go 7411`, `.item 2411 1`

```text
.help          → lista comandos según tu rol
.go 88212791   → teletransporte (Moderator+)
.god on        → modo dios (Administrator)
```

Si el rol no alcanza, verás `Unknown command.` aunque el comando exista.

## Roles (`accounts.Role`)

Definidos en [`RoleEnum.cs`](../Sunshine%20net11.0/Sunshine%20net11.0/Sunshine.Protocol/Enums/RoleEnum.cs):

| Valor DB | Enum | Acceso |
|----------|------|--------|
| 1 | `Player` | Comandos jugador |
| 2 | `Moderator` | + moderación (tp, items, kamas, etc.) |
| 3 | `GameMaster_Padawan` | (reservado; sin comandos extra en esta build) |
| 4 | `GameMaster` | (reservado) |
| 5 | `Administrator` | + god, hp, bank, reload, stop |

La comprobación es `client.Account.Role >= rol_requerido` ([`CommandManager.cs`](../Sunshine%20net11.0/Sunshine%20net11.0/Sunshine.WorldServer/Commands/CommandManager.cs)).

**Importante:** el rol se lee al **login**. Tras cambiar `Role` en la BD, **desconecta y vuelve a entrar** para que aplique.

## Dar admin a una cuenta (SQL)

Tablas:

- `accounts` — campo `Role`
- `characters` — personaje (`Name`)
- `worlds_characters` — enlaza `Account` ↔ `Owner` (Id personaje)

```sql
-- Buscar cuenta por nombre de personaje
SELECT c.Id, c.Name, wc.Account, a.Username, a.Role
FROM characters c
JOIN worlds_characters wc ON wc.Owner = c.Id
JOIN accounts a ON a.Id = wc.Account
WHERE LOWER(c.Name) = 'maestro-yaco';

-- Administrator = 5
UPDATE accounts a
JOIN worlds_characters wc ON wc.Account = a.Id
JOIN characters c ON c.Id = wc.Owner
SET a.Role = 5
WHERE LOWER(c.Name) = 'maestro-yaco';
```

Script reutilizable: [`docker/grant-admin-maestro-yaco.sql`](../docker/grant-admin-maestro-yaco.sql)

### Cuenta configurada (VPS test)

| Campo | Valor |
|-------|--------|
| Personaje | `Maestro-Yaco` |
| AccountId | `264` |
| Username | `admin` |
| Role | `5` (Administrator) |

Ejecutar en el contenedor MariaDB:

```bash
docker exec -i -e MYSQL_PWD=<MYSQL_APP_PASSWORD> sunshine-db \
  mysql -usunshine sunshine < grant-admin-maestro-yaco.sql
```

## Comandos por rol

### Player (todos)

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `.help` | — | Lista comandos disponibles para tu rol |
| `.tp` | — | Abre panel de teletransporte custom |
| `.dj` | — | Panel mazmorras |
| `.xp` | — | Panel XP |
| `.restat` | — | Restat personaje |
| `.parchotage` | — | Parchotage |
| `.debugmap` | — | TP mapa debug (2323, celda 328) |
| `.align` | `neutre` / `ange` / `demon` / `0`/`1`/`2` | Cambiar alineación |

### Moderator (Role ≥ 2)

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `.go` | `<mapId>` | Teletransporte a mapa |
| `.save` | — | Guardar mundo |
| `.look` | *(ver código)* | Cambiar look |
| `.info` | — | Cuentas conectadas |
| `.a` | `<mensaje>` | Anuncio global |
| `.kamas` | `<cantidad>` o `add`/`remove` | Kamas |
| `.item` | `<itemId> <qty> [nombre]` | Dar objeto |
| `.levelup` | `<nivel>` | Subir nivel (1–200) |
| `.spell add` | *(ver código)* | Aprender hechizo |
| `.spell learnall` | Administrator | **[QA]** Todos los hechizos del SpellManager (ver `docs/admin-tools/qa/spell-learnall-qa-fix.md`) |
| `.honor` | *(ver código)* | Honor (máx 20000) |
| `.monster` | `spawn <id> [count] [group]` | Spawn monstruos |
| `.npc` | *(ver código)* | Spawn NPC |
| `.mount equip` | *(ver código)* | Montura |
| `.interactives show` | — | Mostrar interactivos en mapa |

### Administrator (Role ≥ 5)

| Comando | Uso | Descripción |
|---------|-----|-------------|
| `.god` | `on` / `off` [jugador] | Modo dios (sin daño) |
| `.hp` | — | Restaura PV |
| `.bank` | [nombre] | Abre banco |
| `.reload` | `interactives` | Recarga interactivos en mapas |
| `.stop` | — | Detiene el servidor |

## Comandos con espacio (dos palabras)

El dispatcher une la primera y segunda palabra como nombre de comando:

```text
.spell add <spellId>
.item <id> <qty>
.monster spawn <monsterId> <count>
.mount equip ...
.interactives show
.reload interactives
```

## Panel admin (`moveto`)

Si el cliente tiene derechos admin en UI, el mensaje `AdminQuietCommandMessage` acepta:

```text
moveto <mapId>
```

Implementado en `AdminHandler` (no lleva punto).

## Probar rápido (Maestro-Yaco)

1. Desconectar y reconectar (rol actualizado).
2. En chat: `.help` — debe listar comandos de Administrator.
3. Smoke tests:
   - `.god on`
   - `.hp`
   - `.go 7411` (mapa ejemplo; usar mapId válido)
   - `.item 2411 1` (pan, ID ejemplo)
   - `.levelup 50`

## Añadir un comando nuevo

1. Crear clase que herede `WorldCommand` en `Sunshine.WorldServer/Commands/`.
2. Atributo `[CommandHandler("nombre", RoleEnum.XXX)]`.
3. Implementar `Execute()` y `Description`.
4. Si es archivo nuevo: añadir `<Compile Include>` en `Sunshine.csproj` (si `EnableDefaultCompileItems=false`).
5. Rebuild Docker / reiniciar `sunshine-server`.

## Referencias código

| Pieza | Ruta |
|-------|------|
| Carga comandos | `Sunshine.BaseServer/Loaders/Commands/CommandsLoader.cs` |
| Dispatch | `Sunshine.WorldServer/Commands/CommandDispatcher.cs` |
| Chat entrada | `Sunshine.WorldServer/Handlers/Chat/ChatHandler.cs` (línea `.`) |
| Lista comandos | `Sunshine.WorldServer/Commands/**/*.cs` |
