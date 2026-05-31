using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Items.Custom
{
    public sealed class LivingObjectItem : CommonLivingObject
    {
        public LivingObjectItem(int id) : base(id)
        {
        }

        public LivingObjectItem(ItemRecord item) : base(item)
        {
        }

        protected override void ResolveRecord()
        {
            LivingObjectRecord = ItemManager.Instance.TryGetLivingObjectRecord(Template != null ? Template.Id : 0);
        }

        public bool TryBindTo(BasePlayerItem host)
        {
            if (host == null || host.Template == null)
                return false;

            if (Effects == null)
                Effects = new List<Effect>();

            InitializeFromCurrentState();

            if (LivingObjectRecord == null || LivingObjectRecord.ItemType <= 0)
                return false;

            if ((int)host.Template.TypeId != LivingObjectRecord.ItemType)
                return false;

            if (host.Effects != null && host.Effects.Any(x => x != null &&
                (x.Id == EffectsEnum.Effect_LivingObjectId ||
                 x.Id == EffectsEnum.Effect_ChangeAppearance ||
                 x.Id == EffectsEnum.Effect_ChangeAppearance_335)))
                return false;

            if (host.Effects == null)
                host.Effects = new List<Effect>();

            host.Effects.RemoveAll(x => x != null &&
                (x.Id == EffectsEnum.Effect_LivingObjectId ||
                 x.Id == EffectsEnum.Effect_LivingObjectMood ||
                 x.Id == EffectsEnum.Effect_LivingObjectSkin ||
                 x.Id == EffectsEnum.Effect_LivingObjectCategory ||
                 x.Id == EffectsEnum.Effect_LivingObjectLevel ||
                 x.Id == EffectsEnum.Effect_LastMeal));

            foreach (var effect in Effects.Where(x => x != null && x.Id != EffectsEnum.Effect_NonExchangeable_981 && x.Id != EffectsEnum.Effect_NonExchangeable_982))
            {
                host.Effects.RemoveAll(x => x != null && x.Id == effect.Id);
                host.Effects.Add(effect.Clone());
            }

            var resolvedLivingObjectId = ResolveLivingObjectId();
            UpsertEffect(host.Effects, EffectsEnum.Effect_LivingObjectId, resolvedLivingObjectId > 0 ? resolvedLivingObjectId : Template.Id);
            UpsertEffect(host.Effects, EffectsEnum.Effect_LivingObjectMood, Mood);
            UpsertEffect(host.Effects, EffectsEnum.Effect_LivingObjectSkin, SelectedLevel);
            UpsertEffect(host.Effects, EffectsEnum.Effect_LivingObjectCategory, LivingObjectRecord.ItemType);
            UpsertEffect(host.Effects, EffectsEnum.Effect_LivingObjectLevel, ExperiencePoints);

            var hostRaw = host.RawObjectEffects != null && host.RawObjectEffects.Count > 0
                ? ObjectEffectSerializer.Clone(host.RawObjectEffects)
                : host.Effects
                    .Where(x => x != null)
                    .Select(x => (ObjectEffect)x.GetObjectEffectInteger())
                    .ToList();

            hostRaw.RemoveAll(x => x != null &&
                (x.actionId == (short)EffectsEnum.Effect_LivingObjectId ||
                 x.actionId == (short)EffectsEnum.Effect_LivingObjectMood ||
                 x.actionId == (short)EffectsEnum.Effect_LivingObjectSkin ||
                 x.actionId == (short)EffectsEnum.Effect_LivingObjectCategory ||
                 x.actionId == (short)EffectsEnum.Effect_LivingObjectLevel ||
                 x.actionId == (short)EffectsEnum.Effect_LastMeal));

            AppendOrReplaceRaw(hostRaw, EffectsEnum.Effect_LivingObjectId, resolvedLivingObjectId > 0 ? resolvedLivingObjectId : Template.Id);
            AppendOrReplaceRaw(hostRaw, EffectsEnum.Effect_LivingObjectMood, Mood);
            AppendOrReplaceRaw(hostRaw, EffectsEnum.Effect_LivingObjectSkin, SelectedLevel);
            AppendOrReplaceRaw(hostRaw, EffectsEnum.Effect_LivingObjectCategory, LivingObjectRecord.ItemType);
            AppendOrReplaceRaw(hostRaw, EffectsEnum.Effect_LivingObjectLevel, ExperiencePoints);

            if (LastMeal.HasValue)
            {
                hostRaw.Add(new ObjectEffectDate(
                    (short)EffectsEnum.Effect_LastMeal,
                    (short)LastMeal.Value.Year,
                    (short)LastMeal.Value.Month,
                    (short)LastMeal.Value.Day,
                    (short)LastMeal.Value.Hour,
                    (short)LastMeal.Value.Minute));
            }

            host.RawObjectEffects = hostRaw;
            return true;
        }

        private static void UpsertEffect(List<Effect> effects, EffectsEnum effectId, int value)
        {
            if (effects == null)
                return;

            var effect = effects.FirstOrDefault(x => x != null && x.Id == effectId);
            if (effect == null)
                effects.Add(new Effect(effectId, 0, 0, value, 0, 0, 0, SpellShapeEnum.P, 0, 0));
            else
                effect.Value = value;
        }

        private static void AppendOrReplaceRaw(List<ObjectEffect> effects, EffectsEnum effectId, int value)
        {
            if (effects == null)
                return;

            effects.RemoveAll(x => x != null && x.actionId == (short)effectId);
            effects.Add(new ObjectEffectInteger((short)effectId, (short)value));
        }
    }
}
