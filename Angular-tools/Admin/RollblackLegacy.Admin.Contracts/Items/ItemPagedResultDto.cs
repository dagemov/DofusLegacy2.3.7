namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemPagedResultDto<TItem>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<TItem> Items);
