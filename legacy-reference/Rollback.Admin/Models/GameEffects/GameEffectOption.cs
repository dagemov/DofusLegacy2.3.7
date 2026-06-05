using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.GameEffects;

public sealed class GameEffectOption
{
    public EffectId EffectId { get; set; }

    public string Label { get; set; } = string.Empty;

    public int SortPriority { get; set; }

    public EffectEditorKind SuggestedKind { get; set; } = EffectEditorKind.Integer;

    public string GroupLabel { get; set; } = string.Empty;
}
