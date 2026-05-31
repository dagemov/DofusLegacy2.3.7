using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Maps.Interactives.Skills;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Maps.PaddockInstances;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Interactives
{
    public static class SkillDispatcher
    {
        public static void Dispatch(WorldClient client, int skillUid, int elementId)
        {
            if (client == null || client.Character == null || client.Character.Map == null)
                return;

            int? resolvedSkillId = null;
            InteractiveSkillBinding mapping;

            if (InteractiveManager.Instance.Interactives.TryGetValue(skillUid, out mapping))
            {
                if (client.Character.Map.Id != mapping.MapId)
                    return;

                if (mapping.ElementId > 0 && mapping.ElementId != elementId)
                    return;

                if (mapping.HouseId > 0 &&
                    !HouseManager.Instance.CanUseHouseInteractive(client.Character, mapping.HouseId, mapping.MapId, elementId))
                {
                    client.Character.SendServerMessage("Cette interaction n'appartient pas à cette maison.");
                    return;
                }

                if (mapping.PaddockInstanceId > 0 &&
                    !PaddockInstanceManager.Instance.CanUsePaddockInstanceInteractive(client.Character, mapping.PaddockInstanceId, mapping.MapId, elementId))
                {
                    client.Character.SendServerMessage("Cette interaction n'appartient pas à cet enclos instancié.");
                    return;
                }

                resolvedSkillId = mapping.SkillId;
            }
            else
            {
                var interactiveCandidates = client.Character.Map.Interactives != null
                    ? client.Character.Map.Interactives.Where(x => x != null && x.Element == elementId).ToList()
                    : new System.Collections.Generic.List<Interactive>();

                bool IsChestCandidate(Interactive candidate)
                {
                    if (candidate == null || (candidate.HouseId != 0 || candidate.PaddockInstanceId != 0) || candidate.Type != 85 || candidate.GetInteractiveElement == null)
                        return false;

                    var candidateSkills = (candidate.GetInteractiveElement.enabledSkills ?? Enumerable.Empty<InteractiveElementSkill>())
                        .Concat(candidate.GetInteractiveElement.disabledSkills ?? Enumerable.Empty<InteractiveElementSkill>())
                        .Where(x => x != null)
                        .Select(x => x.skillId);

                    return candidateSkills.Any(x => x == 104 || x == 105 || x == 106);
                }

                var orderedCandidates = interactiveCandidates
                    .OrderByDescending(IsChestCandidate)
                    .ThenBy(x => x.HouseId > 0 ? 1 : 0)
                    .ToList();

                Interactive matchedInteractive = null;
                InteractiveElementSkill matchedSkill = null;

                foreach (var candidate in orderedCandidates)
                {
                    if (candidate == null || candidate.GetInteractiveElement == null)
                        continue;

                    if (candidate.HouseId > 0 &&
                        !HouseManager.Instance.CanUseHouseInteractive(
                            client.Character,
                            candidate.HouseId,
                            client.Character.Map.Id,
                            elementId))
                    {
                        continue;
                    }

                    if (candidate.PaddockInstanceId > 0 &&
                        !PaddockInstanceManager.Instance.CanUsePaddockInstanceInteractive(
                            client.Character,
                            candidate.PaddockInstanceId,
                            client.Character.Map.Id,
                            elementId))
                    {
                        continue;
                    }

                    var enabledSkills = (candidate.GetInteractiveElement.enabledSkills ?? Enumerable.Empty<InteractiveElementSkill>())
                        .Where(x => x != null)
                        .ToList();

                    matchedSkill = enabledSkills.FirstOrDefault(x =>
                        x.skillInstanceUid == skillUid ||
                        (IsChestCandidate(candidate) && x.skillId == skillUid));

                    if (matchedSkill != null)
                    {
                        matchedInteractive = candidate;
                        break;
                    }
                }

                if (matchedInteractive == null)
                {
                    matchedInteractive =
                        orderedCandidates.FirstOrDefault(IsChestCandidate) ??
                        orderedCandidates.FirstOrDefault(x =>
                            x.PaddockInstanceId > 0 &&
                            PaddockInstanceManager.Instance.CanUsePaddockInstanceInteractive(
                                client.Character,
                                x.PaddockInstanceId,
                                client.Character.Map.Id,
                                elementId)) ??
                        orderedCandidates.FirstOrDefault(x => x.HouseId == 0 && x.PaddockInstanceId == 0) ??
                        orderedCandidates.FirstOrDefault(x =>
                            x.HouseId > 0 &&
                            HouseManager.Instance.CanUseHouseInteractive(
                                client.Character,
                                x.HouseId,
                                client.Character.Map.Id,
                                elementId)) ??
                        orderedCandidates.FirstOrDefault();

                    if (matchedInteractive != null && matchedInteractive.GetInteractiveElement != null)
                    {
                        var enabledSkills = (matchedInteractive.GetInteractiveElement.enabledSkills ?? Enumerable.Empty<InteractiveElementSkill>())
                            .Where(x => x != null)
                            .ToList();

                        if (enabledSkills.Count == 1)
                            matchedSkill = enabledSkills[0];
                    }
                }

                if (matchedInteractive != null && matchedSkill != null)
                {
                    resolvedSkillId = matchedSkill.skillId;
                    InteractiveManager.Instance.RegisterSkill(
                        skillUid,
                        client.Character.Map.Id,
                        matchedSkill.skillId,
                        elementId,
                        matchedInteractive.HouseId,
                        matchedInteractive.PaddockInstanceId);
                }
            }

            if (!resolvedSkillId.HasValue)
            {
                Logs.Logger.WriteError($"Cannot resolve the skill instance ({skillUid}) for element {elementId} on map {client.Character.Map.Id}.");
                client.Character.SendServerMessage("Interaction introuvable.");
                return;
            }

            Func<Skill> skillFactory = null;

            // Concasseur de ressources : ne jamais le router vers l'atelier craft.
            if (resolvedSkillId.Value == 121 || resolvedSkillId.Value == 181)
            {
                skillFactory = () => new SkillItemCrusher();
            }
            else
            {
                var dynamicSkill = JobManager.Instance.JobSkills.Values
                    .SelectMany(x => x)
                    .FirstOrDefault(x => x != null && x.Id == resolvedSkillId.Value);

                if (dynamicSkill != null)
                {
                    if (dynamicSkill.GatheredRessourceItem != -1)
                        skillFactory = () => new SkillHarvest();
                    else if (dynamicSkill.IsForgemagus)
                        skillFactory = () => new SkillWorkshopFM();
                    else if (!string.IsNullOrWhiteSpace(dynamicSkill.CraftableItemIdsCSV))
                        skillFactory = () => new SkillWorkshop();
                }

                if (skillFactory == null)
                    SkillManager.Instance.Skills.TryGetValue(resolvedSkillId.Value, out skillFactory);
            }

            if (skillFactory == null)
            {
                Logs.Logger.WriteError($"Cannot dispatch the skill ({client.Character.Map.Id}, {resolvedSkillId.Value}) !");
                client.Character.SendServerMessage("Skill introuvable pour cette interaction.");
                return;
            }

            var skill = skillFactory();
            skill.Initialize(client, resolvedSkillId.Value, elementId);
        }
    }
}
