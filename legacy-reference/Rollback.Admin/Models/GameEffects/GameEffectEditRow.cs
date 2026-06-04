using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.GameEffects;

public sealed class GameEffectEditRow
{
    public Guid RowId { get; set; } = Guid.NewGuid();

    public EffectId EffectId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public EffectEditorKind Kind { get; set; } = EffectEditorKind.Integer;

    public uint Random { get; set; }

    public short Duration { get; set; }

    public SpellTargetType TargetType { get; set; }

    public SpellShape Shape { get; set; }

    public byte ZoneSize { get; set; }

    public short Value { get; set; }

    public short MinValue { get; set; }

    public short MaxValue { get; set; }

    public string TextValue { get; set; } = string.Empty;

    public short DurationDays { get; set; }

    public short DurationHours { get; set; }

    public short DurationMinutes { get; set; }

    public DateTime? DateValue { get; set; }

    public int MountId { get; set; }

    public double MountExpirationDate { get; set; }

    public short MountModelId { get; set; }
}
