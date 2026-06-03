namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemEffectsAdminRepository
{
    Task<ItemEffectsRow?> GetEffectsRowAsync(int itemId, CancellationToken cancellationToken = default);

    Task<bool> UpdateEffectsHexAsync(
        int itemId,
        string effectsHex,
        CancellationToken cancellationToken = default);
}

public sealed record ItemEffectsRow(int ItemId, uint TypeId, string? Effects);
