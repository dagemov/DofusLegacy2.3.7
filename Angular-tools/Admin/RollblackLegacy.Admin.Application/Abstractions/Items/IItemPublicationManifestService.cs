using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemPublicationManifestService
{
    Task<ItemPublicationManifestDto> GetManifestAsync(int itemId, CancellationToken cancellationToken = default);
}
