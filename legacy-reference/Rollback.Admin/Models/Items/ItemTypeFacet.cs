using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed record ItemTypeFacet(
    string Key,
    string Label,
    IReadOnlyCollection<ItemType> Types);
