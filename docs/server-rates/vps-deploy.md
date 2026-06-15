# Deploy VPS — configuración de rates del servidor

Procedimiento profesional para desplegar `config_rates_Server.txt` y el binario actualizado en producción.

## Pre-requisitos

- Acceso SSH a la VPS (`174.138.35.107` o host configurado).
- Rama mergeada o imagen Docker construida con `feature/server-rates-config`.
- Documentación operativa: [README.md](./README.md).

## 1. Conectar y verificar estado

```bash
ssh root@<VPS_HOST>
docker ps
```

Confirmar que el contenedor `sunshine` (o nombre del servicio en `docker-compose`) está **Up**.

## 2. Backup antes de cambios

Crear carpeta con fecha:

```bash
BACKUP_DIR="/root/backups/$(date +%Y-%m-%d_%H-%M)"
mkdir -p "$BACKUP_DIR"
```

### Base de datos

```bash
# Ajustar usuario/BD según .env del servidor
docker exec <mysql_container> mysqldump -u root -p<PASSWORD> <database_name> \
  > "$BACKUP_DIR/mysql-dump.sql"
```

O usar script del repo (desde máquina con acceso):

```powershell
.\infrastructure\scripts\PublicationBackup\backup-db.sh
```

### Docker Compose y scripts

```bash
cp /opt/dofus-2.0.0-build/docker-compose.yml "$BACKUP_DIR/"
cp /opt/dofus-2.0.0-build/docker-compose.vps.yml "$BACKUP_DIR/" 2>/dev/null || true
cp /opt/dofus-2.0.0-build/.env "$BACKUP_DIR/.env.redacted"
```

### Binario / config actual

```bash
docker cp sunshine:/app/Config.xml "$BACKUP_DIR/Config.xml"
docker cp sunshine:/app/Config/config_rates_Server.txt "$BACKUP_DIR/" 2>/dev/null || true
```

## 3. Subir `config_rates_Server.txt`

En el host VPS, crear o editar:

```bash
mkdir -p /opt/dofus-2.0.0-build/runtime-config/Config
nano /opt/dofus-2.0.0-build/runtime-config/Config/config_rates_Server.txt
```

Contenido inicial recomendado:

```ini
XP_RATE=2
DROP_RATE=1
KAMAS_RATE=1
PP_RATE=1
WEAPON_USES_PER_TURN=2
WEAPON_USES_PER_FIGHT=0
SPELL_USES_DEFAULT=0
```

Montar como volumen en `docker-compose.vps.yml` (si no existe aún):

```yaml
services:
  sunshine:
    volumes:
      - ./runtime-config/Config:/app/Config
```

## 4. Desplegar binario / imagen

### Build en VPS (patrón existente)

```powershell
# Desde repo local — sincronizar y build-gate
.\scripts\sync-build-gate.ps1
```

En VPS:

```bash
cd /opt/dofus-2.0.0-build
docker compose -f docker/docker-compose.yml -f docker-compose.vps.yml build sunshine
docker compose -f docker/docker-compose.yml -f docker-compose.vps.yml up -d sunshine
```

## 5. Reiniciar y verificar logs

```bash
docker compose -f docker/docker-compose.yml -f docker-compose.vps.yml restart sunshine
docker logs sunshine 2>&1 | grep -E "Server rates"
```

Salida esperada:

```
[ Info ] Server rates loaded from '/app/Config/config_rates_Server.txt'
[ Info ] Server rates applied: XP_RATE=2, DROP_RATE=1, KAMAS_RATE=1, PP_RATE=1, WEAPON_USES_PER_TURN=2, WEAPON_USES_PER_FIGHT=0, SPELL_USES_DEFAULT=0
```

## 6. Pruebas en juego

| Prueba | Criterio |
|--------|----------|
| XP | Combate PvM → XP coherente con `XP_RATE` |
| Arma | `WEAPON_USES_PER_TURN=2` → máximo 2 ataques/turno |
| Drop/PP | Con `PP_RATE` > 1, mejor tasa de drop (muestra estadística) |
| Config inválida | Cambiar `XP_RATE=abc`, reiniciar → warning + default, servidor arranca |
| Sin archivo | Borrar archivo, reiniciar → se recrea con defaults |

## 7. Rollback

```bash
# Restaurar config
docker cp "$BACKUP_DIR/Config/config_rates_Server.txt" sunshine:/app/Config/

# Restaurar imagen anterior (si se etiquetó)
docker compose -f docker/docker-compose.yml -f docker-compose.vps.yml up -d sunshine

# Restaurar BD solo si hubo migración (no aplica a este feature)
# mysql < "$BACKUP_DIR/mysql-dump.sql"
```

## Checklist de aceptación

- [ ] Backup en `backups/YYYY-MM-DD_HH-mm/` antes del deploy
- [ ] Logs muestran ruta y valores de rates cargados
- [ ] Cambiar `XP_RATE` + reinicio aplica sin recompilar
- [ ] Cambiar `WEAPON_USES_PER_TURN` + reinicio aplica sin recompilar
- [ ] Servidor no crashea con archivo faltante o valor inválido
