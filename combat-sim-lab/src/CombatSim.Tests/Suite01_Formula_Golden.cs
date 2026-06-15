using CombatSim.Core;

namespace CombatSim.Tests;

public class Suite01_Formula_Golden : SimTestBase
{
    [Fact]
    public void DiceRoll_Deterministic_WithSeed42()
    {
        var random = new SeededRandom(42);
        var req = new DamageRequest
        {
            DiceNum = 10,
            DiceFace = 15,
            School = EffectSchool.Fire,
            CasterStats = new ActorStats { Intelligence = 100 },
            TargetStats = new ActorStats()
        };
        var r1 = DamagePipeline.Compute(req, random);
        random = new SeededRandom(42);
        var r2 = DamagePipeline.Compute(req, random);
        Assert.Equal(r1.FinalAmount, r2.FinalAmount);
        Assert.Contains(r1.Steps, s => s.Step == "dice_roll");
    }

    [Fact]
    public void OutgoingStats_HighInt_Fire_Amplifies()
    {
        var req = new DamageRequest
        {
            DiceNum = 10,
            DiceFace = 10,
            School = EffectSchool.Fire,
            CasterStats = new ActorStats { Intelligence = 300, FireDamageBonus = 20 },
            TargetStats = new ActorStats()
        };
        var result = DamagePipeline.Compute(req, new SeededRandom(1), useMaxDice: true);
        Assert.True(result.FinalAmount > 10);
        var statsStep = result.Steps.First(s => s.Step == "outgoing_stats");
        Assert.Equal("Fire", statsStep.School);
    }

    [Fact]
    public void Resistance_20Percent_ReducesDamage()
    {
        var req = new DamageRequest
        {
            DiceNum = 20,
            DiceFace = 20,
            School = EffectSchool.Earth,
            CasterStats = new ActorStats { Strength = 100 },
            TargetStats = new ActorStats { EarthResistPercent = 20 }
        };
        var result = DamagePipeline.Compute(req, new SeededRandom(1), useMaxDice: true);
        var resist = result.Steps.First(s => s.Step == "resistance");
        Assert.True(resist.Out < resist.In);
    }

    [Fact]
    public void PoisonResistBehavior_BypassesResistances()
    {
        var req = new DamageRequest
        {
            DiceNum = 20,
            DiceFace = 20,
            School = EffectSchool.Earth,
            CasterStats = new ActorStats { Strength = 100 },
            TargetStats = new ActorStats { EarthResistPercent = 50 },
            IsPoisoned = true
        };
        var result = DamagePipeline.Compute(req, new SeededRandom(1), useMaxDice: true);
        Assert.True(result.FinalAmount > 0);
        Assert.Contains(result.Steps, s => s.Flag == "POISON_RESIST_BEHAVIOR");
    }

    // Spell 196 (Veneno): Effect_DamageNeutral duration=2 dice=3-0 — poison must not zero out via resistances.
    [Fact]
    public void VenenoSpell196_NeutralPoisonDice3_0_BypassesResistances()
    {
        var req = new DamageRequest
        {
            DiceNum = 3,
            DiceFace = 0,
            School = EffectSchool.Neutral,
            CasterStats = new ActorStats { Intelligence = 150 },
            TargetStats = new ActorStats { NeutralResistPercent = 80, PhysicalDamageReduction = 50 },
            IsPoisoned = true
        };
        var result = DamagePipeline.Compute(req, new SeededRandom(42), useMaxDice: true);
        Assert.True(result.FinalAmount > 0);
        Assert.Contains(result.Steps, s => s.Flag == "POISON_RESIST_BEHAVIOR");
    }

    [Fact]
    public void Shield_Absorbs_Before_Hp()
    {
        var req = new DamageRequest
        {
            DiceNum = 30,
            DiceFace = 30,
            School = EffectSchool.Fire,
            CasterStats = new ActorStats { Intelligence = 100 },
            TargetStats = new ActorStats(),
            TargetShield = 80
        };
        var result = DamagePipeline.Compute(req, new SeededRandom(1), useMaxDice: true);
        Assert.True(result.ShieldAbsorbed > 0);
        Assert.True(result.FinalAmount < result.Steps.First(s => s.Step == "resistance").Out);
        Assert.Contains(result.Steps, s => s.Step == "shield");
    }
}
