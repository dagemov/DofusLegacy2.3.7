# Auditoría de mapeo cliente: ItemId, IconId, AppearanceId y nombre

## Estado

- Fase: `7D / 8`
- Tipo: `DOCUMENTAL`
- Runtime modificado en esta fase: `NO`

## Hallazgos verificados

### 1. Qué existe realmente en `sunshine.items`

Confirmado por esquema y código:

- `Id`
- `Name`
- `TypeId`
- `DescriptionId`
- `IconId`
- `ItemSetId`
- `Criteria`
- `AppearanceId`
- `Effects`

No existe hoy una columna dedicada:

- `ClientNameId`

Conclusión:

- el runtime Sunshine actual guarda `Name` como texto runtime
- la identidad cliente multilenguaje no está modelada hoy como columna explícita en DB

### 2. Qué significa cada campo

#### `ItemId`

- identidad principal del template runtime en DB
- es lo que el servidor termina enviando como `objectGID`
- si el cliente no conoce ese template, el item no renderiza

#### `IconId`

- identidad de icono de inventario / preview básica
- sirve para buscar PNG curado en Angular
- conceptualmente apunta al icono cliente, no al look equipado

#### `AppearanceId`

- identidad de look / appearance equipable
- no debe confundirse con el icono
- hoy no hay resolver tipado en el repo para leer `Appearances.d2o`

#### `ClientNameId`

- no existe como columna actual de `sunshine.items`
- en un pipeline de publicación cliente debe provenir del metadata cliente publicado
- la tool futura debería modelarlo explícitamente aunque hoy el runtime no lo persista

#### `Effects`

- payload runtime serializado del item
- afecta stats, no visibilidad cliente por template

#### `Criteria`

- gating lógico / condiciones
- tampoco resuelve icono ni look

## Patrón DB -> cliente

```txt
DB:
  ItemId -> template runtime
  IconId -> identidad de icono
  AppearanceId -> identidad de look
  Name -> texto runtime actual
  DescriptionId -> id relacionado con descripción cliente futura

Servidor:
  envía objectGID = Template.Id

Cliente:
  busca objectGID/templateId en Items.d2o
  resuelve nombre en D2I
  resuelve icono en bitmap/vector packs
  resuelve look equipado desde appearance assets si aplica
```

Conclusión dura:

- `IconId` no sustituye a `ItemId`
- `AppearanceId` no sustituye a `ItemId`
- `IconId` tampoco sustituye a `AppearanceId`

## Qué usa Angular hoy

### Preview básica

Angular actual usa:

- `IconId`
- catálogo curado `/assets/item-previews/by-icon`

Eso es correcto para:

- selector visual de icono
- preview rápida en admin

Eso no equivale todavía a:

- look equipado
- publicación cliente completa

## Caso control: `7754` / `Dofus Ocre`

Consulta DB validada:

```txt
Id = 7754
Name = Dofus Ocre
TypeId = 23
DescriptionId = 40905
IconId = 23012
AppearanceId = 0
ItemSetId = -1
Level = 6
```

Interpretación:

- `7754` sí es un template visible para el cliente
- `IconId = 23012` es compartido con `12617`
- `AppearanceId = 0` confirma que el caso de control funciona sin look equipado especial

Archivo cliente esperado:

- `Client2.3.7/data/common/Items.d2o`

Resolución de nombre cliente:

- `Client2.3.7/data/i18n/i18n_es.d2i`
- `Client2.3.7/data/i18n/i18n_en.d2i`

Resolución de icono:

- `Client2.3.7/content/gfx/items/bitmap*.d2p`

Estado actual de preview Angular:

- no existe hoy un PNG curado `23012.png` en `by-icon/`
- por tanto el control sirve como patrón de identidad cliente, no como preview PNG ya curado

## Caso custom: `12617` / `Dofus Tester`

Consulta DB validada:

```txt
Id = 12617
Name = Dofus Tester
TypeId = 23
DescriptionId = 50091
IconId = 23012
AppearanceId = 0
ItemSetId = -1
Level = 6
```

Comparación con `7754`:

- comparten `TypeId`
- comparten `IconId`
- comparten `AppearanceId = 0`
- difieren en `ItemId`, `Name`, `DescriptionId`

Conclusión:

- la invisibilidad de `12617` no se debe a `IconId`
- tampoco a `AppearanceId`
- se debe a que el cliente no conoce `ItemId = 12617` como template publicado

## Caso `AppearanceId = 458`

Resultado auditado en DB actual:

- no existe ningún row en `sunshine.items` con `AppearanceId = 458`

Resultado auditado en cliente actual:

- no se validó una correspondencia exacta a `Sombrero Jalato`
- el repo aún no trae un lector tipado de `Appearances.d2o`
- por tanto no hay evidencia suficiente para afirmar hoy que `458 = Sombrero Jalato`

Corrección operativa:

- tratar `AppearanceId = 458 -> Sombrero Jalato` como hipótesis no verificada
- no usar ese mapeo como verdad en UI ni en docs de producto
- resolverlo en una futura fase offline con tooling específico de appearance

## Qué pasa en escenarios mixtos

### `IconId` existe pero `AppearanceId` no

Resultado esperado:

- preview básica de inventario puede funcionar
- look equipado queda sin validar

### `AppearanceId` existe pero no hay PNG

Resultado esperado:

- puede existir identidad de look
- Angular no necesariamente tendrá un preview curado
- se necesita extractor offline o captura curada

## Conclusión práctica para la tool

La tool futura debe modelar tres superficies distintas:

1. `Item template visibility`
2. `Icon preview readiness`
3. `Appearance preview readiness`

Warnings sugeridas:

- `TEMPLATE_ID_UNKNOWN_TO_CLIENT`
- `ICON_PREVIEW_NOT_CURATED`
- `APPEARANCE_MAPPING_UNVERIFIED`
- `CLIENT_NAME_ID_NOT_MODELED`

Eso evita repetir el error de asumir que un item es "visible" solo porque tiene `IconId`.
