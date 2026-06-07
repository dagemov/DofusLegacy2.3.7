using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemSetBonusEffectWriteDto(
    int EffectId,
    int Value,
    int? DiceNum,
    int? DiceSide,
    string Format);

public sealed record ItemSetBonusTierWriteDto(
    [Range(2, int.MaxValue)] int PieceCount,
    IReadOnlyList<ItemSetBonusEffectWriteDto> Effects);

public sealed class ItemSetCreateRequest
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Level { get; set; }

    public IReadOnlyList<int> ItemIds { get; set; } = [];

    public IReadOnlyList<ItemSetBonusTierWriteDto> BonusTiers { get; set; } = [];
}

public sealed class ItemSetUpdateRequest
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Level { get; set; }

    public IReadOnlyList<int> ItemIds { get; set; } = [];

    public IReadOnlyList<ItemSetBonusTierWriteDto> BonusTiers { get; set; } = [];
}

public sealed record ItemSetWriteResultDto(int SetId, string Message);
