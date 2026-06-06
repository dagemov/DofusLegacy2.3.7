# Incidente VPS — Cliente no conecta tras rebuild telemetría

**Fecha:** 2026-06-06  
**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`  
**VPS:** `174.138.35.107`  
**Clasificación final:** `RESTORED_WITH_TELEMETRY_ON`

## Síntoma

Tras rebuild de `sunshine-server` con telemetría de combate activada, el cliente mostraba:

```txt
Conexión al servidor fracasó
```

## Diagnóstico (Paso 1–4)

| Check | Resultado |
| --- | --- |
| `sunshine-server` | **Restarting (139)** — crash loop |
| `sunshine-db` | **Up (healthy)** — DB intacta |
| Puertos cliente | `446` (auth), `3467` (world) — **no escuchaban** mientras crasheaba |
| Telemetría env | `FIGHT_TELEMETRY_ENABLED=true`, directorio `/app/logs/combat` |
| Rollback imagen | **No necesario** |

### Error raíz (logs)

```txt
[FAIL] [ 25%] Items : Unable to read beyond the end of the stream.
EffectManager.GetEffects(String hexa) — línea 46
ItemsLoader.Initialize() — línea 41
exit 139
```

**Causa:** items creados vía Admin Angular con formato **ObjectEffect** (`0046` = typeId 70) en columna `items.Effects`. El loader del WorldServer usa `EffectManager.GetEffects(string)` que espera el formato **legacy spell-effect** (`0000006F` + cola larga). No es fallo de telemetría ni de Config.xml.

### Items afectados

| Id | Nombre | Effects (backup) |
| --- | --- | --- |
| 12618 | Capa del gay | ver `Infrastructure/temporal-artifacts/combat-telemetry/broken-items-effects-backup.txt` |
| 12619 | Sombrero del Gay | idem |
| 12620 | Sombrero del Jalato infernal | idem |
| 12621 | Capa del jalato infernal | idem |
| 12622 | PanDofus | idem |

Items **no** afectados (formato legacy OK): `12616` ADMIN TEST, `12617` Dofus Tester.

### Config / puertos (post-restore)

```txt
AuthIp=127.0.0.1  AuthPort=446
WorldIp=127.0.0.1 WorldPort=3467
WORLD_PUBLIC_HOST=127.0.0.1  (.env VPS)
```

`Test-NetConnection` desde Windows: **446** y **3467** → `TcpTestSucceeded : True`.

> Nota operador: si el cliente sigue fallando tras confirmar servidor READY, revisar que el cliente apunte a `174.138.35.107` y considerar fijar `WORLD_PUBLIC_HOST=174.138.35.107` en `.env` + restart controlado.

## Remediación aplicada

**Sin** `docker compose down -v`, **sin** restore DB completo, **sin** desactivar telemetría.

```sql
-- Effects vacío canónico (hex ASCII "0000", 4 chars)
UPDATE items SET Effects=0x30303030 WHERE Id IN (12618,12619,12620,12621,12622);
```

```bash
docker restart sunshine-server
```

## Validación final

| Criterio | Estado |
| --- | --- |
| `docker ps` sunshine-server UP | **OK** |
| Logs auth/world READY 100% | **OK** — `Les serveurs acceptent maintenant les connexions` |
| Puertos 446 / 3467 accesibles | **OK** (VPS + Windows) |
| Telemetría | **ON** (`CombatTelemetryEnabled=true`) |
| DB intacta | **OK** — solo 5 filas `Effects` neutralizadas |
| Cliente reconecta | **PENDING_OPERATOR** |
| Rollback imagen | **No** |

### Rollback imagen (Paso 6)

```txt
rollback image attempted: no
image id before: sunshine-emu-sunshine (actual en contenedor df813e715ead)
image id after:  (sin cambio)
```

## Seguimiento — puertos cliente (mismo día)

Con servidor READY, el cliente seguía fallando: `connection.port=2450` pero VPS publicaba `446`/`3467` y `WORLD_PUBLIC_HOST=127.0.0.1`.

**Fix:** `.env` → `2450`/`5557`/`174.138.35.107`. Detalle: [vps-client-port-host-diagnostic.md](./vps-client-port-host-diagnostic.md).

## Seguimiento técnico (no bloqueante para conexión)

1. **Codec Admin → runtime:** alinear escritura de `items.Effects` con el parser que usa `ItemsLoader` o migrar loader a `ObjectEffectSerializer.Deserialize` — ver [items-builder-effects-serialization-audit.md](../admin-tools/items-builder/items-builder-effects-serialization-audit.md).
2. Restaurar stats de items 12618–12622 re-serializando en formato legacy (o fix de codec + re-save desde Admin).
3. Smoke combate + JSONL según [combat-vps-telemetry-deploy-gate.md](./combat-vps-telemetry-deploy-gate.md).

## Pasos no ejecutados (innecesarios)

- `disable-vps-combat-telemetry.ps1` — telemetría no era causa del crash.
- Restore desde `/root/backups/sunshine/sunshine-pre-restart-20260606T153909Z.sql`.
