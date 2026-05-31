using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Spells;
using System;
using Dapper;
using Dapper.Contrib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;

namespace Sunshine.BaseServer.Loaders.World.Items
{
    public static class ItemsLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading Items...");

            JobManager.Instance.Recipes = JobManager.Instance.GetAllRecipes();
            JobManager.Instance.Harvests = JobManager.Instance.GetAllHarvests();
            JobManager.Instance.Runes = JobManager.Instance.GetAllRunes();
            JobManager.Instance.RunesEffects = JobManager.Instance.GetAllRunesEffects();

            var items = ItemManager.Instance.GetAllItemsTemplate();

            foreach (var item in items)
            {
                var itemSet = ItemManager.Instance.GetItemSetTemplate(item.ItemSetId);
                if (itemSet != null)
                    item.EffectSets = EffectManager.Instance.GetEffects(itemSet.Effects, true);
                else
                    item.EffectSets = new List<List<Effect>>();

                if (item is ItemTemplate)
                    item.EffectsBase = EffectManager.Instance.GetEffects((item as ItemTemplate).Effects);
                else
                    item.EffectsBase = EffectManager.Instance.GetEffects((item as WeaponTemplate).Effects);

                NormalizeMisclassifiedBread(item);
                ItemManager.Instance.Items.Add(item.Id, item);
            }

            ItemManager.Instance.LoadLivingObjects();

            Logs.Logger.Write($"[ World ] {ItemManager.Instance.Items.Count} Items Loaded");
            Logs.Logger.Write($"[ World ] {ItemManager.Instance.LivingObjects.Count} Living Objects Loaded");
        }

        private static void NormalizeMisclassifiedBread(ItemRecord item)
        {
            var template = item as ItemTemplate;
            if (template == null || string.IsNullOrWhiteSpace(template.Name))
                return;

            var currentType = (ItemTypeEnum)template.TypeId;
            if (currentType == ItemTypeEnum.DIVERS)
                return;

            if (!IsLikelyBread(template.Name) || !HasHealEffect(item.EffectsBase))
                return;

            if (!IsResourceLikeType(currentType))
                return;

            TrySetReadonlyAutoProperty(template, "TypeId", (byte)ItemTypeEnum.DIVERS);
        }

        private static bool IsLikelyBread(string name)
        {
            var normalized = name.ToLowerInvariant();
            return normalized.Contains("pain")
                || normalized.Contains("brioche")
                || normalized.Contains("briochette")
                || normalized.Contains("biscotte")
                || normalized.Contains("fougasse")
                || normalized.Contains("galette")
                || normalized.Contains("miche")
                || normalized.Contains("baguette");
        }

        private static bool HasHealEffect(List<Effect> effects)
        {
            return (effects ?? new List<Effect>()).Any(x =>
                x != null && (x.Id == EffectsEnum.Effect_HealHP_81
                           || x.Id == EffectsEnum.Effect_HealHP_108
                           || x.Id == EffectsEnum.Effect_HealHP_143
                           || x.Id == EffectsEnum.Effect_AddHealth));
        }

        private static bool IsResourceLikeType(ItemTypeEnum type)
        {
            return type == ItemTypeEnum.RESSOURCES_DIVERSES
                || type == ItemTypeEnum.CEREALE
                || type == ItemTypeEnum.FARINE
                || type == ItemTypeEnum.DIVERS;
        }

        private static void TrySetReadonlyAutoProperty(object target, string propertyName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return;

            var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(target, value);
        }
    }
}
