# Phase 7D / 8 - Plan de extracción de sprites y previews del cliente

## Estado

- Fase: `7D / 8`
- Tipo: `DOCUMENTAL`
- Implementación en esta fase: `NO`
- Extracción masiva en esta fase: `NO`

## Objetivo

Definir cómo obtener previews reales del cliente para dos necesidades distintas:

1. preview básico de inventario / icono
2. preview futuro de look equipado / appearance

La meta de esta fase no es extraer ni publicar assets todavía. La meta es dejar claro:

- qué campo del item apunta a qué identidad cliente
- qué archivos del cliente son fuente de verdad
- qué herramienta aplica al cliente actual y cuál solo al legacy SWF
- qué pipeline futuro conviene construir para Angular

## Fuentes auditadas

### Cliente actual del repo oficial

Verificado en `Client2.3.7/`:

- `data/common/Items.d2o`
- `data/common/ItemTypes.d2o`
- `data/common/ItemSets.d2o`
- `data/common/Appearances.d2o`
- `data/i18n/i18n_es.d2i`
- `data/i18n/i18n_en.d2i`
- `content/gfx/items/bitmap0.d2p`
- `content/gfx/items/bitmap1.d2p`
- `content/gfx/items/vector0.d2p`
- `content/gfx/items/vector1.d2p`

Conclusión:

- el cliente actual es principalmente `D2O + D2I + D2P`
- no es un cliente centrado en `Items*.swf`

### Referencia legacy

Verificado en la documentación ya inventariada:

- `Items0.swf ... Items10.swf`
- `ItemSets0.swf`
- `ItemTypes0.swf`
- `i18n_es/*.swf`
- `i18n_en/*.swf`
- bitmaps PNG unpacked

Conclusión:

- `JPEXS / FFDec` encaja mucho mejor en la rama legacy SWF
- para el cliente actual no debe asumirse como herramienta principal

## Distinción funcional que debe quedar en la tool

### Preview por icono

Propósito:

- icono de inventario
- preview rápida en Angular
- selección visual inicial

Fuente primaria esperada:

- `IconId`
- `bitmap*.d2p`
- catálogo curado `/assets/item-previews/by-icon`

### Preview por appearance

Propósito:

- look equipado
- selector futuro de apariencia
- validación de identidad visual distinta al icono

Fuente primaria esperada:

- `AppearanceId`
- `Appearances.d2o`
- assets cliente asociados al look
- catálogo curado `/assets/item-previews/by-appearance`

## Algoritmo futuro DB -> cliente -> Angular

```txt
sunshine.items
  -> ItemId
  -> IconId
  -> AppearanceId
  -> Name / DescriptionId / Criteria / Effects

cliente actual
  -> Items.d2o
  -> ItemTypes.d2o
  -> ItemSets.d2o
  -> Appearances.d2o
  -> i18n_es.d2i / i18n_en.d2i
  -> bitmap*.d2p / vector*.d2p

pipeline offline futuro
  -> extractor no destructivo
  -> temporal-artifacts/item-sprite-extraction
  -> catálogo PNG curado
  -> Angular assets
  -> selectors read-only
```

## Reglas de diseño

- `IconId != AppearanceId`
- no cambiar `IconId` automáticamente al seleccionar `AppearanceId`
- no deducir `AppearanceId` solo por similitud de nombre
- no deducir `ClientNameId` desde runtime sin evidencia del cliente

## Caso de control

Caso seguro para control:

- `7754` / `Dofus Ocre`

Razón:

- ya funciona como carrier visible
- comparte `IconId = 23012` con `12617`
- permite distinguir claramente:
  - visibilidad por template conocido
  - preview por icono
  - ausencia de `AppearanceId` específico (`0`)

## Pipeline futuro propuesto

### Fuente

- cliente actual `D2O/D2I/D2P`
- legacy SWF solo como referencia auxiliar

### Extracción offline

Carpeta propuesta:

`Infrastructure/temporal-artifacts/item-sprite-extraction/`

Uso:

- dumps temporales
- reportes
- pruebas puntuales
- nunca catálogo masivo trackeado

### Catálogo curado

Carpetas objetivo:

- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-icon/`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-appearance/`
- `Angular-tools/Admin/RollblackLegacy.Admin.Angular/src/assets/item-previews/by-item/`

Regla:

- solo PNG curados entran al repo
- la extracción masiva queda fuera de Git

## Fases recomendadas

### 7D.1

Auditoría read-only de `AppearanceId` y fuentes cliente.

### 7D.2

Tooling offline mínimo para responder:

- si un `AppearanceId` existe en `Appearances.d2o`
- si un `IconId` puede resolverse a preview extraíble
- qué item(s) usan cada identidad

### 7D.3

Primer lote curado pequeño:

- `1-5` previews por appearance
- sin armas
- sin extracción masiva

### 7D.4

Diseño e implementación futura de:

- `ItemAppearanceSelectorComponent`

## No objetivos de esta fase

- correr JPEXS sobre todo el cliente
- modificar `Client2.3.7/`
- importar miles de PNG
- tocar gameplay
- tocar armas
- mezclar esto con `7B`

## Conclusión

La extracción de preview de sprite debe tratarse como una lane separada del preview por icono.

Para el cliente actual:

- primero `D2O/D2I/D2P`
- después, si hace falta, extracción puntual

Para la referencia legacy:

- `JPEXS / FFDec` sí es una herramienta útil y documentada
- pero no sustituye la comprensión del pipeline del cliente actual
