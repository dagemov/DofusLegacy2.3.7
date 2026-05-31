using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Game.Maps.PaddockInstances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Interactives
{
    public static class HouseInteractiveBuilder
    {
        private static readonly HashSet<int> ExteriorSkillIds = new HashSet<int> { 81, 84, 97, 98, 100, 108 };
        private static readonly HashSet<int> ChestSkillIds = new HashSet<int> { 104, 105, 106 };
        private static readonly HashSet<int> HouseRelatedSkillIds = new HashSet<int>(ExteriorSkillIds.Concat(ChestSkillIds));
        private static readonly HashSet<int> PaddockRelatedSkillIds = new HashSet<int> { 175, 176, 177, 178 };
        private static readonly object SyntheticPaddockSkillSync = new object();
        private static readonly Dictionary<string, InteractiveElementSkill> SyntheticPaddockSkills = new Dictionary<string, InteractiveElementSkill>();

        private static InteractiveElementSkill GetOrCreateSyntheticPaddockSkill(int mapId, Interactive interactive, int skillId)
        {
            var key = mapId + ":" + interactive.Element + ":" + skillId;

            lock (SyntheticPaddockSkillSync)
            {
                InteractiveElementSkill skill;
                if (SyntheticPaddockSkills.TryGetValue(key, out skill))
                    return skill;

                int skillUid = InteractiveManager.Instance.GenerateId();
                skill = new InteractiveElementSkill(skillId, skillUid);
                SyntheticPaddockSkills[key] = skill;
                InteractiveManager.Instance.RegisterSkill(skillUid, mapId, skillId, interactive.Element, interactive.HouseId, interactive.PaddockInstanceId);
                return skill;
            }
        }

        public static InteractiveElement BuildForClient(Interactive interactive, WorldClient client)
        {
            if (interactive == null)
                return null;

            var baseEnabled = interactive.GetInteractiveElement?.enabledSkills ?? Enumerable.Empty<InteractiveElementSkill>();
            var baseDisabled = interactive.GetInteractiveElement?.disabledSkills ?? Enumerable.Empty<InteractiveElementSkill>();
            var combinedSkills = baseEnabled.Concat(baseDisabled).Where(x => x != null).ToList();
            var combinedSkillIds = new HashSet<int>(combinedSkills.Select(x => x.skillId));
            var currentMapId = client?.Character?.Map?.Id ?? 0;

            if (interactive.IsHouseInteractive &&
                !HouseManager.Instance.CanDisplayHouseInteractive(client?.Character, interactive.HouseId, currentMapId, interactive.Element))
                return null;

            if (interactive.IsPaddockInstanceInteractive &&
                !PaddockInstanceManager.Instance.CanDisplayPaddockInstanceInteractive(client?.Character, interactive.PaddockInstanceId, currentMapId, interactive.Element))
                return null;

            bool isPaddockInteractive = interactive.Type == 120 ||
                                        combinedSkillIds.Overlaps(PaddockRelatedSkillIds);

            if (isPaddockInteractive)
            {
                foreach (var skillId in PaddockRelatedSkillIds)
                {
                    if (combinedSkillIds.Contains(skillId))
                        continue;

                    combinedSkills.Add(GetOrCreateSyntheticPaddockSkill(currentMapId, interactive, skillId));
                    combinedSkillIds.Add(skillId);
                }
            }

            if (!combinedSkillIds.Overlaps(HouseRelatedSkillIds) && !combinedSkillIds.Overlaps(PaddockRelatedSkillIds))
                return interactive.GetInteractiveElement;

            IEnumerable<int> visibleSkills = null;
            var isWorldChestInteractive = !interactive.IsHouseInteractive &&
                                          interactive.Type == 85 &&
                                          combinedSkillIds.Overlaps(ChestSkillIds);

            if (combinedSkillIds.Overlaps(PaddockRelatedSkillIds))
            {
                var paddock = PaddockManager.Instance.GetPaddockByMap(currentMapId);
                visibleSkills = paddock != null
                    ? paddock.GetVisibleSkills(client, PaddockRelatedSkillIds)
                    : combinedSkillIds.Where(x => x == 175);
            }
            else if (isWorldChestInteractive || combinedSkillIds.Overlaps(ChestSkillIds))
            {
                // Les coffres de maison viennent de worlds_interactives : Type 85 + SkillsCSV 104,105.
                // world_maps_house ne doit jamais fournir les coffres.
                // On résout donc la maison par le contexte personnage/map intérieure.
                var interiorHouse = HouseManager.Instance.ResolveChestHouse(
                    client?.Character,
                    interactive.Element);

                visibleSkills = interiorHouse != null
                    ? interiorHouse.GetVisibleChestSkills(client)
                    : combinedSkillIds.Where(x => ChestSkillIds.Contains(x));
            }
            else
            {
                var exteriorHouse = HouseManager.Instance.GetHouseByExteriorInteractive(currentMapId, interactive.Element);
                if (exteriorHouse != null)
                    visibleSkills = exteriorHouse.GetVisibleExteriorSkills(client);
            }

            if (visibleSkills == null)
                return interactive.GetInteractiveElement;

            var visibleSkillIds = new HashSet<int>(visibleSkills);
            var enabledSkills = combinedSkills
                .Where(x => visibleSkillIds.Contains(x.skillId))
                .GroupBy(x => x.skillInstanceUid)
                .Select(x => x.First())
                .ToList();

            return new InteractiveElement(interactive.Element, interactive.Type, enabledSkills, new List<InteractiveElementSkill>());
        }
    }
}
