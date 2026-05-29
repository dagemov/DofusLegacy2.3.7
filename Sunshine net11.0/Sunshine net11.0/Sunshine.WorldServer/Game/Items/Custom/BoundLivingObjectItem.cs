using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Spells;
using System;

namespace Sunshine.WorldServer.Game.Items.Custom
{
    public sealed class BoundLivingObjectItem : CommonLivingObject
    {
        public BoundLivingObjectItem(int id) : base(id)
        {
        }

        public BoundLivingObjectItem(ItemRecord item) : base(item)
        {
        }

        protected override void ResolveRecord()
        {
            var livingId = ExtractLivingObjectId(this);
            LivingObjectRecord = ItemManager.Instance.TryGetLivingObjectRecord(livingId);
        }

        public bool TryFeed(BasePlayerItem food)
        {
            if (food == null || LivingObjectRecord == null)
                return false;

            if ((int)food.Template.TypeId != LivingObjectRecord.ItemType)
                return false;

            ExperiencePoints = (short)(ExperiencePoints + Math.Max(1, (int)Math.Ceiling(food.Level / 2d)));
            Mood = 1;
            LastMeal = DateTime.Now;
            return true;
        }

        public BasePlayerItem Dissociate()
        {
            int livingObjectId = LivingObjectId;
            if (livingObjectId <= 0)
                return null;

            short mood = Mood;
            short selectedLevel = SelectedLevel;
            short experience = ExperiencePoints;
            var lastMeal = LastMeal;

            RemoveEffect(EffectsEnum.Effect_LivingObjectId);
            RemoveEffect(EffectsEnum.Effect_LivingObjectMood);
            RemoveEffect(EffectsEnum.Effect_LivingObjectSkin);
            RemoveEffect(EffectsEnum.Effect_LivingObjectCategory);
            RemoveEffect(EffectsEnum.Effect_LivingObjectLevel);
            RemoveEffect(EffectsEnum.Effect_LastMeal);
            SynchronizeRawEffects();

            var livingObject = ItemManager.Instance.CreatePlayerItem(livingObjectId, 1);
            var living = livingObject as LivingObjectItem;
            if (living != null)
            {
                living.InitializeFromCurrentState();
                living.Mood = mood;
                living.ExperiencePoints = experience;
                living.SelectedLevel = selectedLevel;
                living.LastMeal = lastMeal;
            }

            return livingObject;
        }
    }
}
