# D2I append report (staging)

Date: `2026-06-04`  
Staging: `Infrastructure/staging-client/i18n-phase3b/`

## Resultado

| Campo | Valor |
| --- | --- |
| Verified | `true` |
| NameId (compartido ES/EN) | `63079` |
| DescriptionId (compartido ES/EN) | `63080` |
| Entradas ES antes / después | `62710` / `62712` |
| Entradas EN antes / después | `62710` / `62712` |
| Original `i18n_es.d2i` modificado | **No** (solo staging) |

## Textos insertados

| Idioma | Campo | Texto |
| --- | --- | --- |
| ES | Nombre | Dofus de los Hielos |
| ES | Descripción | Dofus de los Hielos creado para pruebas controladas del pipeline de publicación. |
| EN | Nombre | Ice Dofus |
| EN | Descripción | Ice Dofus created for controlled publication pipeline testing. |

## Resolución tras releer staging

| textId | ES | EN |
| ---: | --- | --- |
| 63079 | Dofus de los Hielos | Ice Dofus |
| 63080 | (descripción ES) | (descripción EN) |

## Paquete integrado (stage-item-publication)

Segunda ejecución generó paquete en `Infrastructure/staging-client/publication-phase3b/12617/` con ids `63081`/`63082` (append acumulativo en staging). Para reproducir ids fijos, usar staging i18n limpio antes de append.

Contenido del paquete:

```txt
Items.d2o
i18n_es.d2i
i18n_en.d2i
publication-package-manifest.json
```

Item `12617` en D2O staging apunta a `nameId` / `descriptionId` del manifiesto.
