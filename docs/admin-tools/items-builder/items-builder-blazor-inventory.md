# Items Builder Legacy Blazor Inventory

## Purpose

This inventory isolates the useful legacy `Items Builder` pieces from the older Blazor admin stack so the future Angular + API rewrite can reuse the right ideas without copying the old project structure.

## Inventory

| Module | Screen/Component | Service | Model/DTO | Assets used | DB/tables touched | State | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Item list and search shell | `Rollback.Web/Pages/Admin/Items.razor` | `AdminItemCatalogService` | `ItemListItem`, query/filter state in page model | optional item bitmap preview, manual preview URLs | legacy runtime item tables plus admin metadata lookups | validated in prior admin | `PORT_TO_ANGULAR` |
| Item detail editor | `Rollback.Web/Pages/Admin/Items.razor` | `ItemAdminService` | `ItemEditModel` | manual preview URL, resolved client bitmap preview | legacy runtime item row plus override/metadata support tables | validated in prior admin | `PORT_TO_ANGULAR` |
| Item save orchestration | none, server-side service | `ItemAdminService.SaveAsync` | `AdminSaveResult`, `ItemEditModel` | none directly | runtime item row plus text/client metadata override writes | validated | `REUSE_LOGIC` |
| Manual asset upload flow | `Items.razor` upload block | `AdminAssetUploadService`, `ItemAdminService.SaveManualAssetAsync` | file upload payload plus `ManualAssetRelativePath` | `.png`, `.jpg`, `.jpeg`, `.webp`, stored under `wwwroot/admin-assets` | admin asset override support table plus file storage | validated but repo-polluting in old shape | `REUSE_ASSET_PIPELINE` |
| Diagnostic and audit panel | `Items.razor` audit sections | `ItemIdentityDiagnosticService`, `ItemAuditEvaluator`, `ItemAdminService.GetDiagnosticAsync` | `ItemDiagnosticReport`, audit/warning payloads | client bitmap reference, manual asset presence | runtime item row plus admin metadata joins | validated | `REUSE_LOGIC` |
| Identity correction preview/apply | `Items.razor` correction panel | `ItemIdentityCorrectionService`, `ItemAdminService.ApplyIdentityCorrectionAsync` | `ItemIdentityCorrectionPlan` | none directly | runtime item row plus override/metadata tables | validated but needs Sunshine-specific review | `REUSE_LOGIC` |
| Appearance and icon resolution | not UI-only | `ItemAppearanceResolverService` | resolution result models and client metadata records | hashes of local `items/bitmap/*.png`, manual preview PNG | runtime item row plus legacy admin client metadata tables | validated but sensitive to local client packs | `REUSE_LOGIC` |
| Preview lookup experience | `Items.razor` bitmap lookup block | `GameAssetPreviewService` | preview request/result models | local `client/app/content/gfx/items/bitmap/*.png`, manual preview URL | no required runtime writes | validated as operator aid | `PORT_TO_ANGULAR` |
| Client publish workflow | `Items.razor` publish button | `ItemClientPublishService` | `ItemClientPublishResult` | local client `Items*.swf`, `i18n*.swf`, `items/bitmap/*.png`, manual PNG | client files plus admin metadata | validated in old emulator, not safe for first rewrite step | `HIGH_RISK` |
| Runtime inspection helper | `Tools/ItemRuntimeInspector` | console helper | console-only output models | local client bitmap and runtime metadata | runtime DB reads and local file inspection | useful for reference and troubleshooting | `REFERENCE_ONLY` |
| Legacy SQL support patching | ad hoc SQL scripts | `SQL/patch_item_appearance_autofix_phase1.sql` | SQL only | none | legacy admin support tables | local fix-up artifact, not a portable feature | `IGNORE` |

## What to carry forward first

Carry into the new stack first:

- search and filter ergonomics from the Blazor page
- the separation between runtime values, client metadata, and manual preview assets
- diagnostic and warning surfacing
- identity correction as a previewable workflow, not a silent mutation
- preview lookup that proves whether a bitmap exists locally

## What to defer

Defer until the new API and asset policy are stable:

- direct client publish execution
- any automatic bitmap id allocation logic that writes to client packs
- reuse of legacy admin support tables without Sunshine-focused ownership review

## Notes for the rewrite

- `ItemEditModel` is richer than a basic CRUD DTO and should be split into read models, write requests, and diagnostic payloads in the new API.
- The future Angular UI should preserve the old operator mental model: runtime item fields, client-facing metadata, manual asset state, and diagnostics must be visible at the same time.
- The old upload service accepted multiple image formats, but the publish workflow ultimately required a true `.png`. The new design should keep that distinction explicit.
