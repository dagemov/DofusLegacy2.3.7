using Rollback.Admin.Models.GameEffects;
using Rollback.World.CustomEnums;
using Rollback.World.Database.Items;
using Rollback.World.Game.Effects;
using Rollback.World.Game.Effects.Types;

namespace Rollback.Admin.Services;

public sealed class GameEffectEditorService
{
    private readonly GameEffectDisplayService _displayService;

    public GameEffectEditorService(GameEffectDisplayService displayService) =>
        _displayService = displayService;

    public IReadOnlyList<GameEffectOption> GetOptions() =>
        _displayService.GetOptions();

    public List<GameEffectEditRow> Deserialize(byte[]? binaryEffects)
    {
        if (binaryEffects is null || binaryEffects.Length == 0)
            return new List<GameEffectEditRow>();

        try
        {
            return EffectManager.DeserializeEffects(binaryEffects)
                .Select(MapToRow)
                .ToList();
        }
        catch
        {
            return new List<GameEffectEditRow>();
        }
    }

    public byte[] Serialize(IEnumerable<GameEffectEditRow> rows)
    {
        var effects = rows
            .Where(x => x.EffectId != 0)
            .Select(MapToEffect)
            .ToArray();

        return EffectManager.SerializeEffects(effects);
    }

    public List<GameEffectTierEditModel> DeserializeSet(byte[]? binaryEffects)
    {
        if (binaryEffects is null || binaryEffects.Length == 0)
            return new List<GameEffectTierEditModel>();

        try
        {
            return EffectManager.DeserializeSetBonusTiers(binaryEffects)
                .Select(tier => new GameEffectTierEditModel
                {
                    RequiredItemCount = tier.RequiredPieces,
                    Effects = tier.Effects.Select(MapToRow).ToList(),
                })
                .OrderBy(tier => tier.RequiredItemCount)
                .ToList();
        }
        catch
        {
            return new List<GameEffectTierEditModel>();
        }
    }

    public byte[] SerializeSet(IEnumerable<GameEffectTierEditModel> tiers)
    {
        var normalized = tiers
            .OrderBy(x => x.RequiredItemCount)
            .Select(tier => new ItemSetBonusTier(
                tier.RequiredItemCount,
                tier.Effects
                    .Where(effect => effect.EffectId != 0)
                    .Select(MapToEffect)
                    .ToArray()))
            .ToArray();

        return EffectManager.SerializeSetBonusTiers(normalized);
    }

    public GameEffectEditRow CreateDefaultRow() =>
        CreateDefaultRow(EffectId.EffectAddVitality);

    public GameEffectEditRow CreateDefaultRow(EffectId effectId)
    {
        var option = _displayService.GetOptions().FirstOrDefault(x => x.EffectId == effectId);
        return new GameEffectEditRow
        {
            EffectId = effectId,
            DisplayName = _displayService.GetDisplayName(effectId),
            Kind = option?.SuggestedKind ?? EffectEditorKind.Integer,
            Shape = SpellShape.empty,
        };
    }

    public GameEffectTierEditModel CreateDefaultTier(int requiredItemCount) =>
        new()
        {
            RequiredItemCount = requiredItemCount,
            Effects = new List<GameEffectEditRow>(),
        };

    public void UpdateDisplay(GameEffectEditRow row)
    {
        row.DisplayName = _displayService.GetDisplayName(row.EffectId);
    }

    private GameEffectEditRow MapToRow(EffectBase effect)
    {
        var row = new GameEffectEditRow
        {
            EffectId = effect.Id,
            DisplayName = _displayService.GetDisplayName(effect.Id),
            Random = effect.Random,
            Duration = effect.Duration,
            TargetType = effect.TargetType,
            Shape = effect.Shape,
            ZoneSize = effect.ZoneSize,
        };

        switch (effect)
        {
            case EffectDice dice:
                row.Kind = EffectEditorKind.Dice;
                row.Value = dice.Value;
                row.MinValue = dice.DiceNum;
                row.MaxValue = dice.DiceFace;
                break;

            case EffectInteger integer:
                row.Kind = EffectEditorKind.Integer;
                row.Value = integer.Value;
                break;

            case EffectString effectString:
                row.Kind = EffectEditorKind.String;
                row.TextValue = effectString.Value;
                break;

            case EffectDuration duration:
                row.Kind = EffectEditorKind.Duration;
                row.DurationDays = duration.Days;
                row.DurationHours = duration.Hours;
                row.DurationMinutes = duration.Minutes;
                break;

            case EffectDate date:
                row.Kind = EffectEditorKind.Date;
                row.DateValue = date.Date;
                break;

            case EffectMount mount:
                row.Kind = EffectEditorKind.Mount;
                row.MountId = mount.MountId;
                row.MountExpirationDate = mount.ExpirationDate;
                row.MountModelId = mount.ModelId;
                break;

            default:
                row.Kind = EffectEditorKind.Base;
                break;
        }

        return row;
    }

    private static EffectBase MapToEffect(GameEffectEditRow row)
    {
        EffectBase effect = row.Kind switch
        {
            EffectEditorKind.Dice => new EffectDice
            {
                Value = row.Value,
                DiceNum = row.MinValue,
                DiceFace = row.MaxValue,
            },
            EffectEditorKind.String => new EffectString
            {
                Value = row.TextValue ?? string.Empty,
            },
            EffectEditorKind.Duration => new EffectDuration
            {
                Days = row.DurationDays,
                Hours = row.DurationHours,
                Minutes = row.DurationMinutes,
            },
            EffectEditorKind.Date => new EffectDate(row.EffectId, row.DateValue ?? DateTime.UtcNow),
            EffectEditorKind.Mount => new EffectMount
            {
                MountId = row.MountId,
                ExpirationDate = row.MountExpirationDate,
                ModelId = row.MountModelId,
            },
            EffectEditorKind.Base => new EffectBase(),
            _ => new EffectInteger
            {
                Value = row.Value,
            },
        };

        effect.Id = row.EffectId;
        effect.Random = row.Random;
        effect.Duration = row.Duration;
        effect.TargetType = row.TargetType;
        effect.Shape = row.Shape;
        effect.ZoneSize = row.ZoneSize;
        return effect;
    }
}
