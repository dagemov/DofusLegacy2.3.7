using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemPreviewStateResolver
{
    ItemPreviewStateDto Resolve(int? itemId, int? iconId);
}
