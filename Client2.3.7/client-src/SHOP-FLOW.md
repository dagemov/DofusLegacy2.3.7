# Flujo tienda NPC / `.tiendas`

## Parche aplicado (2026-06-19)

**Archivo:** `ExchangeManagementFrame.as` en `DofusInvoker.swf`

**Problema:** `.tiendas` envía `sellerId=9001` (template id). El invoker vanilla hace
`getEntityInfos(9001)` → null → crash al leer `.look`.

**Fix:** Si la entidad no está en mapa, fallback a `Npc.getNpcById(sellerId)` + look del datacenter.

## Archivos cliente a copiar al juego

1. `Client2.3.7/DofusInvoker.swf` (parcheado)
2. `Client2.3.7/data/Launcher/VerInfo.rec`

## Servidor

Sin cambios. Ya envía `Send5761 sellerId=9001 virtual=True` correctamente.

## Probar

1. NPC vendedor normal → debe seguir abriendo
2. `.tiendas` → debe abrir tienda 9001 (63 items)
