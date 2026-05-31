using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Maps.Interactives;
using Sunshine.MySql.Database.World.Recipes;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using System;
using Sunshine.Protocol.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;
using Sunshine.WorldServer.Game.Items;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.Protocol.Enums;
using Sunshine.MySql.Database.World.Items;
using Sunshine.WorldServer.Game.Exchanges;

namespace Sunshine.WorldServer.Game.Actors.Characters.Jobs
{
    public class JobManager : Singleton<JobManager>
    {
        public Dictionary<int, List<InteractiveSkill>> JobSkills = new Dictionary<int, List<InteractiveSkill>>();

        public Dictionary<int, List<HarvestTemplate>> Harvests = new Dictionary<int, List<HarvestTemplate>>();

        public Dictionary<int, List<RecipeTemplate>> Recipes = new Dictionary<int, List<RecipeTemplate>>();

        public Dictionary<int, RuneTemplate> Runes = new Dictionary<int, RuneTemplate>();

        public Dictionary<EffectsEnum, RuneEffectRecord> RunesEffects = new Dictionary<EffectsEnum, RuneEffectRecord>();

        private Dictionary<sbyte, sbyte> _caseAvailable = new Dictionary<sbyte, sbyte>()
        {
            {1, 2}, {10, 3}, {20, 4}, {40, 5}, {60, 6}, {80, 7}, {100, 8}
        };     
        
        public RuneTemplate GetRune(int id)
        {
            if (Runes.ContainsKey(id))
                return Runes[id];
            return null;
        }

        public RuneEffectRecord GetRuneEffect(EffectsEnum effect)
        {
            if (RunesEffects.ContainsKey(effect))
                return RunesEffects[effect];
            return null;
        }

        public double GetPWR(int value, RuneTemplate rune)
        {
            return rune.PEffect * value;
        }

        public double GetPWR(int value, RuneEffectRecord rune)
        {
            return rune.PEffect * value;
        }

        public double GetPWRG(BasePlayerItem item)
        {
            double? pwrg = 0;

            foreach (var effect in item.Effects)
                pwrg += effect.Value * GetRuneEffect(effect.Id)?.PEffect;

            return pwrg.HasValue ? pwrg.Value : 0;
        }

        public double GetPWRGMin(BasePlayerItem item)
        {
            double? pwrg = 0;

            foreach (var effect in item.Effects)
                pwrg += effect.DiceNum * GetRuneEffect(effect.Id)?.PEffect;

            return pwrg.HasValue ? pwrg.Value : 0;
        }

        public double GetPWRGMax(BasePlayerItem item)
        {
            double? pwrg = 0;

            foreach (var effect in item.Effects)
                pwrg += effect.DiceFace * GetRuneEffect(effect.Id)?.PEffect;

            return pwrg.HasValue ? pwrg.Value : 0;
        }

        public bool IsOverMax(int baseJet, int currentJet, int value) // 5 < 3 + 3
        {
            return (baseJet < (currentJet + value));
        }

        public bool IsExotique(BasePlayerItem item, Effect runeEffect)
        {
            return item.Effects.Contains(runeEffect);
        }

        public BasePlayerItem CreateItemWithLessJets(BasePlayerItem item, double pwr)
        {
            AsyncRandom rdn = new AsyncRandom();

            double pwrCount = 0;

            while (pwrCount < pwr)
            {
                Effect effectRemove = item.Effects[rdn.Next(0, item.Effects.Count - 1)];

                if (GetRuneEffect(effectRemove.Id).PEffect < pwr)
                {
                    item.Effects.Remove(effectRemove);
                    effectRemove.Value -= 1;
                    item.Effects.Add(effectRemove);
                    pwr += 1;
                }
            }

            return ItemManager.Instance.CreatePlayerItem(item);
        }

        public BasePlayerItem CreateItemWithLessJets(BasePlayerItem item, Effect runeEffect, double pwr)
        {
            AsyncRandom rdn = new AsyncRandom();

            double pwrCount = 0;

            while (pwrCount < pwr)
            {
                Effect effectRemove = item.Effects[rdn.Next(0, item.Effects.Count - 1)];

                if (effectRemove.Id == runeEffect.Id)
                    continue;
                   
                if (GetRuneEffect(effectRemove.Id).PEffect < pwr)
                {
                    item.Effects.Remove(effectRemove);
                    effectRemove.Value -= 1;
                    item.Effects.Add(effectRemove);
                    pwr += 1;
                }
            }

            return ItemManager.Instance.CreatePlayerItem(item);
        }

        public BasePlayerItem CreateItemWithMoreJets(BasePlayerItem item, Effect runeEffect, double pwr, double puit, MagicPoolStatusEnum state)
        {
            if (state == MagicPoolStatusEnum.UNMODIFIED || state == MagicPoolStatusEnum.INCREASED)
            {
                if (IsExotique(item, runeEffect))
                    item.Effects.Add(runeEffect);
                else
                {
                    Effect effectExist = item.Effects.FirstOrDefault(x => x.Id == runeEffect.Id);

                    int index = item.Effects.IndexOf(effectExist);

                    item.Effects[index].Value += runeEffect.Value;
                }

                return ItemManager.Instance.CreatePlayerItem(item);
            }
            else // DECREASED
            { 
                BasePlayerItem itemDecreased = CreateItemWithLessJets(item, runeEffect, pwr - puit);

                if (IsExotique(itemDecreased, runeEffect))
                    itemDecreased.Effects.Add(runeEffect);
                else
                {
                    Effect effectExist = itemDecreased.Effects.FirstOrDefault(x => x.Id == runeEffect.Id);

                    int index = itemDecreased.Effects.IndexOf(effectExist);

                    itemDecreased.Effects[index].Value += runeEffect.Value;
                }

                return ItemManager.Instance.CreatePlayerItem(itemDecreased);
            }

        }

        public sbyte GetJobSlot(int level)
        {
            return _caseAvailable.LastOrDefault(x => x.Key <= level).Value;
        }

        private bool TryGetJobSkills(sbyte job, out List<InteractiveSkill> skills)
        {
            return JobSkills.TryGetValue(job, out skills) && skills != null;
        }

        private bool TryGetHarvestsForSkill(int skill, sbyte job, out List<HarvestTemplate> harvests)
        {
            harvests = null;

            List<InteractiveSkill> skills;
            if (!TryGetJobSkills(job, out skills))
                return false;

            var item = skills.FirstOrDefault(x => x != null && x.Id == skill);
            if (item == null || item.GatheredRessourceItem == -1)
                return false;

            return Harvests.TryGetValue(item.GatheredRessourceItem, out harvests) && harvests != null && harvests.Count > 0;
        }

        public IEnumerable<SkillActionDescription> GetSkillActionDescription(sbyte job, sbyte level)
        {
            var skillActions = new List<SkillActionDescription>();

            List<InteractiveSkill> skills;
            if (!TryGetJobSkills(job, out skills))
                return skillActions;

            foreach (var skill in skills.Where(x => x != null))
            {
                if (skill.GatheredRessourceItem != -1)
                {
                    if (!HasLevelToCollect(skill.Id, job, level))
                        continue;

                    var harvest = GetHarvest(skill.Id, job, level);
                    if (harvest == null || string.IsNullOrWhiteSpace(harvest.Loot))
                        continue;

                    var loot = harvest.Loot.Split(',');
                    byte minLoot;
                    byte maxLoot;
                    if (loot.Length < 2 || !byte.TryParse(loot[0], out minLoot) || !byte.TryParse(loot[1], out maxLoot))
                        continue;

                    skillActions.Add(new SkillActionDescriptionCollect((short)skill.Id, (byte)(harvest.Time / 100), minLoot, maxLoot));
                }
                else
                {
                    skillActions.Add(new SkillActionDescriptionCraft((short)skill.Id, GetJobSlot(level), 100));
                }
            }

            return skillActions;
        }

        public Dictionary<int, List<RecipeTemplate>> GetAllRecipes()
        {
            Dictionary<int, List<RecipeTemplate>> recipes = new Dictionary<int, List<RecipeTemplate>>();            
            var recipesRecord =  DatabaseManager.Connection.Query<RecipeTemplate>($"SELECT * FROM recipes");
            foreach(var record in recipesRecord)
            {
                if (recipes.ContainsKey(record.Skill))
                    recipes[record.Skill].Add(record);
                else
                    recipes.Add(record.Skill, new List<RecipeTemplate> { record });
            }
            return recipes;
        }

        public Dictionary<int, RuneTemplate> GetAllRunes()
        {
            return DatabaseManager.Connection.Query<RuneTemplate>($"SELECT * FROM runes").ToDictionary(x => x.Id);
        }

        public Dictionary<EffectsEnum, RuneEffectRecord> GetAllRunesEffects()
        {
            return DatabaseManager.Connection.Query<RuneEffectRecord>($"SELECT * FROM runes_effects").ToDictionary(x => (EffectsEnum)x.Id);
        }

        public Dictionary<int, List<HarvestTemplate>> GetAllHarvests()
        {
            Dictionary<int, List<HarvestTemplate>> harvests = new Dictionary<int, List<HarvestTemplate>>();
            var harvestsRecord = DatabaseManager.Connection.Query<HarvestTemplate>($"SELECT * FROM jobs_harvest");
            foreach (var record in harvestsRecord)
            {
                if (harvests.ContainsKey(record.Item))
                    harvests[record.Item].Add(record);
                else
                    harvests.Add(record.Item, new List<HarvestTemplate> { record });
            }
            return harvests;
        }

        public Tuple<RecipeTemplate, List<int>, List<int>> GetRecipe(IEnumerable<int> ingredients, int skill)
        {
            if (Recipes.ContainsKey(skill))
            {
                var ingredientsCSV = string.Join(",", ingredients.Select(x => x.ToString()));
                var recipe = Recipes[skill].FirstOrDefault(x => x.IngredientIdsCSV == ingredientsCSV);
                if (recipe != null)
                {
                    var ingredientsToList = recipe.IngredientIdsCSV.Split(',').Select(x => int.Parse(x)).ToList();
                    var quantitiesToList = recipe.QuantitiesCSV.Split(',').Select(x => int.Parse(x)).ToList();
                    return new Tuple<RecipeTemplate, List<int>, List<int>>(recipe, ingredientsToList, quantitiesToList);
                }
                else
                    return new Tuple<RecipeTemplate, List<int>, List<int>>(null, new List<int>(), new List<int>());
            }
            else
                return new Tuple<RecipeTemplate, List<int>, List<int>>(null, new List<int>(), new List<int>());
        }

        public HarvestTemplate GetHarvest(int skill, sbyte job, sbyte level)
        {
            List<HarvestTemplate> harvests;
            if (!TryGetHarvestsForSkill(skill, job, out harvests))
                return null;

            return harvests.FirstOrDefault(x => x != null && x.Level == level) ?? harvests.LastOrDefault(x => x != null && x.Level <= level);
        }

        public bool HasLevelToCollect(int skill, CharacterJobRecord job, int level)
        {
            return job != null && HasLevelToCollect(skill, job.Job, level);
        }

        public bool HasLevelToCollect(int skill, sbyte job, int level)
        {
            List<HarvestTemplate> harvests;
            if (!TryGetHarvestsForSkill(skill, job, out harvests))
                return false;

            var ressource = harvests.Where(x => x != null).OrderBy(x => x.Level).FirstOrDefault();
            return ressource != null && level >= ressource.Level;
        }

        public double GetTimeToCollect(int skill, CharacterJobRecord job, int level)
        {
            return job == null ? 0 : GetTimeToCollect(skill, job.Job, level);
        }

        public double GetTimeToCollect(int skill, sbyte job, int level)
        {
            List<HarvestTemplate> harvests;
            if (!TryGetHarvestsForSkill(skill, job, out harvests))
                return 0;

            var harvest = harvests.Where(x => x != null && x.Level <= level).OrderBy(x => x.Level).LastOrDefault();
            return harvest != null ? harvest.Time : 0;
        }

        public int GetRandomQuantity(BasePlayerItem ressource, int level)
        {
            if (ressource?.Template == null)
                return 0;

            List<HarvestTemplate> harvests;
            if (!Harvests.TryGetValue(ressource.Template.Id, out harvests) || harvests == null)
                return 0;

            var harvest = harvests.Where(x => x != null && x.Level <= level).OrderBy(x => x.Level).LastOrDefault();
            if (harvest == null || string.IsNullOrWhiteSpace(harvest.Loot))
                return 0;

            var quantites = harvest.Loot.Split(',').Select(x =>
            {
                int value;
                return int.TryParse(x, out value) ? value : 0;
            }).ToList();

            if (quantites.Count < 2)
                return 0;

            return new AsyncRandom().Next(quantites[0], quantites[1] + 1);
        }

        public long GetExperience(BasePlayerItem ressource)
        {
            if (ressource?.Template == null)
                return 0;

            List<HarvestTemplate> harvests;
            if (!Harvests.TryGetValue(ressource.Template.Id, out harvests) || harvests == null)
                return 0;

            var harvest = harvests.FirstOrDefault(x => x != null);
            return harvest != null ? harvest.Experience : 0;
        }

        public long GetExperience(int recipeCount)
        {
            switch (recipeCount)
            {
                case 2:
                    return 25;
                case 3:
                    return 50;
                case 4:
                    return 75;
                case 5:
                    return 125;
                case 6:
                    return 250;
                case 7:
                    return 500;
                case 8:
                    return 750;
                default:
                    return 0;
            }
        }
    }
}
