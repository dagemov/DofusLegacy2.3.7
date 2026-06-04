# Client Identity Item Check Report

Generated: `2026-06-03 21:02:12 UTC`

## Inputs

- Repo: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7`
- Items.d2o: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\data\common\Items.d2o`
- ItemTypes.d2o: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\data\common\ItemTypes.d2o`
- ItemSets.d2o: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\data\common\ItemSets.d2o`
- Appearances.d2o: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\data\common\Appearances.d2o`
- i18n_es.d2i: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\data\i18n\i18n_es.d2i`
- i18n_en.d2i: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Client2.3.7\data\i18n\i18n_en.d2i`

## Summary

| ItemId | DB Name | Client | Statuses | Preview |
| --- | --- | --- | --- | --- |
| `7754` | Dofus Ocre | KNOWN | SAFE_EXISTING_TEMPLATE, CLIENT_KNOWN, ICON_PREVIEW_MISSING | missing |
| `12616` | ADMIN TEST | UNKNOWN | CLIENT_UNKNOWN, NEEDS_CLIENT_PATCH, ICON_PREVIEW_FOUND, APPEARANCE_UNKNOWN | 1003.png |
| `12617` | Dofus Tester | UNKNOWN | CLIENT_UNKNOWN, NEEDS_CLIENT_PATCH, ICON_PREVIEW_MISSING | missing |
| `39` | Petite Amulette du Hibou | KNOWN | SAFE_EXISTING_TEMPLATE, CLIENT_KNOWN, ICON_PREVIEW_FOUND | 1001.png |

## Detailed results

### Item `7754`

- DB Name: `Dofus Ocre`
- Client known: `True`
- Primary status: `SAFE_EXISTING_TEMPLATE`
- Statuses: `SAFE_EXISTING_TEMPLATE, CLIENT_KNOWN, ICON_PREVIEW_MISSING`
- Warnings: `No hay preview curado por item ni por icono.`
- Recommended action: `Seguir con QA runtime; el template ya existe en cliente.`
- Preview path: `(missing)`
- DB DescriptionId / Client DescriptionId: `40905 / 40905`
- Client NameId: `40904`
- DB Description ES: `Terrakurial, el dragón de la tierra, es el creador del Dofus Ocre. Este Dofus encierra grandes poderes por lo cual no debe dejarse entre las manos de cualquiera... ¡y tampoco entre los pies!`
- DB Description EN: `Laid by Terrakourial the Earth Dragon, this Dofus contains considerable powers and shouldn't be given to just anyone.`
- Client Name ES: `Dofus Ocre`
- Client Name EN: `Ochre Dofus`
- DB TypeId / Client TypeId: `23 / 23`
- Client Type ES / EN: `Dofus` / `Dofus`
- DB SetId / Client SetId: `(missing) / (missing)`
- Client Set ES / EN: `(missing)` / `(missing)`
- DB IconId / Client IconId: `23012 / 23012`
- DB AppearanceId / Client AppearanceId: `(missing) / (missing)`
- Appearance known: `(n/a)`

### Item `12616`

- DB Name: `ADMIN TEST`
- Client known: `False`
- Primary status: `CLIENT_UNKNOWN`
- Statuses: `CLIENT_UNKNOWN, NEEDS_CLIENT_PATCH, ICON_PREVIEW_FOUND, APPEARANCE_UNKNOWN`
- Warnings: `AppearanceId > 0 no existe en Appearances.d2o.`
- Recommended action: `Publicar el template 12616 en Items.d2o y alinear i18n antes de declararlo visible.`
- Preview path: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Angular-tools\Admin\RollblackLegacy.Admin.Angular\src\assets\item-previews\by-icon\1003.png`
- DB DescriptionId / Client DescriptionId: `50090 / (missing)`
- Client NameId: `(missing)`
- DB Description ES: `Lo esencial está en el lago helado`
- DB Description EN: `The Frozen Lake`
- Client Name ES: `(missing)`
- Client Name EN: `(missing)`
- DB TypeId / Client TypeId: `1 / (missing)`
- Client Type ES / EN: `(missing)` / `(missing)`
- DB SetId / Client SetId: `(missing) / (missing)`
- Client Set ES / EN: `(missing)` / `(missing)`
- DB IconId / Client IconId: `1003 / (missing)`
- DB AppearanceId / Client AppearanceId: `1004 / (missing)`
- Appearance known: `False`

### Item `12617`

- DB Name: `Dofus Tester`
- Client known: `False`
- Primary status: `CLIENT_UNKNOWN`
- Statuses: `CLIENT_UNKNOWN, NEEDS_CLIENT_PATCH, ICON_PREVIEW_MISSING`
- Warnings: `No hay preview curado por item ni por icono.`
- Recommended action: `Publicar el template 12617 en Items.d2o y alinear i18n antes de declararlo visible.`
- Preview path: `(missing)`
- DB DescriptionId / Client DescriptionId: `50091 / (missing)`
- Client NameId: `(missing)`
- DB Description ES: `El camino de la aventura`
- DB Description EN: `Follow your path`
- Client Name ES: `(missing)`
- Client Name EN: `(missing)`
- DB TypeId / Client TypeId: `23 / (missing)`
- Client Type ES / EN: `(missing)` / `(missing)`
- DB SetId / Client SetId: `(missing) / (missing)`
- Client Set ES / EN: `(missing)` / `(missing)`
- DB IconId / Client IconId: `23012 / (missing)`
- DB AppearanceId / Client AppearanceId: `(missing) / (missing)`
- Appearance known: `(n/a)`

### Item `39`

- DB Name: `Petite Amulette du Hibou`
- Client known: `True`
- Primary status: `SAFE_EXISTING_TEMPLATE`
- Statuses: `SAFE_EXISTING_TEMPLATE, CLIENT_KNOWN, ICON_PREVIEW_FOUND`
- Warnings: `(missing)`
- Recommended action: `Seguir con QA runtime; el template ya existe en cliente.`
- Preview path: `C:\Users\Hombr\source\repos\DofusLegacy2.3.7\Angular-tools\Admin\RollblackLegacy.Admin.Angular\src\assets\item-previews\by-icon\1001.png`
- DB DescriptionId / Client DescriptionId: `43649 / 43649`
- Client NameId: `43648`
- DB Description ES: `Este amuleto aumenta la inteligencia de su portador.`
- DB Description EN: `This amulet increases the wearer's intelligence.`
- Client Name ES: `Pequeño Amuleto del Búho`
- Client Name EN: `Small Owl Amulet`
- DB TypeId / Client TypeId: `1 / 1`
- Client Type ES / EN: `Amuleto` / `Amulet`
- DB SetId / Client SetId: `(missing) / (missing)`
- Client Set ES / EN: `(missing)` / `(missing)`
- DB IconId / Client IconId: `1001 / 1001`
- DB AppearanceId / Client AppearanceId: `(missing) / (missing)`
- Appearance known: `(n/a)`

## Interpretation

- `CLIENT_KNOWN`: el `ItemId` existe en `Items.d2o`.
- `CLIENT_UNKNOWN`: el `ItemId` no existe en `Items.d2o`.
- `SAFE_EXISTING_TEMPLATE`: el cliente ya conoce el template actual.
- `NEEDS_CLIENT_PATCH`: hace falta publicar template cliente o alinear metadata.
- `I18N_MISSING_ES` / `I18N_MISSING_EN`: `DescriptionId` DB no resolvio en ese idioma.
- `ICON_MISSING`: el item no trae `IconId` usable en DB.
- `APPEARANCE_UNKNOWN`: `AppearanceId` > 0, pero no existe en `Appearances.d2o`.
- `CLIENT_DATA_UNAVAILABLE`: la tool no pudo leer los metadata del cliente desde este entorno.
