namespace CombatSim.Core;

/// <summary>
/// Port of Sunshine FightActor.InflictDamage damage math (SOURCE: FightActor.cs).
/// </summary>
public static class DamagePipeline
{
    public static DamagePipelineResult Compute(DamageRequest req, IRandom random, bool useMaxDice = false)
    {
        var result = new DamagePipelineResult();
        var steps = result.Steps;

        if (req.TargetOptions.GodMode)
        {
            steps.Add(new FormulaStep { Step = "godmode", Blocked = true });
            result.BlockedByGodmode = true;
            return result;
        }

        if (req.TargetOptions.Invulnerable)
        {
            steps.Add(new FormulaStep { Step = "invulnerable", Blocked = true, Out = 8400 });
            result.BlockedByInvulnerable = true;
            return result;
        }

        if (req.TargetOptions.HasSacrificePartner && !req.IsPoisoned)
        {
            steps.Add(new FormulaStep { Step = "sacrifice", Detail = "redirect" });
            result.RedirectedToSacrifice = true;
            return result;
        }

        int baseMin = (int)Math.Min(req.DiceNum, req.DiceFace);
        int baseMax = (int)Math.Max(req.DiceNum, req.DiceFace);
        int rolled = useMaxDice ? baseMax : random.Next(baseMin, baseMax);
        steps.Add(new FormulaStep { Step = "dice_roll", In = baseMin, Out = rolled, Detail = $"max={baseMax}" });

        int amount = rolled;
        int boost = req.CasterStats.SpellBoost;
        if (boost != 0)
        {
            amount += boost;
            steps.Add(new FormulaStep { Step = "spell_boost", In = rolled, Out = amount, Detail = $"+{boost}" });
        }

        int afterStats = ApplyOutgoingStats(req.CasterStats, amount, req.School);
        steps.Add(new FormulaStep { Step = "outgoing_stats", School = req.School.ToString(), In = amount, Out = afterStats });

        double armor = CalculateArmorReduction(req.School, req.TargetStats, req.CasterStats.Level, req.IsPoisoned);
        int afterResist = ApplyResistances(req.TargetStats, afterStats, req.School, req.Pvp, armor, req.IsPoisoned);
        steps.Add(new FormulaStep
        {
            Step = "resistance",
            In = afterStats,
            Out = afterResist,
            Flag = req.IsPoisoned ? "POISON_RESIST_BEHAVIOR" : null
        });

        if (req.TargetOptions.HasReflect && !req.IsPoisoned)
        {
            steps.Add(new FormulaStep { Step = "reflect", Out = afterResist });
            result.Reflected = true;
            result.FinalAmount = afterResist;
            return result;
        }

        amount = Math.Max(0, afterResist);
        int shield = req.TargetShield;
        if (shield > 0 && amount > 0)
        {
            int absorbed = Math.Min(shield, amount);
            amount -= absorbed;
            result.ShieldAbsorbed = absorbed;
            steps.Add(new FormulaStep { Step = "shield", Absorbed = absorbed, Out = amount });
        }

        result.FinalAmount = amount;
        steps.Add(new FormulaStep { Step = "final", Out = amount });
        return result;
    }

    // SOURCE: FightActor.CalculateDamage
    public static int ApplyOutgoingStats(ActorStats caster, int damage, EffectSchool type)
    {
        if (damage <= 0) return 0;
        int result = type switch
        {
            EffectSchool.Neutral => ScalePhysical(caster, damage, caster.NeutralDamageBonus),
            EffectSchool.Earth => ScalePhysical(caster, damage, caster.EarthDamageBonus),
            EffectSchool.Water => ScaleMagic(caster, damage, caster.Chance, caster.WaterDamageBonus),
            EffectSchool.Air => ScaleMagic(caster, damage, caster.Agility, caster.AirDamageBonus),
            EffectSchool.Fire => ScaleMagic(caster, damage, caster.Intelligence, caster.FireDamageBonus),
            _ => damage
        };
        return Math.Max(0, result);
    }

    private static int ScalePhysical(ActorStats c, int damage, int elemBonus) =>
        (int)((damage * (100 + c.Strength + c.DamageBonusPercent + c.DamageMultiplicator * 100)) / 100.0
              + c.DamageBonus + c.PhysicalDamage + elemBonus);

    private static int ScaleMagic(ActorStats c, int damage, int mainStat, int elemBonus) =>
        (int)((damage * (100 + mainStat + c.DamageBonusPercent + c.DamageMultiplicator * 100)) / 100.0
              + c.DamageBonus + c.MagicDamage + elemBonus);

    // SOURCE: FightActor.CalculateDamageResistance — poison bypasses resistances
    public static int ApplyResistances(ActorStats target, int damage, EffectSchool type, bool pvp, double armor, bool isPoisoned)
    {
        if (isPoisoned) return damage;

        double percentResist;
        double flatResist;

        switch (type)
        {
            case EffectSchool.Neutral:
                percentResist = target.NeutralResistPercent + (pvp ? target.PvpNeutralResistPercent : 0);
                flatResist = target.NeutralElementReduction + (pvp ? target.PvpNeutralElementReduction : 0) + target.PhysicalDamageReduction;
                break;
            case EffectSchool.Earth:
                percentResist = target.EarthResistPercent + (pvp ? target.PvpEarthResistPercent : 0);
                flatResist = target.EarthElementReduction + (pvp ? target.PvpEarthElementReduction : 0) + target.PhysicalDamageReduction;
                break;
            case EffectSchool.Water:
                percentResist = target.WaterResistPercent + (pvp ? target.PvpWaterResistPercent : 0);
                flatResist = target.WaterElementReduction + (pvp ? target.PvpWaterElementReduction : 0) + target.MagicDamageReduction;
                break;
            case EffectSchool.Air:
                percentResist = target.AirResistPercent + (pvp ? target.PvpAirResistPercent : 0);
                flatResist = target.AirElementReduction + (pvp ? target.PvpAirElementReduction : 0) + target.MagicDamageReduction;
                break;
            case EffectSchool.Fire:
                percentResist = target.FireResistPercent + (pvp ? target.PvpFireResistPercent : 0);
                flatResist = target.FireElementReduction + (pvp ? target.PvpFireElementReduction : 0) + target.MagicDamageReduction;
                break;
            default:
                return Math.Max(0, damage);
        }

        return Math.Max(0, (int)((1.0 - percentResist / 100.0) * (damage - (flatResist + armor))));
    }

    public static double CalculateArmorReduction(EffectSchool type, ActorStats target, int casterLevel, bool isPoisoned)
    {
        if (isPoisoned) return 0;
        return type switch
        {
            EffectSchool.Neutral => target.NeutralDamageArmor * (100 + 5 * casterLevel) / 100.0,
            EffectSchool.Earth => target.EarthDamageArmor * (100 + 5 * casterLevel) / 100.0,
            EffectSchool.Water => target.WaterDamageArmor * (100 + 5 * casterLevel) / 100.0,
            EffectSchool.Air => target.AirDamageArmor * (100 + 5 * casterLevel) / 100.0,
            EffectSchool.Fire => target.FireDamageArmor * (100 + 5 * casterLevel) / 100.0,
            _ => 0
        };
    }
}
