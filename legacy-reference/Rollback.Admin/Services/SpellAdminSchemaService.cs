using Rollback.Admin.Models.Spells;
using Rollback.World.Database.Spells;

namespace Rollback.Admin.Services;

public sealed class SpellAdminSchemaService
{
    private readonly GameEffectEditorService _effectEditorService;

    public SpellAdminSchemaService(GameEffectEditorService effectEditorService) =>
        _effectEditorService = effectEditorService;

    public SpellLevelEditModel MapLevel(SpellLevelRecord record, int levelNumber) =>
        new()
        {
            LevelNumber = levelNumber,
            Id = record.Id,
            APCost = record.APCost,
            MinRange = record.MinRange,
            MaxRange = record.MaxRange,
            CastInLine = record.CastInLine,
            CastTestLOS = record.CastTestLOS,
            NeedFreeCell = record.NeedFreeCell,
            RangeCanBeBoosted = record.RangeCanBeBoosted,
            CriticalFailureEndsTurn = record.CriticalFailureEndsTurn,
            CriticalHitProbability = record.CriticalHitProbability,
            CriticalFailureProbability = record.CriticalFailureProbability,
            MaxCastPerTurn = record.MaxCastPerTurn,
            MaxCastPerTarget = record.MaxCastPerTarget,
            MinCastInterval = record.MinCastInterval,
            MinPlayerLevel = record.MinPlayerLevel,
            Effects = _effectEditorService.Deserialize(record.BinaryEffects),
            CriticalEffects = _effectEditorService.Deserialize(record.BinaryCriticalEffects),
            StatesRequired = record.StatesRequired.OrderBy(x => x).ToList(),
            StatesForbidden = record.StatesForbidden.OrderBy(x => x).ToList(),
        };

    public SpellLevelEditModel CreateDefaultLevel(int levelNumber) =>
        new()
        {
            LevelNumber = levelNumber,
            APCost = 3,
            MinRange = 1,
            MaxRange = 1,
            CastTestLOS = true,
            NeedFreeCell = false,
            RangeCanBeBoosted = true,
            MaxCastPerTurn = 1,
            MaxCastPerTarget = 0,
            MinCastInterval = 0,
            MinPlayerLevel = (byte)Math.Max(1, (levelNumber - 1) * 10),
            Effects = new(),
            CriticalEffects = new(),
            StatesRequired = new(),
            StatesForbidden = new(),
        };

    public SpellLevelEditModel CloneLevel(SpellLevelEditModel source, int levelNumber) =>
        new()
        {
            LevelNumber = levelNumber,
            APCost = source.APCost,
            MinRange = source.MinRange,
            MaxRange = source.MaxRange,
            CastInLine = source.CastInLine,
            CastTestLOS = source.CastTestLOS,
            NeedFreeCell = source.NeedFreeCell,
            RangeCanBeBoosted = source.RangeCanBeBoosted,
            CriticalFailureEndsTurn = source.CriticalFailureEndsTurn,
            CriticalHitProbability = source.CriticalHitProbability,
            CriticalFailureProbability = source.CriticalFailureProbability,
            MaxCastPerTurn = source.MaxCastPerTurn,
            MaxCastPerTarget = source.MaxCastPerTarget,
            MinCastInterval = source.MinCastInterval,
            MinPlayerLevel = source.MinPlayerLevel,
            Effects = source.Effects.Select(CloneEffect).ToList(),
            CriticalEffects = source.CriticalEffects.Select(CloneEffect).ToList(),
            StatesRequired = source.StatesRequired.ToList(),
            StatesForbidden = source.StatesForbidden.ToList(),
        };

    public void ApplyLevel(SpellLevelEditModel model, SpellLevelRecord record)
    {
        record.APCost = model.APCost;
        record.MinRange = model.MinRange;
        record.MaxRange = model.MaxRange;
        record.CastInLine = model.CastInLine;
        record.CastTestLOS = model.CastTestLOS;
        record.NeedFreeCell = model.NeedFreeCell;
        record.RangeCanBeBoosted = model.RangeCanBeBoosted;
        record.CriticalFailureEndsTurn = model.CriticalFailureEndsTurn;
        record.CriticalHitProbability = model.CriticalHitProbability;
        record.CriticalFailureProbability = model.CriticalFailureProbability;
        record.MaxCastPerTurn = model.MaxCastPerTurn;
        record.MaxCastPerTarget = model.MaxCastPerTarget;
        record.MinCastInterval = model.MinCastInterval;
        record.MinPlayerLevel = model.MinPlayerLevel;
        record.BinaryEffects = _effectEditorService.Serialize(model.Effects);
        record.BinaryCriticalEffects = _effectEditorService.Serialize(model.CriticalEffects);
        record.StatesRequiredCSV = string.Join(",", model.StatesRequired.Distinct().OrderBy(x => x));
        record.StatesForbiddenCSV = string.Join(",", model.StatesForbidden.Distinct().OrderBy(x => x));
    }

    private static Models.GameEffects.GameEffectEditRow CloneEffect(Models.GameEffects.GameEffectEditRow effect) =>
        new()
        {
            EffectId = effect.EffectId,
            DisplayName = effect.DisplayName,
            Kind = effect.Kind,
            Random = effect.Random,
            Duration = effect.Duration,
            TargetType = effect.TargetType,
            Shape = effect.Shape,
            ZoneSize = effect.ZoneSize,
            Value = effect.Value,
            MinValue = effect.MinValue,
            MaxValue = effect.MaxValue,
            TextValue = effect.TextValue,
            DurationDays = effect.DurationDays,
            DurationHours = effect.DurationHours,
            DurationMinutes = effect.DurationMinutes,
            DateValue = effect.DateValue,
            MountId = effect.MountId,
            MountExpirationDate = effect.MountExpirationDate,
            MountModelId = effect.MountModelId,
        };
}
