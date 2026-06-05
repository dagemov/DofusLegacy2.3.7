namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellPagedResultDto<TItem>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<TItem> Items);
