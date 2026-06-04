using System.Text.RegularExpressions;
using Rollback.Admin.Models.GameEffects;
using Rollback.World.CustomEnums;
using Rollback.World.Game.Effects;

namespace Rollback.Admin.Services;

public sealed class GameEffectDisplayService
{
    private static readonly Regex SplitPascalCaseRegex = new("([a-z])([A-Z0-9])", RegexOptions.Compiled);

    private readonly Dictionary<EffectId, (string Label, int Priority)> _labels = new()
    {
        [EffectId.EffectTeleport] = ("Teleportar", -12),
        [EffectId.EffectPushBack] = ("Empujar", -11),
        [EffectId.EffectPullForward] = ("Atraer", -10),
        [EffectId.EffectSwitchPosition] = ("Intercambiar posicion", -9),
        [EffectId.EffectCarry] = ("Cargar objetivo", -8),
        [EffectId.EffectThrow] = ("Lanzar objetivo", -7),
        [EffectId.EffectStealMP77] = ("Robar PM", -6),
        [EffectId.EffectHealHP81] = ("Curar", -5),
        [EffectId.EffectStealHPFix] = ("Robo de vida neutro fijo", -4),
        [EffectId.EffectStealAP84] = ("Robar PA", -3),
        [EffectId.EffectStealHPWater] = ("Robo de vida agua", -2),
        [EffectId.EffectStealHPEarth] = ("Robo de vida tierra", -1),
        [EffectId.EffectStealHPAir] = ("Robo de vida aire", 0),
        [EffectId.EffectStealHPFire] = ("Robo de vida fuego", 0),
        [EffectId.EffectStealHPNeutral] = ("Robo de vida neutro", 0),
        [EffectId.EffectAddAP111] = ("+ PA", 0),
        [EffectId.EffectSubAP] = ("- PA", 1),
        [EffectId.EffectLostAP] = ("Quitar PA", 1),
        [EffectId.EffectAddMP128] = ("+ PM", 2),
        [EffectId.EffectAddMP] = ("+ PM", 2),
        [EffectId.EffectSubMP] = ("- PM", 3),
        [EffectId.EffectLostMP] = ("Quitar PM", 3),
        [EffectId.EffectAddVitality] = ("+ Vitalidad", 4),
        [EffectId.EffectSubVitality] = ("- Vitalidad", 5),
        [EffectId.EffectAddHealth] = ("+ Vida", 6),
        [EffectId.EffectAddInitiative] = ("+ Iniciativa", 7),
        [EffectId.EffectSubInitiative] = ("- Iniciativa", 8),
        [EffectId.EffectAddStrength] = ("+ Fuerza", 9),
        [EffectId.EffectSubStrength] = ("- Fuerza", 10),
        [EffectId.EffectAddIntelligence] = ("+ Inteligencia", 11),
        [EffectId.EffectSubIntelligence] = ("- Inteligencia", 12),
        [EffectId.EffectAddChance] = ("+ Suerte", 13),
        [EffectId.EffectSubChance] = ("- Suerte", 14),
        [EffectId.EffectAddAgility] = ("+ Agilidad", 15),
        [EffectId.EffectSubAgility] = ("- Agilidad", 16),
        [EffectId.EffectAddWisdom] = ("+ Sabiduria", 17),
        [EffectId.EffectSubWisdom] = ("- Sabiduria", 18),
        [EffectId.EffectAddRange] = ("+ Alcance", 19),
        [EffectId.EffectSubRange] = ("- Alcance", 20),
        [EffectId.EffectAddCriticalHit] = ("+ Golpes criticos", 21),
        [EffectId.EffectSubCriticalHit] = ("- Golpes criticos", 22),
        [EffectId.EffectAddDamageBonus] = ("+ Danos", 23),
        [EffectId.EffectAddDamageBonus121] = ("+ Danos", 24),
        [EffectId.EffectSubDamageBonus] = ("- Danos", 25),
        [EffectId.EffectAddDamageBonusPercent] = ("+ % Danos", 26),
        [EffectId.EffectSubDamageBonusPercent] = ("- % Danos", 27),
        [EffectId.Effect701] = ("+ Potencia", 28),
        [EffectId.EffectAddHealBonus] = ("+ Curaciones", 29),
        [EffectId.EffectSubHealBonus] = ("- Curaciones", 30),
        [EffectId.EffectAddProspecting] = ("+ Prospeccion", 31),
        [EffectId.EffectSubProspecting] = ("- Prospeccion", 32),
        [EffectId.EffectAddSummonLimit] = ("+ Invocaciones", 33),
        [EffectId.EffectIncreaseWeight] = ("+ Pods", 34),
        [EffectId.EffectDecreaseWeight] = ("- Pods", 35),
        [EffectId.EffectAddEarthResistPercent] = ("+ % Resistencia Tierra", 36),
        [EffectId.EffectAddWaterResistPercent] = ("+ % Resistencia Agua", 37),
        [EffectId.EffectAddAirResistPercent] = ("+ % Resistencia Aire", 38),
        [EffectId.EffectAddFireResistPercent] = ("+ % Resistencia Fuego", 39),
        [EffectId.EffectAddNeutralResistPercent] = ("+ % Resistencia Neutro", 40),
        [EffectId.EffectSubEarthResistPercent] = ("- % Resistencia Tierra", 41),
        [EffectId.EffectSubWaterResistPercent] = ("- % Resistencia Agua", 42),
        [EffectId.EffectSubAirResistPercent] = ("- % Resistencia Aire", 43),
        [EffectId.EffectSubFireResistPercent] = ("- % Resistencia Fuego", 44),
        [EffectId.EffectSubNeutralResistPercent] = ("- % Resistencia Neutro", 45),
        [EffectId.EffectAddEarthElementReduction] = ("+ Resistencia Tierra fija", 46),
        [EffectId.EffectAddWaterElementReduction] = ("+ Resistencia Agua fija", 47),
        [EffectId.EffectAddAirElementReduction] = ("+ Resistencia Aire fija", 48),
        [EffectId.EffectAddFireElementReduction] = ("+ Resistencia Fuego fija", 49),
        [EffectId.EffectAddNeutralElementReduction] = ("+ Resistencia Neutro fija", 50),
        [EffectId.EffectSubEarthElementReduction] = ("- Resistencia Tierra fija", 51),
        [EffectId.EffectSubWaterElementReduction] = ("- Resistencia Agua fija", 52),
        [EffectId.EffectSubAirElementReduction] = ("- Resistencia Aire fija", 53),
        [EffectId.EffectSubFireElementReduction] = ("- Resistencia Fuego fija", 54),
        [EffectId.EffectSubNeutralElementReduction] = ("- Resistencia Neutro fija", 55),
        [EffectId.EffectAddLock] = ("+ Placaje", 56),
        [EffectId.EffectSubLock] = ("- Placaje", 57),
        [EffectId.EffectAddDodge] = ("+ Huida", 58),
        [EffectId.EffectSubDodge] = ("- Huida", 59),
        [EffectId.EffectDamageNeutral] = ("Danos neutro", 60),
        [EffectId.EffectDamageEarth] = ("Danos tierra", 61),
        [EffectId.EffectDamageFire] = ("Danos fuego", 62),
        [EffectId.EffectDamageWater] = ("Danos agua", 63),
        [EffectId.EffectDamageAir] = ("Danos aire", 64),
        [EffectId.EffectDamageCaster] = ("Dano al lanzador", 64),
        [EffectId.EffectFixedNeutralDamage] = ("Danos neutro fijo", 64),
        [EffectId.EffectLearnSpell] = ("Aprender hechizo", 65),
        [EffectId.EffectAddSpellPoints] = ("+ Puntos de hechizo", 66),
        [EffectId.EffectAddGlobalDamageReduction105] = ("+ Reduccion de danos", 67),
        [EffectId.EffectAddGlobalDamageReduction] = ("+ Reduccion de danos", 68),
        [EffectId.EffectReflectSpell] = ("Reflejar hechizo", 69),
        [EffectId.EffectAddDamageReflection] = ("Devolver danos", 70),
        [EffectId.EffectHealHP108] = ("Curar", 71),
        [EffectId.EffectHealHP143] = ("Curar", 71),
        [EffectId.EffectRegainAP] = ("Ganar PA", 72),
        [EffectId.EffectDispelMagicEffects] = ("Deshechizar", 73),
        [EffectId.EffectSkipTurn] = ("Hacer pasar turno", 74),
        [EffectId.EffectKill] = ("Matar", 75),
        [EffectId.Effect147] = ("Resucitar aliado", 76),
        [EffectId.EffectInvisibility] = ("Invisibilidad", 77),
        [EffectId.EffectAddDodgeAPProbability] = ("+ Esquiva PA", 78),
        [EffectId.EffectAddDodgeMPProbability] = ("+ Esquiva PM", 79),
        [EffectId.EffectSubDodgeAPProbability] = ("- Esquiva PA", 80),
        [EffectId.EffectSubDodgeMPProbability] = ("- Esquiva PM", 81),
        [EffectId.EffectDouble] = ("Invocar doble", 82),
        [EffectId.EffectSummon] = ("Invocar criatura", 83),
        [EffectId.EffectSummonStatic] = ("Invocacion estatica", 84),
        [EffectId.EffectRevealsInvisible] = ("Revelar invisibles", 85),
        [EffectId.Effect206] = ("Resucitar", 86),
        [EffectId.EffectAddDamageReflection220] = ("Devolver danos", 87),
        [EffectId.EffectAddMagicDamageReduction] = ("+ Reduccion magica", 88),
        [EffectId.EffectSubMagicDamageReduction] = ("- Reduccion magica", 89),
        [EffectId.EffectAddPhysicalDamageReduction] = ("+ Reduccion fisica", 90),
        [EffectId.EffectSubPhysicalDamageReduction] = ("- Reduccion fisica", 91),
        [EffectId.EffectAddErosion] = ("+ Erosion", 96),
        [EffectId.EffectReviveAndGiveHPToLastDiedAlly] = ("Revivir ultimo aliado", 97),
    };

    private readonly IReadOnlyList<GameEffectOption> _options;

    public GameEffectDisplayService()
    {
        _options = Enum.GetValues<EffectId>()
            .OrderBy(GetSortPriority)
            .ThenBy(GetDisplayName)
            .Select(effectId => new GameEffectOption
            {
                EffectId = effectId,
                Label = GetDisplayName(effectId),
                SortPriority = GetSortPriority(effectId),
                SuggestedKind = SuggestKind(effectId),
                GroupLabel = ResolveGroupLabel(effectId),
            })
            .ToArray();
    }

    public IReadOnlyList<GameEffectOption> GetOptions() =>
        _options;

    public string GetDisplayName(EffectId effectId)
    {
        if (_labels.TryGetValue(effectId, out var entry))
            return entry.Label;

        var fallback = effectId.ToString();
        if (fallback.StartsWith("Effect"))
            fallback = fallback[6..];

        fallback = fallback.Replace("Add", "Agregar ")
                           .Replace("Sub", "Quitar ");

        return SplitPascalCaseRegex.Replace(fallback, "$1 $2").Trim();
    }

    public int GetSortPriority(EffectId effectId) =>
        _labels.TryGetValue(effectId, out var entry) ? entry.Priority : 1000 + (int)effectId;

    public EffectEditorKind SuggestKind(EffectId effectId) =>
        EffectManager.IsDiceEffect(effectId) ? EffectEditorKind.Dice : EffectEditorKind.Integer;

    private string ResolveGroupLabel(EffectId effectId)
    {
        var label = GetDisplayName(effectId);

        if (label.Contains("PA", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("PM", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Alcance", StringComparison.OrdinalIgnoreCase))
            return "Principales";

        if (label.Contains("Vitalidad", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Vida", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Fuerza", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Inteligencia", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Suerte", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Agilidad", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Sabiduria", StringComparison.OrdinalIgnoreCase))
            return "Stats";

        if (label.Contains("Resistencia", StringComparison.OrdinalIgnoreCase))
            return "Resistencias";

        if (label.Contains("Danos", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Curaciones", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("criticos", StringComparison.OrdinalIgnoreCase))
            return "Combate";

        return "Especiales";
    }
}
