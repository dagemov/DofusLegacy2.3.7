namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminPagedItemsReadModel(
    int TotalCount,
    IReadOnlyList<AdminItemListReadModel> Items);
