using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.WorldServer.Game.Actors.Npcs.Actions;
using Sunshine.WorldServer.Game.Actors.Characters.Quests;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Prisms;
using Sunshine.WorldServer.Game.Maps.Houses;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Game.Maps.PaddockInstances;
using Sunshine.WorldServer.Game.Maps.Interactives;
using Sunshine.WorldServer.Handlers.Houses;
using Sunshine.WorldServer.Handlers.Mounts;
using Sunshine.WorldServer.Handlers.Paddocks;
using Sunshine.WorldServer.Handlers.Dialogs;
using Sunshine.Logs;

namespace Sunshine.WorldServer.Handlers.Context.Roleplay
{
    public class ContextRoleplayHandler : WorldPacketHandler
    {
        [WorldHandler(225)]
        public static void HandleMapInformationsRequestMessage(WorldClient client, MapInformationsRequestMessage message)
        {
            SendMapComplementaryInformationsDataMessage(client);
            client.Send(new MapFightCountMessage((short)client.Character.Map.Fights.Count));
        }

        [WorldHandler(5610)]
        public static void HandleStatsUpgradeRequestMessage(WorldClient client, StatsUpgradeRequestMessage message)
        {
            StatsBoostTypeEnum statsBoost = (StatsBoostTypeEnum)message.statId;
            short boostPoint = message.boostPoint;
            int breedId = client.Character.Breed;
            short currentPoint = (short)client.Character.Stats[CharacterManager.Instance.StatsRelations[statsBoost]].Base;

            if (statsBoost < StatsBoostTypeEnum.Strength || statsBoost > StatsBoostTypeEnum.Intelligence)
            {
                Logs.Logger.WriteError("Wrong statsid");
                return;
            }

            if (boostPoint > 0 && client.Character.StatsPoints >= boostPoint)
            {
                client.Character.Stats[CharacterManager.Instance.StatsRelations[statsBoost]].Base +=
                    (short)BreedManager.Instance.SetStatsPoints(boostPoint, breedId, statsBoost, currentPoint);
                client.Character.StatsPoints -= boostPoint;
                client.Send(new StatsUpgradeResultMessage(message.boostPoint));
                client.Character.Stats.Update();
                client.Character.RefreshStats();
            }
            else
            {
                client.Character.SendServerMessage("Not enough statsPoint !");
            }
        }

        [WorldHandler(221)]
        public static void HandleChangeMapMessage(WorldClient client, ChangeMapMessage message)
        {
            client.Character.StopMove(false);

            if (client.Character.IsStartingMonsterFight || client.Character.IsInFight() || client.Character.Fighter != null)
                return;

            if (client?.Character == null || client.Character.Map == null)
                return;

            if (client.Character.IsInFight() || client.Character.Fighter != null || client.Character.IsStartingMonsterFight)
                return;

            if (client.Character.HasPendingMonsterFight())
                client.Character.ClearPendingMonsterFight();

            if (client.Character.Map.TopNeighbourId == message.mapId)
                client.Character.Cell.Id += 532;
            else if (client.Character.Map.BottomNeighbourId == message.mapId)
                client.Character.Cell.Id -= 532;
            else if (client.Character.Map.LeftNeighbourId == message.mapId)
                client.Character.Cell.Id += 13;
            else if (client.Character.Map.RightNeighbourId == message.mapId)
                client.Character.Cell.Id -= 13;

            Map map = MapManager.Instance.GetMap(message.mapId);
            if (map == null)
                return;

            if (map.IsBug())
            {
                string[] parameters = map.ParametersCSV.Split(',');
                int newMap = int.Parse(parameters[0]);
                client.Character.Teleport(newMap, client.Character.Cell.Id);
            }
            else
            {
                client.Character.Teleport((int)message.mapId, client.Character.Cell.Id);
            }
        }

        [WorldHandler(5731)]
        public static void HandleGameRolePlayPlayerFightRequestMessage(WorldClient client, GameRolePlayPlayerFightRequestMessage message)
        {
            Character target;
            if (!CharacterManager.Instance.Characters.TryGetValue(message.targetId, out target) || target == null)
                return;

            if (message.friendly)
            {
                FighterRefusedReasonEnum fighterRefusedReasonEnum = client.Character.CanRequestFight(target);
                if (fighterRefusedReasonEnum != FighterRefusedReasonEnum.FIGHTER_ACCEPTED)
                    ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, fighterRefusedReasonEnum);
                else
                    client.Character.SetFightRequest(FightTypeEnum.FIGHT_TYPE_CHALLENGE, target);
            }
            else
            {
                FighterRefusedReasonEnum fighterRefusedReasonEnum = client.Character.CanAgress(target);
                if (fighterRefusedReasonEnum != FighterRefusedReasonEnum.FIGHTER_ACCEPTED)
                {
                    ContextHandler.SendChallengeFightJoinRefusedMessage(client, client.Character, fighterRefusedReasonEnum);
                }
                else
                {
                    client.Character.SetFight(FightTypeEnum.FIGHT_TYPE_AGRESSION);
                    target.SetFight(FightTypeEnum.FIGHT_TYPE_AGRESSION, client.Character.Fight);

                    var fight = client.Character.Fight;
                    fight.AddFighter(client.Character.Fighter = new CharacterFighter(client.Character), true);
                    fight.AddFighter(target.Fighter = new CharacterFighter(target));
                }
            }
        }

        [WorldHandler(5732)]
        public static void HandleGameRolePlayPlayerFightFriendlyAnswerMessage(WorldClient client, GameRolePlayPlayerFightFriendlyAnswerMessage message)
        {
            Fight fight = client.Character.Fight;
            if (fight == null)
                return;

            if (message.accept)
            {
                SendGameRolePlayPlayerFightFriendlyAnsweredMessage(fight.Leader.Client, client.Character, fight.Leader, client.Character, true);
                fight.AddFighter(fight.Leader.Fighter = new CharacterFighter(fight.Leader), true);
                fight.AddFighter(client.Character.Fighter = new CharacterFighter(client.Character));
            }
            else
            {
                SendGameRolePlayPlayerFightFriendlyAnsweredMessage(client, client.Character, fight.Leader, client.Character, false);
                SendGameRolePlayPlayerFightFriendlyAnsweredMessage(fight.Leader.Client, client.Character, fight.Leader, client.Character, false);
                fight.Leader.Fight = null;
                client.Character.Fight = null;
                FightManager.Instance.RemoveFight(fight);
            }
        }

        [WorldHandler(5898)]
        public static void HandleNpcGenericActionRequestMessage(WorldClient client, NpcGenericActionRequestMessage message)
        {
            var npc = client.Character.Map.GetActor(message.npcId);

            if (npc != null)
            {
                if (npc is Npc)
                    ((Npc)npc).InteractWith((NpcActionTypeEnum)message.npcActionId, client.Character);
                else if (npc is TaxCollector)
                    ((TaxCollector)npc).InteractWith((NpcActionTypeEnum)message.npcActionId, client.Character);
            }
            else
            {
                if (NpcManager.Instance.Npcs.ContainsKey(client.Character.Map.Id))
                    NpcManager.Instance.Npcs[client.Character.Map.Id].First().InteractWith((NpcActionTypeEnum)message.npcActionId, client.Character);
            }
        }

        [WorldHandler(5616)]
        public static void HandleNpcDialogReplyMessage(WorldClient client, NpcDialogReplyMessage message)
        {
            if (!client.Character.IsInDialog() || !(client.Character.Dialog is NpcTalkAction talk))
            {
                Logger.WriteWarning($"[NpcReplyRaw] charId={client.Character?.Id} clientReplyId={message.replyId} dialogId=? result=NoDialog");
                return;
            }

            Logger.WriteInfo($"[NpcReplyRaw] charId={client.Character.Id} npcId={talk.Npc?.Record?.Id} dialogId={talk.CurrentMessageId} clientReplyId={message.replyId} packet=5616 result=Received");
            talk.ChangeMessage(message.replyId);
        }

        public static void SendNpcDialogCreationMessage(WorldClient client, Npc npc)
        {
            Logger.WriteInfo($"[NpcDialog] charId={client.Character.Id} npcId={npc.Record.Id} mapId={client.Character.Map.Id} dialogId=open");
            client.Send(new NpcDialogCreationMessage(client.Character.Map.Id, npc.Id));
        }

        public static void SendNpcDialogQuestionMessage(WorldClient client, Npc npc)
        {
            switch (npc.Record.Id)
            {
                case 1222:
                    switch (client.Character.Map.Id)
                    {
                        case 35652610:
                            client.Character.Quests.UpdateObjective(1052, 3512, true, true);
                            break;
                    }
                    break;
            }

            if (npc.GetDialogRepliesId.Count <= 0)
            {
                Logger.WriteWarning($"[NpcDialog] charId={client.Character.Id} npcId={npc.Record.Id} mapId={client.Character.Map.Id} dialogId={npc.GetFirstDialogMessageId()} replies=0 result=AutoClose");
                DialogHandler.SendLeaveDialogMessage(client);
                return;
            }

            var firstMessage = npc.GetFirstDialogMessageId();
            var visibleReplies = npc.GetDialogReplies(firstMessage);
            var dialogParams = npc.GetDialogParameters(client.Character, firstMessage).ToList();
            var paramsDialogs = string.Empty;
            string[] splitParams = new string[0];

            if (!npc.Record.HasQuest)
            {
                Logger.WriteInfo($"[NpcDialog] charId={client.Character.Id} npcId={npc.Record.Id} mapId={client.Character.Map.Id} dialogId={firstMessage}");
                client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, visibleReplies));
            }
            else
            {
                var paramsQuests = npc.GetNpcTypes.Where(x => x == 5);
                for (int i = 0; i < paramsQuests.Count(); i++)
                {
                    var paramQuest = paramsQuests.ElementAt(i);
                    short questId = short.Parse(npc.GetParameters[npc.GetNpcTypes.IndexOf(paramQuest)].Split(',')[0]);

                    if (!client.Character.Quests.HasQuest(questId))
                    {
                        client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, visibleReplies));
                        break;
                    }
                    else
                    {
                        dialogParams.Clear();
                        int indexObjective = npc.GetNpcTypes.IndexOf(paramQuest);
                        short stepId = short.Parse(npc.GetParameters[indexObjective].Split(',')[1]);
                        short objectiveId = short.Parse(npc.GetParameters[indexObjective].Split(',')[2]);

                        var objective = client.Character.Quests.GetQuestbjectives(stepId, objectiveId);
                        var step = client.Character.Quests.GetQuestStep(questId, stepId);
                        var quest = client.Character.Quests.GetQuest(questId);
                        int dialogs = 0;
                        int indexD = 0;

                        if (quest.isValided)
                        {
                            if (paramsQuests.Count() <= 1)
                            {
                                dialogs = npc.GetNpcTypes.Where(x => x == -3).ElementAt(i);
                                indexD = npc.GetNpcTypes.IndexOf(dialogs);

                                if (npc.GetDialogMessagesId.Count > npc.GetAllDialogs.Count)
                                    indexD--;

                                firstMessage = npc.GetAllDialogs.ElementAt(indexD).Key;
                                paramsDialogs = npc.GetDialogParams[indexD];

                                if (paramsDialogs != "")
                                {
                                    splitParams = paramsDialogs.Split(',');

                                    foreach (var param in splitParams)
                                    {
                                        switch (param)
                                        {
                                            case "N":
                                                dialogParams.Add(client.Character.Name);
                                                break;

                                            case "L":
                                                dialogParams.Add(client.Character.Level.ToString());
                                                break;
                                        }
                                    }
                                }

                                client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, new short[0]));
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (objective == null)
                        {
                            dialogs = npc.GetNpcTypes.Where(x => x == -1).ElementAt(i);
                            indexD = npc.GetNpcTypes.IndexOf(dialogs);

                            if (npc.GetDialogMessagesId.Count > npc.GetAllDialogs.Count)
                                indexD--;

                            firstMessage = npc.GetAllDialogs.ElementAt(indexD).Key;
                            paramsDialogs = npc.GetDialogParams[indexD];

                            if (paramsDialogs != "")
                            {
                                splitParams = paramsDialogs.Split(',');

                                foreach (var param in splitParams)
                                {
                                    switch (param)
                                    {
                                        case "N":
                                            dialogParams.Add(client.Character.Name);
                                            break;

                                        case "L":
                                            dialogParams.Add(client.Character.Level.ToString());
                                            break;
                                    }
                                }
                            }

                            client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, new short[0]));
                            break;
                        }

                        var allObjectives = QuestManager.Instance.GetAllQuestObjectivesByStepId(stepId);
                        bool isLastObjective = allObjectives.Last().Id == objective.Objective;

                        if (!objective.IsFinished && objective.Type != 1 && objective.Type != 3 && objective.Type != 12)
                        {
                            dialogs = npc.GetNpcTypes.Where(x => x == -1).ElementAt(i);
                            indexD = npc.GetNpcTypes.IndexOf(dialogs);

                            if (npc.GetDialogMessagesId.Count > npc.GetAllDialogs.Count)
                                indexD--;

                            firstMessage = npc.GetAllDialogs.ElementAt(indexD).Key;
                            paramsDialogs = npc.GetDialogParams[indexD];

                            if (paramsDialogs != "")
                            {
                                splitParams = paramsDialogs.Split(',');

                                foreach (var param in splitParams)
                                {
                                    switch (param)
                                    {
                                        case "N":
                                            dialogParams.Add(client.Character.Name);
                                            break;

                                        case "L":
                                            dialogParams.Add(client.Character.Level.ToString());
                                            break;
                                    }
                                }
                            }

                            client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, new short[0]));
                            break;
                        }
                        else
                        {
                            if (!objective.IsValided)
                            {
                                dialogs = npc.GetNpcTypes.Where(x => x == -2).ElementAt(i);
                                indexD = npc.GetNpcTypes.IndexOf(dialogs);

                                if (npc.GetDialogMessagesId.Count > npc.GetAllDialogs.Count)
                                    indexD--;

                                firstMessage = npc.GetAllDialogs.ElementAt(indexD).Key;

                                switch (objective.Type)
                                {
                                    case 1:
                                        client.Character.Quests.UpdateObjective(objective.Step, objective.Objective, true);
                                        break;

                                    case 3:
                                    case 12:
                                        client.Character.Quests.UpdateObjective(objective, npc);
                                        break;
                                }

                                paramsDialogs = npc.GetDialogParams[indexD];

                                if (paramsDialogs != "")
                                {
                                    splitParams = paramsDialogs.Split(',');

                                    foreach (var param in splitParams)
                                    {
                                        switch (param)
                                        {
                                            case "N":
                                                dialogParams.Add(client.Character.Name);
                                                break;

                                            case "L":
                                                dialogParams.Add(client.Character.Level.ToString());
                                                break;
                                        }
                                    }
                                }

                                client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, new short[0]));
                                break;
                            }
                            else
                            {
                                if (isLastObjective)
                                {
                                    var allSteps = QuestManager.Instance.GetAllStepsByQuestId(questId);
                                    bool isLastStep = allSteps.Last().Id == stepId;

                                    if (!isLastStep)
                                        dialogs = npc.GetNpcTypes.Where(x => x == -4).ElementAt(i);
                                    else
                                        dialogs = npc.GetNpcTypes.Where(x => x == -5).ElementAt(i);

                                    indexD = npc.GetNpcTypes.IndexOf(dialogs);

                                    if (npc.GetDialogMessagesId.Count > npc.GetAllDialogs.Count)
                                        indexD--;

                                    firstMessage = npc.GetAllDialogs.ElementAt(indexD).Key;
                                    paramsDialogs = npc.GetDialogParams[indexD];

                                    if (paramsDialogs != "")
                                    {
                                        splitParams = paramsDialogs.Split(',');

                                        foreach (var param in splitParams)
                                        {
                                            switch (param)
                                            {
                                                case "N":
                                                    dialogParams.Add(client.Character.Name);
                                                    break;

                                                case "L":
                                                    dialogParams.Add(client.Character.Level.ToString());
                                                    break;
                                            }
                                        }
                                    }

                                    client.Send(new NpcDialogQuestionMessage(firstMessage, dialogParams, new short[0]));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SendNpcDialogQuestionMessage(WorldClient client, short messageId, IEnumerable<string> dialogs, IEnumerable<short> replies)
        {
            if (client?.Character != null)
            {
                var npcId = client.Character.Dialog is NpcTalkAction talk ? talk.Npc.Record.Id : 0;
                Logger.WriteInfo($"[NpcDialog] charId={client.Character.Id} npcId={npcId} mapId={client.Character.Map.Id} dialogId={messageId}");
            }

            client.Send(new NpcDialogQuestionMessage(messageId, dialogs, replies));
        }

        [WorldHandler(5685u)]
        public static void HandleEmotePlayRequestMessage(WorldClient client, EmotePlayRequestMessage message)
        {
            client.Character.PlayEmote((EmotesEnum)message.emoteId);
        }

        public static void SendEmotePlayMessage(List<WorldClient> clients, Character character, EmotesEnum emote)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Send(new EmotePlayMessage((sbyte)emote, DateTime.Now.GetUnixTimeStampByte(), character.Id, character.Account.Id));
        }

        public static void SendEmotePlayMessage(WorldClient client, RolePlayActor actor, EmotesEnum emote)
        {
            client.Send(new EmotePlayMessage((sbyte)emote, DateTime.Now.GetUnixTimeStampByte(), actor.Id, 0));
        }

        public static void SendEmoteListMessage(WorldClient client)
        {
            client.Send(new EmoteListMessage(new List<sbyte> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 21, 22, 23, 24 }));
        }

        public static void SendCurrentMapMessage(WorldClient client, int mapid)
        {
            client.Send(new CurrentMapMessage(mapid));
        }

        private static IEnumerable<GameRolePlayActorInformations> GetVisibleRolePlayActors(WorldClient client, Map map)
        {
            PrismManager.Instance.EnsureMapPrismState(map);

            var visibleActors = map.RolePlayActors
                .Where(x =>
                {
                    if (!(x is PrismActor prismActor))
                        return true;

                    return PrismManager.Instance.IsAuthoritativePrismActor(map, prismActor);
                })
                .Select(x => x is Npc ? ((Npc)x).GetGameRolePlayNpcInformations(client) : x.GetGameRolePlayActorInformations())
                .ToList();

            visibleActors.AddRange(MountHandler.GetPublicPaddockRolePlayActors(client));
            return visibleActors;
        }

        public static void SendMapComplementaryInformationsDataMessage(WorldClient client)
        {
            var map = client.Character.Map;
            PrismManager.Instance.EnsureMapPrismState(map);
            var obstacles = new List<MapObstacle>();

            foreach (var obstcl in map.Interactives.Select(x => x.GetObstacles))
                obstcl.ForEach(x => obstacles.Add(x));

            var visibleInteractives = map.Interactives.Select(x => HouseInteractiveBuilder.BuildForClient(x, client)).Where(x => x != null).ToArray();
            var statedElements = map.Interactives.Where(x => x.GetStatedElement != null).Select(x => x.GetStatedElement).ToArray();
            var placementFights = map.Fights.Where(x => x != null && x.State == FightStateEnum.Placement).Select(x => x.GetFightCommonInformations).ToArray();

            var currentHouse = HouseManager.Instance.ResolveInteriorHouse(client.Character);
            if (currentHouse != null)
            {
                client.Send(new MapComplementaryInformationsDataInHouseMessage(
                    (short)map.SubAreaId,
                    map.Id,
                    0,
                    HouseManager.Instance.GetHousesInformationsByMap(map.Id, client),
                    GetVisibleRolePlayActors(client, map),
                    visibleInteractives,
                    statedElements,
                    obstacles,
                    placementFights,
                    currentHouse.GetInformationsInside()));
            }
            else if (client.Character.LastTargetedPaddockInstance != null &&
                     client.Character.LastTargetedPaddockInstance.ContainsInteriorMap(map.Id))
            {
                var exteriorPoint = client.Character.LastTargetedPaddockInstance.Map != null
                    ? client.Character.LastTargetedPaddockInstance.Map.Point
                    : map.Point;

                client.Send(new MapComplementaryInformationsWithCoordsMessage(
                    (short)map.SubAreaId,
                    map.Id,
                    0,
                    HouseManager.Instance.GetHousesInformationsByMap(map.Id, client),
                    GetVisibleRolePlayActors(client, map),
                    visibleInteractives,
                    statedElements,
                    obstacles,
                    placementFights,
                    exteriorPoint != null ? (short)exteriorPoint.X : (short)0,
                    exteriorPoint != null ? (short)exteriorPoint.Y : (short)0));
            }
            else
            {
                client.Send(new MapComplementaryInformationsDataMessage(
                    (short)map.SubAreaId,
                    map.Id,
                    0,
                    HouseManager.Instance.GetHousesInformationsByMap(map.Id, client),
                    GetVisibleRolePlayActors(client, map),
                    visibleInteractives,
                    statedElements,
                    obstacles,
                    placementFights));
            }

            client.Character.EnterMap(map);

            var currentPaddock = PaddockManager.Instance.GetPaddockByMap(map.Id);
            if (currentPaddock != null)
                PaddockHandler.SendPaddockPropertiesMessage(client, currentPaddock);

            MountHandler.SyncPublicPaddockMountVisuals(client, true);

            HouseHandler.TrySendInsideHousePanel(client);
        }

        public static void SendGameRolePlayShowActorMessage(WorldClient client, RolePlayActor actor)
        {
            if (actor is PrismActor prismActor)
            {
                var map = client.Character != null ? client.Character.Map : null;
                if (!PrismManager.Instance.IsAuthoritativePrismActor(map, prismActor))
                    return;

            }

            if (actor is Npc)
                client.Send(new GameRolePlayShowActorMessage(((Npc)actor).GetGameRolePlayNpcInformations(client)));
            else
                client.Send(new GameRolePlayShowActorMessage(actor.GetGameRolePlayActorInformations()));
        }

        public static void SendGameRolePlayPlayerFightFriendlyRequestedMessage(WorldClient client, Character requester, Character source, Character target)
        {
            client.Send(new GameRolePlayPlayerFightFriendlyRequestedMessage(requester.Id, source.Id, target.Id));
        }

        public static void SendGameRolePlayPlayerFightFriendlyAnsweredMessage(WorldClient client, Character replier, Character source, Character target, bool accepted)
        {
            client.Send(new GameRolePlayPlayerFightFriendlyAnsweredMessage(replier.Id, source.Id, target.Id, accepted));
        }
    }
}