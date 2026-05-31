using Sunshine.Protocol.Messages;
using Sunshine.WorldServer;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Characters.Quests;
using Sunshine.WorldServer.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Characters.Quests
{
    public class QuestHandler : WorldPacketHandler
    {
        [WorldHandler(5623)]
        public static void HandleQuestListRequestMessage(WorldClient client, QuestListRequestMessage message)
        {
            client.Send(new QuestListMessage(client.Character.Quests.GetFinishedQuests().Select(x => x.Quest), 
                client.Character.Quests.GetActivedQuests().Select(x => x.Quest)));
        }

        [WorldHandler(5622)]
        public static void HandleQuestStepInfoRequestMessage(WorldClient client, QuestStepInfoRequestMessage message)
        {
            client.Character.Quests.GetQuestStepInfos((short)message.questId);
        }

        [WorldHandler(6085)]
        public static void HandleQuestObjectiveValidationMessage(WorldClient client, QuestObjectiveValidationMessage message)
        {
            if (client.Character.Quests.HasQuest(message.questId))
            {
                if (!QuestManager.Instance.QuestObjectives.ContainsKey(message.objectiveId))
                    return;

                var stepId = QuestManager.Instance.QuestObjectives[message.objectiveId].Step;

                client.Character.Quests.UpdateObjective(stepId, message.objectiveId, true, message.questId == 489);
            }
        }
    }
}
