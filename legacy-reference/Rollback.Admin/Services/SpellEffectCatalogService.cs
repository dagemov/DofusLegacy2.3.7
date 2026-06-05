using Rollback.Admin.Models.GameEffects;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class SpellEffectCatalogService
{
    private static readonly (EffectId EffectId, string GroupLabel)[] Definitions =
    {
        (EffectId.EffectDamageNeutral, "Danos directos"),
        (EffectId.EffectDamageEarth, "Danos directos"),
        (EffectId.EffectDamageFire, "Danos directos"),
        (EffectId.EffectDamageWater, "Danos directos"),
        (EffectId.EffectDamageAir, "Danos directos"),
        (EffectId.EffectFixedNeutralDamage, "Danos directos"),
        (EffectId.EffectDamageCaster, "Danos directos"),

        (EffectId.EffectStealHPNeutral, "Robo de vida"),
        (EffectId.EffectStealHPEarth, "Robo de vida"),
        (EffectId.EffectStealHPFire, "Robo de vida"),
        (EffectId.EffectStealHPWater, "Robo de vida"),
        (EffectId.EffectStealHPAir, "Robo de vida"),
        (EffectId.EffectStealHPFix, "Robo de vida"),

        (EffectId.EffectHealHP81, "Curas y proteccion"),
        (EffectId.EffectHealHP108, "Curas y proteccion"),
        (EffectId.EffectHealHP143, "Curas y proteccion"),
        (EffectId.EffectGiveHPPercent, "Curas y proteccion"),
        (EffectId.EffectAddHealBonus, "Curas y proteccion"),
        (EffectId.EffectSubHealBonus, "Curas y proteccion"),
        (EffectId.EffectAddGlobalDamageReduction105, "Curas y proteccion"),
        (EffectId.EffectAddGlobalDamageReduction, "Curas y proteccion"),
        (EffectId.EffectAddMagicDamageReduction, "Curas y proteccion"),
        (EffectId.EffectSubMagicDamageReduction, "Curas y proteccion"),
        (EffectId.EffectAddPhysicalDamageReduction, "Curas y proteccion"),
        (EffectId.EffectSubPhysicalDamageReduction, "Curas y proteccion"),
        (EffectId.EffectAddDamageReflection, "Curas y proteccion"),
        (EffectId.EffectAddDamageReflection220, "Curas y proteccion"),

        (EffectId.EffectAddAP111, "PA y PM"),
        (EffectId.EffectRegainAP, "PA y PM"),
        (EffectId.EffectStealAP84, "PA y PM"),
        (EffectId.EffectLostAP, "PA y PM"),
        (EffectId.EffectSubAP, "PA y PM"),
        (EffectId.EffectAddMP, "PA y PM"),
        (EffectId.EffectAddMP128, "PA y PM"),
        (EffectId.EffectStealMP77, "PA y PM"),
        (EffectId.EffectLostMP, "PA y PM"),
        (EffectId.EffectSubMP, "PA y PM"),
        (EffectId.EffectAddDodgeAPProbability, "PA y PM"),
        (EffectId.EffectSubDodgeAPProbability, "PA y PM"),
        (EffectId.EffectAddDodgeMPProbability, "PA y PM"),
        (EffectId.EffectSubDodgeMPProbability, "PA y PM"),

        (EffectId.Effect701, "Boosts y debuffs"),
        (EffectId.EffectAddStrength, "Boosts y debuffs"),
        (EffectId.EffectSubStrength, "Boosts y debuffs"),
        (EffectId.EffectAddIntelligence, "Boosts y debuffs"),
        (EffectId.EffectSubIntelligence, "Boosts y debuffs"),
        (EffectId.EffectAddChance, "Boosts y debuffs"),
        (EffectId.EffectSubChance, "Boosts y debuffs"),
        (EffectId.EffectAddAgility, "Boosts y debuffs"),
        (EffectId.EffectSubAgility, "Boosts y debuffs"),
        (EffectId.EffectAddWisdom, "Boosts y debuffs"),
        (EffectId.EffectSubWisdom, "Boosts y debuffs"),
        (EffectId.EffectAddVitality, "Boosts y debuffs"),
        (EffectId.EffectSubVitality, "Boosts y debuffs"),
        (EffectId.EffectAddHealth, "Boosts y debuffs"),
        (EffectId.EffectAddRange, "Boosts y debuffs"),
        (EffectId.EffectSubRange, "Boosts y debuffs"),
        (EffectId.EffectAddCriticalHit, "Boosts y debuffs"),
        (EffectId.EffectSubCriticalHit, "Boosts y debuffs"),
        (EffectId.EffectAddDamageBonus, "Boosts y debuffs"),
        (EffectId.EffectAddDamageBonus121, "Boosts y debuffs"),
        (EffectId.EffectSubDamageBonus, "Boosts y debuffs"),
        (EffectId.EffectAddDamageBonusPercent, "Boosts y debuffs"),
        (EffectId.EffectSubDamageBonusPercent, "Boosts y debuffs"),
        (EffectId.EffectAddDodge, "Boosts y debuffs"),
        (EffectId.EffectSubDodge, "Boosts y debuffs"),
        (EffectId.EffectAddLock, "Boosts y debuffs"),
        (EffectId.EffectSubLock, "Boosts y debuffs"),
        (EffectId.EffectAddErosion, "Boosts y debuffs"),

        (EffectId.EffectAddEarthResistPercent, "Resistencias"),
        (EffectId.EffectAddWaterResistPercent, "Resistencias"),
        (EffectId.EffectAddAirResistPercent, "Resistencias"),
        (EffectId.EffectAddFireResistPercent, "Resistencias"),
        (EffectId.EffectAddNeutralResistPercent, "Resistencias"),
        (EffectId.EffectSubEarthResistPercent, "Resistencias"),
        (EffectId.EffectSubWaterResistPercent, "Resistencias"),
        (EffectId.EffectSubAirResistPercent, "Resistencias"),
        (EffectId.EffectSubFireResistPercent, "Resistencias"),
        (EffectId.EffectSubNeutralResistPercent, "Resistencias"),
        (EffectId.EffectAddEarthElementReduction, "Resistencias"),
        (EffectId.EffectAddWaterElementReduction, "Resistencias"),
        (EffectId.EffectAddAirElementReduction, "Resistencias"),
        (EffectId.EffectAddFireElementReduction, "Resistencias"),
        (EffectId.EffectAddNeutralElementReduction, "Resistencias"),
        (EffectId.EffectSubEarthElementReduction, "Resistencias"),
        (EffectId.EffectSubWaterElementReduction, "Resistencias"),
        (EffectId.EffectSubAirElementReduction, "Resistencias"),
        (EffectId.EffectSubFireElementReduction, "Resistencias"),
        (EffectId.EffectSubNeutralElementReduction, "Resistencias"),

        (EffectId.EffectTeleport, "Posicionamiento y control"),
        (EffectId.EffectPushBack, "Posicionamiento y control"),
        (EffectId.EffectPullForward, "Posicionamiento y control"),
        (EffectId.EffectSwitchPosition, "Posicionamiento y control"),
        (EffectId.EffectCarry, "Posicionamiento y control"),
        (EffectId.EffectThrow, "Posicionamiento y control"),
        (EffectId.EffectDispelMagicEffects, "Posicionamiento y control"),
        (EffectId.EffectSkipTurn, "Posicionamiento y control"),
        (EffectId.EffectInvisibility, "Posicionamiento y control"),
        (EffectId.EffectRevealsInvisible, "Posicionamiento y control"),
        (EffectId.EffectReflectSpell, "Posicionamiento y control"),

        (EffectId.EffectDouble, "Invocacion y especiales"),
        (EffectId.EffectSummon, "Invocacion y especiales"),
        (EffectId.EffectSummonStatic, "Invocacion y especiales"),
        (EffectId.EffectAddSummonLimit, "Invocacion y especiales"),
        (EffectId.EffectKill, "Invocacion y especiales"),
        (EffectId.Effect147, "Invocacion y especiales"),
        (EffectId.Effect206, "Invocacion y especiales"),
        (EffectId.EffectReviveAndGiveHPToLastDiedAlly, "Invocacion y especiales"),
    };

    private readonly IReadOnlyList<GameEffectOption> _options;

    public SpellEffectCatalogService(GameEffectDisplayService displayService)
    {
        var baseOptions = displayService.GetOptions().ToDictionary(x => x.EffectId);
        var options = new List<GameEffectOption>(Definitions.Length);

        for (var index = 0; index < Definitions.Length; index++)
        {
            var definition = Definitions[index];
            if (!baseOptions.TryGetValue(definition.EffectId, out var baseOption))
                continue;

            options.Add(new GameEffectOption
            {
                EffectId = definition.EffectId,
                Label = baseOption.Label,
                SuggestedKind = baseOption.SuggestedKind,
                SortPriority = index,
                GroupLabel = definition.GroupLabel,
            });
        }

        _options = options;
    }

    public IReadOnlyList<GameEffectOption> GetOptions() =>
        _options;

    public EffectId GetDefaultEffectId() =>
        EffectId.EffectDamageNeutral;
}
