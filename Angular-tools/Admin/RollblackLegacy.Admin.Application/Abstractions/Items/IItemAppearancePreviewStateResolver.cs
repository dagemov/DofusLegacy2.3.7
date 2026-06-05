using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemAppearancePreviewStateResolver
{
    ItemAppearancePreviewStateDto Resolve(
        int appearanceId,
        bool? appearanceKnown,
        string? appearancesD2oPath = null);
}
