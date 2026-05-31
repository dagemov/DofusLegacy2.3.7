using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Mounts;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Characters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Effects.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Mounts
{
    public class Mount
    {
        private static readonly double[][] XP_PER_GAP =
        {
            new double[] {0, 10},
            new double[] {10, 8},
            new double[] {20, 6},
            new double[] {30, 4},
            new double[] {40, 3},
            new double[] {50, 2},
            new[] {60d, 1.5d},
            new double[] {70, 1}
        };

        public MountRecord Record { get; }
        public MountTemplateRecord Template { get; }
        public List<MountBonusRecord> Bonuses { get; }
        public List<ObjectItem> Items { get; }

        public Mount(MountRecord record, MountTemplateRecord template, IEnumerable<MountBonusRecord> bonuses, IEnumerable<ObjectItem> items)
        {
            Record = record;
            Template = template;
            Bonuses = bonuses?.ToList() ?? new List<MountBonusRecord>();
            Items = items?.ToList() ?? new List<ObjectItem>();
        }

        public int Id => Record.Id;
        public int OwnerId => Record.OwnerId ?? 0;
        public bool IsInStable => Record.IsInStable > 0;
        public bool Sex => Record.Sex > 0;
        public string Name => string.IsNullOrWhiteSpace(Record.Name) ? "Dragodinde" : Record.Name;

        public int Level => ExperienceManager.Instance.GetMountLevelExperienceFloor(Record.Experience);
        public long ExperienceLevelFloor => ExperienceManager.Instance.GetMountExperienceLevelFloor((short)Level);
        public long ExperienceNextLevelFloor => ExperienceManager.Instance.GetMountNextExperienceLevelFloor((short)Level);

        public int MaxPods => 1000;
        public int EnergyMax => Math.Max(0, Template.EnergyBase + ((Level - 1) * Template.EnergyPerLevel));
        public int MaturityForAdult => Math.Max(0, Template.MaturityBase);
        public int StaminaMax => 10000;
        public int LoveMax => 10000;
        public int SerenityMax => 10000;
        public int AggressivityMax => 10000;
        public int ReproductionCountMax => 20;
        public int BoostLimiter => 100;
        public double BoostMax => 1000;
        public bool IsRideable => Record.Energy > 0 && Record.Maturity >= MaturityForAdult;
        public bool IsWild => false;
        public bool IsFecondationReady => false;

        public IEnumerable<int> GetBehaviors()
        {
            var behaviors = string.IsNullOrWhiteSpace(Record.BehaviorsCSV)
                ? new List<int>()
                : Record.BehaviorsCSV
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        int value;
                        return int.TryParse(x.Trim(), out value) ? value : 0;
                    })
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

            // Compat: un ancien patch utilisait 8 (Prédisposée génétique) à la place de 9 (Caméléone).
            if (behaviors.Count == 1 && behaviors.Contains(8))
                behaviors[0] = 9;

            if (!behaviors.Contains(9))
                behaviors.Add(9);

            return behaviors;
        }

        public short GetScaledBonus(int finalBonus)
        {
            if (finalBonus <= 0)
                return 0;

            return (short)Math.Floor(finalBonus * Level / 100d);
        }

        public long AdjustGivenExperience(Character giver, long amount)
        {
            if (amount <= 0)
                return 0;

            if (giver == null)
                return amount;

            var gap = giver.Level - Level;
            for (var i = XP_PER_GAP.Length - 1; i >= 0; i--)
            {
                if (gap > XP_PER_GAP[i][0])
                    return (long)Math.Floor(amount * XP_PER_GAP[i][1] * 0.01d);
            }

            return (long)Math.Floor(amount * XP_PER_GAP[0][1] * 0.01d);
        }

        public long AddExperience(long amount)
        {
            if (amount <= 0 || Level >= 100)
                return 0;

            var before = Record.Experience;
            var cap = ExperienceManager.Instance.GetMountExperienceLevelFloor(100);
            Record.Experience = Math.Min(cap, Record.Experience + amount);
            return Record.Experience - before;
        }

        public MountInformationsForPaddock GetInformationsForPaddock()
        {
            return new MountInformationsForPaddock(Template.Id, Name, Record.OwnerName ?? string.Empty);
        }

        public List<Effect> GetEffects()
        {
            return Bonuses
                .Select(x => new Effect((EffectsEnum)x.EffectId, 0, 0, GetScaledBonus(x.Amount), 0, 0, 0, SpellShapeEnum.P, 0, 0))
                .Where(x => x != null && x.Value > 0)
                .ToList();
        }

        public void ApplyMountEffects(Character owner, bool send = true)
        {
            if (owner == null)
                return;

            foreach (var effect in GetEffects())
            {
                if (ItemEffectHandler.TryGetRelatedStat(effect.Id, out var stats))
                    owner.Stats[stats].Equiped += (short)effect.Value;
            }

            if (send)
                owner.RefreshStats();
        }

        public void UnApplyMountEffects(Character owner, bool send = true)
        {
            if (owner == null)
                return;

            foreach (var effect in GetEffects())
            {
                if (ItemEffectHandler.TryGetRelatedStat(effect.Id, out var stats))
                    owner.Stats[stats].Equiped -= (short)effect.Value;
            }

            if (send)
                owner.RefreshStats();
        }

        public MountClientData GetClientData()
        {
            return new MountClientData(
                sex: Sex,
                isRideable: IsRideable,
                isWild: IsWild,
                isFecondationReady: IsFecondationReady,
                id: Id,
                model: Record.TemplateId,
                ancestor: new int[0],
                behaviors: GetBehaviors(),
                name: Name,
                ownerId: OwnerId,
                experience: Record.Experience,
                experienceForLevel: ExperienceLevelFloor,
                experienceForNextLevel: ExperienceNextLevelFloor,
                level: (sbyte)Level,
                maxPods: MaxPods,
                stamina: Math.Max(0, Record.Stamina),
                staminaMax: StaminaMax,
                maturity: Math.Max(0, Record.Maturity),
                maturityForAdult: MaturityForAdult,
                energy: Math.Max(0, Record.Energy),
                energyMax: EnergyMax,
                serenity: Record.Serenity,
                aggressivityMax: AggressivityMax,
                serenityMax: SerenityMax,
                love: Math.Max(0, Record.Love),
                loveMax: LoveMax,
                fecondationTime: Math.Max(0, Template.FecondationTime),
                boostLimiter: BoostLimiter,
                boostMax: BoostMax,
                reproductionCount: Math.Max(0, Record.ReproductionCount),
                reproductionCountMax: ReproductionCountMax,
                effectList: Bonuses
                    .Select(x => new ObjectEffectInteger((short)x.EffectId, GetScaledBonus(x.Amount)))
                    .Where(x => x.value > 0)
                    .ToArray());
        }
    }
}
