namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminPagedSpellsReadModel(
    int TotalCount,
    IReadOnlyList<AdminSpellCatalogReadModel> Items);
