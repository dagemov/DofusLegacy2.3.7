using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemClientPublicationInspector
{
    Task<ItemClientPublicationAuditResult> InspectAsync(
        int itemId,
        int typeId,
        CancellationToken cancellationToken = default);
}
