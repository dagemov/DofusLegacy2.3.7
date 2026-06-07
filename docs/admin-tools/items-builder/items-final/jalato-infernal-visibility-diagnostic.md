# Diagnóstico — visibilidad Jalato Infernal

**Fecha:** 2026-06-02  
**Estado:** **OPERATOR_QUERY** — ItemIds deben confirmarse en Admin/DB VPS  
**Rama:** `feature/items-sets-visibility-and-vps-combat-telemetry`

## Items reportados (no visibles en cliente)

| Nombre esperado | ItemId | Estado diagnóstico |
| --- | ---: | --- |
| Jalato Infernal | **PENDING** | Buscar en `/admin/items?search=Jalato` |
| Sombrero Jalato Infernal | **PENDING** | idem |
| Capa Jalato Infernal | **PENDING** | idem |

> No hay referencia exacta a estos nombres en el repo git. Los IDs deben leerse desde `sunshine.items` en VPS o Admin local conectado a VPS.

## Checklist por item (completar operador)

Para cada fila, abrir `/admin/items/{id}/publication-status` y registrar:

| Campo | Jalato | Sombrero | Capa |
| --- | --- | --- | --- |
| ItemId | | | |
| Name | | | |
| TypeId | | | |
| IconId | | | |
| AppearanceId | | | |
| DescriptionId | | | |
| SetId | | | |
| ¿En `sunshine.items`? | Sí (Admin) | | |
| ¿En `Items.d2o` cliente? | | | |
| ¿i18n_es/en? | | | |
| ¿Icon preview OK? | | | |
| ¿Package staging? | | | |
| ¿Aplicado cliente real? | | | |
| PublicationState | | | |
| VisibilityState | | | |
| PreviewState | | | |
| ¿Requiere restart world? | | | |

## Causa raíz probable (patrón conocido)

```txt
DB/Admin OK  →  cliente NO parcheado  →  INVISIBLE / VISIBLE_WITH_PATCH
```

El pipeline actual publica solo tras:

1. `stage-item-package.ps1 -ItemIds ...`
2. `validate-publication-package`
3. `CONFIRM_BACKUP=1` + `CONFIRM_PUBLISH=1` + `apply-package-to-real-client`
4. `validate-real-client`
5. `CONFIRM_RESTART=1` restart world (si aplica)

La UI Admin **no** publica automáticamente; tras guardar muestra:

> Item guardado en DB. Pendiente de publicar al cliente.

## SQL de descubrimiento (VPS, operador)

```sql
SELECT Id, Name, TypeId, IconId, AppearanceId, DescriptionId, ItemSetId
FROM items
WHERE Name LIKE '%Jalato%' OR Name LIKE '%Infernal%'
ORDER BY Id;
```

## Acción recomendada

```powershell
# Tras obtener ItemIds reales:
.\infrastructure\artifacts\items-publication\stage-item-package.ps1 -ItemIds 126XX,126YY,126ZZ -TemplateItemId 7754

# Publish controlado (solo con aprobación):
$env:CONFIRM_BACKUP='1'
$env:CONFIRM_PUBLISH='1'
# ... apply-package-to-real-client + validate-real-client
```

## Decisión

| Resultado | Acción |
| --- | --- |
| Package validado, publish pendiente | **READY_FOR_OPERATOR_PUBLISH** |
| Falta i18n/D2O en cliente | Ejecutar publish controlado |
| Set sin `ItemSets.d2o` | Staging opcional ItemSets + mismo flujo |
