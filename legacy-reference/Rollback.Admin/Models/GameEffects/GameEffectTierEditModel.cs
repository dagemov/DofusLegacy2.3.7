namespace Rollback.Admin.Models.GameEffects;

public sealed class GameEffectTierEditModel
{
    public Guid TierId { get; set; } = Guid.NewGuid();

    public int RequiredItemCount { get; set; } = 2;

    public List<GameEffectEditRow> Effects { get; set; } = new();
}
