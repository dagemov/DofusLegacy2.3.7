using Sunshine.WorldServer.Game.Actors.Characters.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Loaders.World.Quests
{
    public static class QuestsLoader
    {
        public static void Initialize()
        {
            var quests = QuestManager.Instance.GetAllQuests();
            var questSteps = QuestManager.Instance.GetAllQuestSteps();
            var questObjectives = QuestManager.Instance.GetAllQuestObjectives();
            var questStepsByQuest = QuestManager.Instance.GetAllQuestStepsByQuestId();

            QuestManager.Instance.Quests = quests;
            QuestManager.Instance.QuestSteps = questSteps;
            QuestManager.Instance.QuestObjectives = questObjectives;
            QuestManager.Instance.QuestStepsByQuest = questStepsByQuest;
        }
    }
}
