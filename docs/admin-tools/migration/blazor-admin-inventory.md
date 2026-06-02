# Legacy Blazor Admin Inventory

## Scope

Source analyzed:

- `DofusBeta-2.0/Dofus-2/Rollback/Rollback.Web`
- supporting logic in `DofusBeta-2.0/Dofus-2/Rollback/Rollback.Admin`

This inventory is for migration planning only. It is not a copy plan.

## Structural findings

- `Rollback.Web` is a `net6.0` Blazor Server app with project references to `Rollback.Admin`, `Rollback.Accounts`, and `Rollback.Protocol`.
- The real business logic is concentrated in `Rollback.Admin/Services/*`.
- The web project adds:
  - cookie auth
  - admin pages
  - manual asset upload
  - basic auth/account controllers
- `AdminBootstrapService` creates and evolves admin-only support tables such as:
  - `admin_entity_asset_overrides`
  - `admin_entity_client_metadata`
  - `admin_entity_text_overrides`
- The stack is business-useful but tightly coupled to in-process services and direct DB/client file access.

## Module inventory

| Module | Screen / Component | Service | Model / DTO | Assets used | DB / tables touched | Estado | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Admin shell and account portal | `Pages/Login.razor`, `Pages/Register.razor`, `Pages/Account.razor`, `Pages/Admin/Users.razor`, `Pages/Admin/Characters.razor`, `Controllers/AuthController.cs`, `Controllers/AdminAccountsController.cs` | `IAccountPortalService`, `CharacterAdminService` | `AdminCharacterListItem`, special-spell grant models, account summaries | none | `characters`, `experiences`, plus account tables via `Rollback.Accounts` | functional | `REFERENCE_ONLY` |
| Items builder | `Pages/Admin/Items.razor`, `Components/Admin/ItemCatalogPicker.razor`, `Components/Admin/GameAssetPreview.razor`, `Components/Admin/EffectListEditor.razor` | `ItemAdminService`, `ItemAppearanceCatalogService`, `GameEffectEditorService` | `ItemEditModel`, `ItemListItem`, `ItemDiagnosticReport`, `AdminSaveResult` | item previews, manual preview URLs | `items_templates`, `items_sets`, `admin_entity_text_overrides`, `admin_entity_asset_overrides` | functional and high-value | `PORT_TO_ANGULAR` |
| Item PNG and client publish pipeline | mainly item page + shared preview/upload components | `AdminAssetUploadService`, `GameAssetPreviewService`, `ItemClientPublishService`, `ClientDataPathResolver` | `GameAssetPreviewModel`, `AdminEntityAssetOverride`, `AdminEntityClientMetadata`, `ItemClientPublishResult` | `wwwroot/admin-assets/items`, client `items/bitmap/*.png`, `Items*.swf`, `i18n*.swf`, FFDec | `admin_entity_asset_overrides`, `admin_entity_client_metadata` | functional but client-coupled | `REUSE_ASSET_PIPELINE` |
| Sets editor | `Pages/Admin/Sets.razor` | `SetAdminService`, `SetClientPublishService`, `GameEffectEditorService` | `ItemSetEditModel`, `ItemSetListItem` | `ItemSets0.swf`, `i18n*.swf`, item previews | `items_sets`, `items_templates` | functional | `PORT_TO_ANGULAR` |
| Spells editor | `Pages/Admin/Spells.razor`, `Components/Admin/EffectListEditor.razor`, `Components/Admin/ShortIdCollectionEditor.razor` | `SpellAdminService`, `SpellAdminSchemaService`, `SpellEffectCatalogService`, `GameEffectEditorService` | `SpellEditModel`, `SpellLevelEditModel`, `SpellListItem`, `GameEffectEditRow` | runtime effect blobs, client identity references | `spells_templates`, `spells_levels`, `breeds_spells`, `monsters_spells`, `characters_spells` | functional and complex | `PORT_TO_ANGULAR` |
| Spell publish and glyph/trap sync | same spell page, publish action in service layer | `SpellPublishOrchestrator`, `SpellClientPublishService`, `SpellAdminService` | `SpellClientPublishResult`, `SpellReferenceSummary` | `Spells*.swf`, `SpellLevels*.swf`, `i18n*.swf`, FFDec | `admin_spell_trigger_payload_sync`, `spells_templates`, `spells_levels` | functional and high-risk | `REUSE_LOGIC` |
| Monster families catalog | `Pages/Admin/MonsterFamilies.razor` | `MonsterFamilyCatalogService` | `MonsterFamilyCatalogItem` | none | `monsters_templates`, `monsters_grades` | functional lookup slice | `REFERENCE_ONLY` |
| Monster builder | `Pages/Admin/Monsters.razor` | `MonsterAdminService`, `MonsterCatalogService` | `MonsterEditModel`, `MonsterGradeAdminModel`, `MonsterListItem` | manual preview paths for monsters, shared preview component | `monsters_templates`, `monsters_grades`, `monsters_spells`, `monsters_drops`, `admin_entity_asset_overrides`, `admin_entity_text_overrides` | functional but visual preview incomplete | `REUSE_LOGIC` |
| Monster group builder | `Pages/Admin/MonsterGroups.razor` | `MonsterGroupAdminService` | `MonsterGroupEditModel`, `MonsterGroupAssignmentAdminModel`, `MonsterGroupSyncResult` | none | `admin_monster_groups`, `admin_monster_group_entries`, `admin_monster_group_assignments`, `monsters_spawns`, `world_maps` | functional | `REFERENCE_ONLY` |
| Map spawn editor | `Pages/Admin/Spawns.razor` | `MapSpawnAdminService` | `MapSpawnOverview`, `MonsterSpawnAdminModel` | none | `world_maps`, `monsters_spawns`, `monsters_grades` | functional | `REFERENCE_ONLY` |
| NPC editor | `Pages/Admin/Npcs.razor` | `NpcAdminService`, `NpcSkinCatalogService` | `NpcEditModel`, `NpcListItem`, `NpcSkinOption` | NPC skin references, manual preview base | `npcs_templates`, `npcs_spawns`, `npcs_actions` | functional | `REFERENCE_ONLY` |
| NPC vendors | `Pages/Admin/Vendors.razor` | `NpcVendorAdminService`, `NpcVendorInventorySyncService`, `ItemAdminService` | `NpcVendorEditModel`, `NpcVendorItemEntry`, `NpcVendorAssignmentResult` | item previews, vendor item composition | `npcs_items`, `npcs_actions`, `npcs_templates`, `items_templates`, `items_sets` | functional | `REFERENCE_ONLY` |
| Admin metadata and bootstrap layer | no direct page, used across items/spells/monsters/NPCs | `AdminBootstrapService`, `AdminEntityAssetOverrideService`, `AdminEntityClientMetadataService`, `AdminEntityTextOverrideService` | metadata and override models | none directly | `admin_entity_asset_overrides`, `admin_entity_client_metadata`, `admin_entity_text_overrides` | functional shared foundation | `REUSE_LOGIC` |

## High-value logic to preserve conceptually

- `ItemAdminService` exposes a mature item workflow:
  - CRUD
  - diagnostics
  - manual asset save/clear
  - typed lookups
  - client publish hooks
- `SpellAdminService` already separates:
  - identity
  - runtime levels
  - effect serialization
  - client publish orchestration
- `SpellAdminSchemaService` is a strong candidate for backend reuse because it cleanly maps level records to editable DTOs.
- `SpellPublishOrchestrator` already treats runtime save and client publish as separate concerns.
- `AdminAssetUploadService` and `GameAssetPreviewService` provide a reusable concept for manual preview overrides and curated PNG validation.

## Risks and caveats

- Monster and NPC previews are not equivalent to a real in-client look renderer yet.
- Item and spell client publish flows depend on:
  - curated local client directories
  - FFDec availability
  - safe backup discipline
- The Blazor stack is in-process and should not be copied as architecture.
- Admin support tables are useful, but their schema must be re-reviewed against the `sunshine` target before reuse.

## Migration recommendation

Use this legacy Blazor stack for:

- business rules
- DTO shapes worth retaining
- warning logic
- preview/upload concepts
- client publish concepts

Do not use it for:

- final UI stack
- direct architectural copy
- uncontrolled SQL reuse
