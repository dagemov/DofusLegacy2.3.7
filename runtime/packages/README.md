# Parches del cliente (OneLauncher)

Edita **solo este archivo** para publicar parches nuevos. No hace falta reiniciar Docker.

## Ruta en la VPS (WinSCP)

```
/opt/dofus-2.0.0/runtime/packages/
├── Updates.xml      ← lista de parches (version + archivo)
├── V0.rar           ← cliente base
└── config.zip       ← parche incremental
```

## URLs publicas

- Manifiesto XML: `https://rollblack-legacy.onesv.online/api/launcher/updates.xml`
- Manifiesto JSON: `https://rollblack-legacy.onesv.online/api/launcher/manifest`
- Descarga parche: `https://rollblack-legacy.onesv.online/api/files/{archivo}`

## Publicar un parche nuevo

1. Sube el `.zip` o `.rar` a `runtime/packages/`.
2. Edita `Updates.xml` y agrega una entrada con version mayor:

```xml
<update>
  <version>2.0.2</version>
  <file>mi-parche.zip</file>
</update>
```

3. Guarda. Los clientes lo veran al abrir el launcher (sin reiniciar contenedores).

## Ejemplo actual

- `2.0.0` → `V0.rar` (cliente completo)
- `2.0.1` → `config.zip` (parche de configuracion)
