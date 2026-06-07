# Spell Builder Production Parity - Client Publication Strategy

Fecha: `2026-06-07`
Rama auditada: `feature/spell-builder-api-migration`

## Objetivo

Definir que falta para llegar a un flujo productivo de publicacion cliente de spells sin tocar cliente real en esta fase.

## Fuentes revisadas

- `legacy-reference/Rollback.Admin/Services/SpellClientPublishService.cs`
- `legacy-reference/Rollback.Admin/Services/SpellAdminService.cs`
- `infrastructure/scripts/ClientItemPublicationPipeline/Program.cs`
- `infrastructure/scripts/PublicationBackup/backup-client.ps1`
- `Client2.3.7/config.xml`
- `Client2.3.7/data/Launcher/VerInfo.rec`

## Hallazgos

### 1. Legacy publicaba spells como parte natural del guardado

- `SpellAdminService.SaveAsync(...)` terminaba llamando a publicacion cliente.
- `SpellClientPublishService` generaba:
  - backup previo
  - warnings de publicacion
  - actualizacion de spell presentation
  - actualizacion de spell levels
  - actualizacion de textos

### 2. El repo actual si tiene pipeline de publicacion, pero solo de items

- El entrypoint `infrastructure/scripts/ClientItemPublicationPipeline/Program.cs` ofrece modos para:
  - `stage-item-publication`
  - `validate-publication-package`
  - `apply-package-to-real-client`
  - `validate-real-client`
- El pipeline actual trabaja sobre:
  - `Items.d2o`
  - `i18n_es.d2i`
  - `i18n_en.d2i`
  - en algunos casos `ItemTypes.d2o`
- No se encontro un pipeline equivalente para spells.

### 3. El backup cliente actual tampoco cubre spells

- `backup-client.ps1` preserva:
  - `Items.d2o`
  - `ItemSets.d2o`
  - `ItemTypes.d2o`
  - `i18n_es.d2i`
  - `i18n_en.d2i`
- No incluye `SpellLevels.d2o`.

### 4. El cliente actual contiene artefactos de spells

- `VerInfo.rec` lista `data/common/SpellLevels.d2o`.
- `config.xml` declara `data.path.i18n`.
- `VerInfo.rec` tambien muestra:
  - `content/gfx/spells/*.swf`
  - `content/scripts/spellFx/*.dx`
  - `data/i18n/i18n_es.d2i`
  - `data/i18n/i18n_en.d2i`

### 5. No hay evidencia en esta fase de una automatizacion actual de spells lista para usar

- No se detecto en `infrastructure/scripts` una pipeline actual dedicada a publicar spells.
- Por evidencia local, la automatizacion actual madura del repo pertenece a `Items`, no a `Spells`.

## Que requiere publicacion cliente

Con evidencia local disponible, Spell Builder productivo necesita al menos auditar estos frentes antes de cualquier publicacion:

1. Datos de niveles del spell en cliente.
2. Textos visibles del spell en i18n.
3. Recursos visuales relacionados si el cambio afecta icono o presentacion.
4. Backups y checksums previos del cliente real.
5. Validacion posterior sobre el cliente publicado.

## Estrategia recomendada

### Etapa 1 - Confirmacion de superficie cliente real

- Confirmar de forma controlada que artefactos del cliente actual son la fuente de verdad para spells en este fork:
  - `SpellLevels.d2o`
  - `i18n_es.d2i`
  - `i18n_en.d2i`
  - recursos visuales si el cambio los toca
- No portar ciegamente la publicacion SWF legacy al cliente actual.

### Etapa 2 - Paridad de backup

- Extender la estrategia de backup actual para incluir spells antes de cualquier publish.
- El minimo esperado para spells es:
  - `data/common/SpellLevels.d2o`
  - `data/i18n/i18n_es.d2i`
  - `data/i18n/i18n_en.d2i`
- Si el cambio toca recursos visuales, agregar tambien los paths relevantes del cliente.

### Etapa 3 - Paquete de staging de spells

- Replicar la disciplina de items:
  - staging package
  - validator
  - apply to sandbox
  - validate sandbox
  - apply to real client
  - validate real client
- El usuario final no debe operar `d2o`, `d2i`, `swf` o scripts manuales.

### Etapa 4 - Integracion con Spell Builder

- El flujo de producto final debe ocultar la capa tecnica y exponer solo:
  - validacion previa
  - resumen de cambios
  - confirmacion de publicacion
  - resultado de publicacion
  - siguiente paso de prueba en juego

## Riesgos

### Riesgos de cliente/publicacion

- Publicar levels sin alinear i18n deja cliente inconsistente.
- Publicar en cliente real sin backup de spells impide rollback seguro.
- Tocar recursos visuales sin saber si forman parte del cambio de spell puede contaminar alcance.

### Riesgos de arquitectura

- Portar literalmente la publicacion SWF legacy puede ser incorrecto para la estructura actual del cliente.
- Diseñar la publicacion sin primero cerrar el write de effects generaria un flujo roto: se podria publicar un nivel sin poder guardar el comportamiento real.

### Riesgos de UX

- Si la publicacion queda separada en scripts manuales, Spell Builder no llega a herramienta productiva final.

## Decisiones recomendadas

1. Tratar la publicacion cliente de spells como subproblema propio, no como apendice del editor.
2. Reutilizar el patron operativo de `ClientItemPublicationPipeline`, no su contenido tecnico de items.
3. No prometer `publicar` en UI hasta tener backup, staging y validacion real de spells.
4. Mantener la publicacion desacoplada del motor de combate.

## Que NO implementar todavia

- Publicacion cliente real de spells
- Parches manuales sobre cliente fuera de una pipeline validable
- Reutilizacion directa del publish legacy SWF sin confirmar compatibilidad con el cliente actual

## Nota obligatoria de idioma

La UI Angular final de Spell Builder debe quedar `100% en español`, incluyendo todos los pasos visibles de validacion, confirmacion, publicacion y post-publicacion.

## Proxima fase recomendada

`Fase 3 - Spell Client Staging Audit`

Alcance recomendado:

- confirmar artefactos cliente exactos de spells
- definir backup minimo de spells
- diseñar paquete de staging y validator sin aplicar publish real
