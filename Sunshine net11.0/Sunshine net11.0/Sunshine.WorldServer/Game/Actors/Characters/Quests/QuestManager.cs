using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.MySql.Database.World.Characters.Quests;
using Sunshine.MySql.Database.World.Quests;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Characters.Quests
{
    public class QuestManager : Singleton<QuestManager>
    {
        public Dictionary<short, QuestRecord> Quests = new Dictionary<short, QuestRecord>();

        public Dictionary<short, QuestStepRecord> QuestSteps = new Dictionary<short, QuestStepRecord>();

        public Dictionary<short, QuestObjectiveRecord> QuestObjectives = new Dictionary<short, QuestObjectiveRecord>();

        public Dictionary<short, List<QuestStepRecord>> QuestStepsByQuest = new Dictionary<short, List<QuestStepRecord>>();

        public QuestRecord GetQuest(short quest)
        {
            if (Quests.ContainsKey(quest))
                return Quests[quest];
            return null;
        }

        public QuestStepRecord GetQuestStep(short step)
        {
            if (QuestSteps.ContainsKey(step))
                return QuestSteps[step];
            return null;
        }

        public QuestObjectiveRecord GetQuestObjective(short objective)
        {
            if (QuestObjectives.ContainsKey(objective))
                return QuestObjectives[objective];
            return null;
        }

        public IEnumerable<QuestObjectiveRecord> GetAllQuestObjectivesByStepId(short stepId)
        {
            return QuestObjectives.Values.Where(x => x.Step == stepId);
        }

        public List<QuestStepRecord> GetAllStepsByQuestId(short questId)
        {
            if (QuestStepsByQuest.ContainsKey(questId))
                return QuestStepsByQuest[questId];
            return null;
        }

        public Dictionary<short, QuestRecord> GetAllQuests()
        {
            return DatabaseManager.Connection.Query<QuestRecord>($"SELECT * FROM quests").ToDictionary(x => x.Id);
        }

        public Dictionary<short, QuestStepRecord> GetAllQuestSteps()
        {
            return DatabaseManager.Connection.Query<QuestStepRecord>($"SELECT * FROM quests_steps").ToDictionary(x => x.Id);
        }

        public Dictionary<short, QuestObjectiveRecord> GetAllQuestObjectives()
        {
            return DatabaseManager.Connection.Query<QuestObjectiveRecord>($"SELECT * FROM quests_objectives").ToDictionary(x => x.Id);
        }      

        public Dictionary<short, List<QuestStepRecord>> GetAllQuestStepsByQuestId()
        {
            Dictionary<short, List<QuestStepRecord>> steps = new Dictionary<short, List<QuestStepRecord>>();

            var stepsQuest = DatabaseManager.Connection.Query<QuestStepRecord>($"SELECT * FROM quests_steps");
            foreach (var step in stepsQuest)
            {
                if (steps.ContainsKey(step.Quest))
                    steps[step.Quest].Add(step);
                else
                    steps.Add(step.Quest, new List<QuestStepRecord> { step });
            }
            return steps;
        }

        public bool ParseCriteria(string criteria, Character character)
        {
            #region Criteria
            if (criteria == null || criteria == "")
                return true;

            var critsOr = criteria.Split('|'); // OR   
            int total = 0;
            int countAnd = -1;

            foreach(var critOr in critsOr)
            {
                var critsAnd = critOr.Split('&'); // AND
                countAnd = critOr.Contains('&') ? critsAnd.Length : -1;
                total = 0;

                if (countAnd != -1)
                {
                    foreach (var critAnd in critsAnd)
                    {
                        var type = critAnd.Substring(0, 1);
                        var sign = critAnd.Substring(0, critAnd.IndexOf('='));

                        var value = short.Parse(critAnd);

                        switch (sign)
                        {
                            case "=":
                                switch (type)
                                {
                                    case "B":
                                        if (character.Breed == value)
                                            total++;
                                        break;
                                }
                                break;

                            case ">":
                                switch (type)
                                {
                                    case "B":
                                        if (character.Breed > value)
                                            total++;
                                        break;
                                }
                                break;

                            case "<":
                                switch (type)
                                {
                                    case "B":
                                        if (character.Breed < value)
                                            total++;
                                        break;
                                }
                                break;

                            case "<=":
                                switch (type)
                                {
                                    case "B":
                                        if (character.Breed <= value)
                                            total++;
                                        break;
                                }
                                break;

                            case ">=":
                                switch (type)
                                {
                                    case "B":
                                        if (character.Breed >= value)
                                            total++;
                                        break;
                                }
                                break;
                        }
                    }
                }

                if (countAnd != -1 && total >= countAnd)
                    return true;

                if (countAnd != -1 && total < countAnd)
                    return false;

                var type1 = critOr.Substring(0, 1);
                var sign1 = critOr.Remove(0, 1).Remove(critOr.IndexOf('='));
                var value1 = short.Parse(critOr.Remove(0, critOr.IndexOf('=') + 1));

                switch (sign1)
                {
                    case "=":
                        switch (type1)
                        {
                            case "B":
                                if (character.Breed == value1)
                                    return true;
                                break;
                        }
                        break;

                    case ">":
                        switch (type1)
                        {
                            case "B":
                                if (character.Breed > value1)
                                    return true;
                                break;
                        }
                        break;

                    case "<":
                        switch (type1)
                        {
                            case "B":
                                if (character.Breed < value1)
                                    return true;
                                break;
                        }
                        break;

                    case "<=":
                        switch (type1)
                        {
                            case "B":
                                if (character.Breed <= value1)
                                    return true;
                                break;
                        }
                        break;

                    case ">=":
                        switch (type1)
                        {
                            case "B":
                                if (character.Breed >= value1)
                                    return true;
                                break;
                        }
                        break;
                }

                if (critOr == critsOr.LastOrDefault())
                    return false;
            }


            return true;
            #endregion
        }

        public void VerifyQuest(Character character, QuestTypeEnum questType, object obj)
        {
            switch (questType)
            {
                case QuestTypeEnum.QUEST_TYPE_WIN_VS_MONSTER_ONE_FIGHT:
                case QuestTypeEnum.QUEST_TYPE_WIN_VS_MONSTER:
                    {
                        IEnumerable<FightActor> monstersFighter = obj as IEnumerable<FightActor>;
                        var objectives = character.Quests.GetQuestsObjectives(questType);
                        foreach (var objective in objectives)
                        {
                            var objec = this.GetQuestObjective(objective.Objective);
                            var monstersCSV = objec.ParametersCSV.Split(',').Select(x => int.Parse(x)).ToList();
                            for (int i = 0; i < monstersCSV.Count(); i += 2)
                            {
                                var monster = monstersCSV[i];
                                var quantity = monstersCSV[i + 1];
                                var fighters = monstersFighter.Where(x => (x as MonsterFighter).Monster.Record.Id == monster);
                                if (fighters.Count() >= quantity) // Verify if count monster >= objective quest
                                    character.Quests.UpdateObjective(objec.Step, objec.Id, true, monster == 2785 || monster == 2781);

                            }
                        }
                    }
                    break;
            }
        }
    }
}
