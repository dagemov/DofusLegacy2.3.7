using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Items.Custom
{
    public abstract class CommonLivingObject : BasePlayerItem
    {
        protected static readonly short[] ExperienceSteps =
        {
            0, 10, 21, 33, 46, 60, 75, 91, 108, 126, 145, 165, 186, 208, 231, 255,
            280, 306, 333, 361
        };

        protected CommonLivingObject(int id) : base(id)
        {
        }

        protected CommonLivingObject(ItemRecord item) : base(item)
        {
        }

        protected LivingObjectRecord LivingObjectRecord { get; set; }

        public int LivingObjectId => GetEffectValue(EffectsEnum.Effect_LivingObjectId, Template != null ? Template.Id : 0);

        public short Mood
        {
            get => (short)GetEffectValue(EffectsEnum.Effect_LivingObjectMood, 0);
            set => SetEffectValue(EffectsEnum.Effect_LivingObjectMood, value);
        }

        public short SelectedLevel
        {
            get => (short)Math.Max(1, GetEffectValue(EffectsEnum.Effect_LivingObjectSkin, 1));
            set
            {
                short maxLevel = CurrentLevel;
                if (LivingObjectRecord != null && LivingObjectRecord.Skins != null && LivingObjectRecord.Skins.Count > 0)
                    maxLevel = (short)Math.Min(maxLevel, LivingObjectRecord.Skins.Count);

                if (maxLevel <= 0)
                    maxLevel = 1;

                short clamped = value;
                if (clamped <= 0)
                    clamped = 1;
                if (clamped > maxLevel)
                    clamped = maxLevel;

                SetEffectValue(EffectsEnum.Effect_LivingObjectSkin, clamped);
            }
        }

        public short ExperiencePoints
        {
            get => (short)Math.Max(0, GetEffectValue(EffectsEnum.Effect_LivingObjectLevel, 0));
            set => SetEffectValue(EffectsEnum.Effect_LivingObjectLevel, (short)Math.Max(0, (int)value));
        }

        public short CurrentLevel
        {
            get
            {
                short level = 1;
                while (ExperienceSteps.Length > level && ExperienceSteps[level] <= ExperiencePoints)
                    level++;

                return level;
            }
        }

        public int SupportedItemType => LivingObjectRecord != null ? LivingObjectRecord.ItemType : 0;

        public override short AppearanceId
        {
            get
            {
                if (LivingObjectRecord != null && LivingObjectRecord.Skins != null && LivingObjectRecord.Skins.Count > 0)
                {
                    int index = SelectedLevel - 1;
                    if (index >= 0 && index < LivingObjectRecord.Skins.Count)
                        return (short)LivingObjectRecord.Skins[index];
                }

                return base.AppearanceId;
            }
        }

        public DateTime? LastMeal
        {
            get
            {
                var rawDate = RawObjectEffects != null
                    ? RawObjectEffects.OfType<ObjectEffectDate>().FirstOrDefault(x => x.actionId == (short)EffectsEnum.Effect_LastMeal)
                    : null;

                if (rawDate == null)
                    return null;

                try
                {
                    return new DateTime(rawDate.year, rawDate.month, rawDate.day, rawDate.hour, rawDate.minute, 0, DateTimeKind.Local);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (value.HasValue)
                    UpsertLastMealRawEffect(value.Value);
                else
                    RemoveEffect(EffectsEnum.Effect_LastMeal);

                SynchronizeRawEffects();
            }
        }

        public virtual void InitializeFromCurrentState()
        {
            if (Effects == null)
                Effects = new List<Effect>();

            if (RawObjectEffects == null)
                RawObjectEffects = new List<ObjectEffect>();

            ResolveRecord();

            EnsureEffectValue(EffectsEnum.Effect_LivingObjectMood, 0);
            EnsureEffectValue(EffectsEnum.Effect_LivingObjectSkin, 1);
            EnsureEffectValue(EffectsEnum.Effect_LivingObjectLevel, 0);

            var resolvedLivingObjectId = ResolveLivingObjectId();
            if (resolvedLivingObjectId > 0)
                EnsureEffectValue(EffectsEnum.Effect_LivingObjectId, resolvedLivingObjectId);

            if (LivingObjectRecord != null && LivingObjectRecord.ItemType > 0)
                EnsureEffectValue(EffectsEnum.Effect_LivingObjectCategory, LivingObjectRecord.ItemType);

            ClampSelectedLevel();
            SynchronizeRawEffects();
        }

        protected abstract void ResolveRecord();

        protected int ResolveLivingObjectId()
        {
            var effectValue = ExtractLivingObjectId(this);
            if (effectValue > 0)
                return effectValue;

            if (LivingObjectRecord != null && LivingObjectRecord.Id > 0)
                return LivingObjectRecord.Id;

            return Template != null && Type == ItemTypeEnum.OBJET_VIVANT
                ? Template.Id
                : 0;
        }

        protected void ClampSelectedLevel()
        {
            short current = (short)Math.Max(1, GetEffectValue(EffectsEnum.Effect_LivingObjectSkin, 1));
            short levelCap = CurrentLevel;
            if (LivingObjectRecord != null && LivingObjectRecord.Skins != null && LivingObjectRecord.Skins.Count > 0)
                levelCap = (short)Math.Min(levelCap, LivingObjectRecord.Skins.Count);

            if (levelCap <= 0)
                levelCap = 1;

            short clamped = current;
            if (clamped <= 0)
                clamped = 1;
            if (clamped > levelCap)
                clamped = levelCap;

            SetEffectValue(EffectsEnum.Effect_LivingObjectSkin, clamped);
        }

        protected int GetEffectValue(EffectsEnum effectId, int defaultValue)
        {
            var effect = Effects != null ? Effects.FirstOrDefault(x => x != null && x.Id == effectId) : null;
            if (effect != null)
                return effect.Value;

            var rawInteger = RawObjectEffects != null
                ? RawObjectEffects.OfType<ObjectEffectInteger>().FirstOrDefault(x => x.actionId == (short)effectId)
                : null;

            return rawInteger != null ? rawInteger.value : defaultValue;
        }

        protected void EnsureEffectValue(EffectsEnum effectId, int defaultValue)
        {
            if (Effects == null)
                Effects = new List<Effect>();

            if (Effects.Any(x => x != null && x.Id == effectId))
                return;

            Effects.Add(new Effect(effectId, 0, 0, defaultValue, 0, 0, 0, SpellShapeEnum.P, 0, 0));
        }

        protected void SetEffectValue(EffectsEnum effectId, int value)
        {
            if (Effects == null)
                Effects = new List<Effect>();

            var effect = Effects.FirstOrDefault(x => x != null && x.Id == effectId);
            if (effect == null)
            {
                effect = new Effect(effectId, 0, 0, value, 0, 0, 0, SpellShapeEnum.P, 0, 0);
                Effects.Add(effect);
            }
            else
            {
                effect.Value = value;
            }

            SynchronizeRawEffects();
        }

        protected void RemoveEffect(EffectsEnum effectId)
        {
            if (Effects != null)
                Effects.RemoveAll(x => x != null && x.Id == effectId);

            if (RawObjectEffects != null)
                RawObjectEffects.RemoveAll(x => x != null && x.actionId == (short)effectId);
        }

        protected void SynchronizeRawEffects()
        {
            if (Effects == null)
                Effects = new List<Effect>();

            if (RawObjectEffects == null)
                RawObjectEffects = new List<ObjectEffect>();

            var preservedLastMeal = LastMeal;

            if (RawObjectEffects.Count == 0 || RawObjectEffects.All(x => x is ObjectEffectInteger))
            {
                RawObjectEffects = Effects
                    .Where(x => x != null)
                    .Select(x => (ObjectEffect)x.GetObjectEffectInteger())
                    .ToList();

                if (preservedLastMeal.HasValue)
                    UpsertLastMealRawEffect(preservedLastMeal.Value);

                return;
            }

            UpsertLivingRawEffect(EffectsEnum.Effect_LivingObjectId);
            UpsertLivingRawEffect(EffectsEnum.Effect_LivingObjectMood);
            UpsertLivingRawEffect(EffectsEnum.Effect_LivingObjectSkin);
            UpsertLivingRawEffect(EffectsEnum.Effect_LivingObjectCategory);
            UpsertLivingRawEffect(EffectsEnum.Effect_LivingObjectLevel);

            if (preservedLastMeal.HasValue)
                UpsertLastMealRawEffect(preservedLastMeal.Value);
        }

        private void UpsertLivingRawEffect(EffectsEnum effectId)
        {
            var effect = Effects != null ? Effects.FirstOrDefault(x => x != null && x.Id == effectId) : null;
            var existing = RawObjectEffects
                .OfType<ObjectEffectInteger>()
                .FirstOrDefault(x => x.actionId == (short)effectId);

            if (effect == null)
            {
                if (existing != null)
                    RawObjectEffects.Remove(existing);
                return;
            }

            if (existing != null)
                existing.value = (short)effect.Value;
            else
                RawObjectEffects.Add(new ObjectEffectInteger((short)effectId, (short)effect.Value));
        }

        private void UpsertLastMealRawEffect(DateTime value)
        {
            if (RawObjectEffects == null)
                RawObjectEffects = new List<ObjectEffect>();

            RawObjectEffects.RemoveAll(x => x != null && x.actionId == (short)EffectsEnum.Effect_LastMeal);
            RawObjectEffects.Add(new ObjectEffectDate(
                (short)EffectsEnum.Effect_LastMeal,
                (short)value.Year,
                (short)value.Month,
                (short)value.Day,
                (short)value.Hour,
                (short)value.Minute));
        }

        protected static int ExtractLivingObjectId(BasePlayerItem item)
        {
            if (item == null)
                return 0;

            var effect = item.Effects != null
                ? item.Effects.FirstOrDefault(x => x != null && x.Id == EffectsEnum.Effect_LivingObjectId)
                : null;

            if (effect != null)
                return effect.Value;

            var rawInteger = item.RawObjectEffects != null
                ? item.RawObjectEffects.OfType<ObjectEffectInteger>().FirstOrDefault(x => x.actionId == (short)EffectsEnum.Effect_LivingObjectId)
                : null;

            return rawInteger != null ? rawInteger.value : 0;
        }

        public override ObjectItem GetObjectItem()
        {
            SynchronizeRawEffects();
            return base.GetObjectItem();
        }

        public override ObjectItemToSellInBid GetObjectItemToSell()
        {
            SynchronizeRawEffects();
            return base.GetObjectItemToSell();
        }

        public override BidExchangerObjectInfo GetBidExchangerObjectInfo()
        {
            SynchronizeRawEffects();
            return base.GetBidExchangerObjectInfo();
        }

        public override ObjectItemNotInContainer GetObjectItemNotInContainer()
        {
            SynchronizeRawEffects();
            return base.GetObjectItemNotInContainer();
        }
    }
}
