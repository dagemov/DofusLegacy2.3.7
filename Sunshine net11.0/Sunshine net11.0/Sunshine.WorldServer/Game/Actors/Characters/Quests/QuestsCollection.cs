using Sunshine.MySql.Database.Managers;
using Sunshine.BaseServer.Configuration;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Client;
using Sunshine.Protocol.Enums;
using Sunshine.MySql.Database.World.Characters.Quests;
using Sunshine.MySql.Database.World.Quests;

namespace Sunshine.WorldServer.Game.Actors.Characters.Quests
{
    public class QuestsCollection
    {
        private Character _character;
        private Dictionary<short, CharacterQuestRecord> _quests; // by quest id
        private Dictionary<short, List<CharacterQuestStepRecord>> _steps; // by quest id
        private Dictionary<short, List<CharacterQuestObjectiveRecord>> _objectives; // by step id


        public QuestsCollection(Character character)
        {
            _character = character;
            _quests = CharacterManager.Instance.GetCharacterQuests(character.Id).ToDictionary(x => x.Quest);
            _steps = CharacterManager.Instance.GetCharacterQuestSteps(character.Id);
            _objectives = CharacterManager.Instance.GetCharacterQuestObjectives(character.Id);
        }

        public CharacterQuestRecord GetQuest(short id)
        {
            return _quests.ContainsKey(id) ? _quests[id] : null;
        }

        public IEnumerable<CharacterQuestRecord> GetQuests()
        {
            return _quests.Values;
        }

        public IEnumerable<CharacterQuestStepRecord> GetQuestsSteps(short questId)
        {
            return _steps[questId];
        }

        public IEnumerable<CharacterQuestObjectiveRecord> GetQuestsObjectives()
        {
            List<CharacterQuestObjectiveRecord> objs = new List<CharacterQuestObjectiveRecord>();
            foreach (var steps in _objectives.Values)
                steps.ForEach(x => objs.Add(x));
            return objs;
        }

        public CharacterQuestObjectiveRecord GetQuestbjectives(short stepId, short objectiveId)
        {
            return _objectives[stepId].FirstOrDefault(x => x.Objective == objectiveId);
        }

        public IEnumerable<CharacterQuestObjectiveRecord> GetQuestsObjectives(short stepId)
        {
            return _objectives[stepId];
        }

        public IEnumerable<CharacterQuestObjectiveRecord> GetQuestsObjectives(short stepId, bool finished = false, bool valided = false)
        {
            return _objectives[stepId].Where(x => x.IsFinished && x.IsValided == valided);
        }

        public CharacterQuestStepRecord GetQuestStep(short questId, short stepId)
        {
            return _steps[questId].FirstOrDefault(x => x.Step == stepId);
        }

        public IEnumerable<CharacterQuestObjectiveRecord> GetQuestsObjectives(QuestTypeEnum type, bool isFinished = false)
        {
            List<CharacterQuestObjectiveRecord> objs = new List<CharacterQuestObjectiveRecord>();
            foreach (var steps in _objectives.Values)
            {
               foreach(var objective in steps)
                {
                    if (objective.Type == (short)type && objective.IsFinished == isFinished)
                        objs.Add(objective);
                }
            }
            return objs;
        }

        public IEnumerable<CharacterQuestRecord> GetFinishedQuests()
        {
            return _quests.Values.Where(x => x.IsFinished);
        }

        public IEnumerable<CharacterQuestRecord> GetActivedQuests()
        {
            return _quests.Values.Where(x => !x.IsFinished);
        }

        public bool HasQuest(short questId)
        {
            return _quests.ContainsKey(questId);
        }

        public void GetQuestStepInfos(short questId)
        {
            var quest = QuestManager.Instance.GetQuest(questId);
            if (quest != null)
            {

                var characterQuest = _character.Quests.GetQuest(questId);
                if (characterQuest != null)
                {
                    if (quest.StepIdsCSV == null || quest.StepIdsCSV == "")
                        _character.Client.Send(new QuestStepNoInfoMessage(questId));
                    else
                    {
                        short stepId = _steps[questId].LastOrDefault().Step;
                        var objectives = GetQuestsObjectives(stepId);
                        _character.Client.Send(new QuestStepInfoMessage(questId, stepId, objectives.Select(x => x.Objective), objectives.Select(x => !x.IsValided && !x.IsFinished)));
                    }
                }
            }
        }

        public void AddQuest(int npc, short questId)
        {
            var quest = QuestManager.Instance.GetQuest(questId);
            if (quest != null)
            {
                short stepId = short.Parse(quest.StepIdsCSV);
                var questStep = QuestManager.Instance.GetQuestStep(stepId);
                var questObjectives = QuestManager.Instance.GetAllQuestObjectivesByStepId(stepId);
                QuestObjectiveRecord objective = null;

                foreach(var questObj in questObjectives)
                {
                    if (questObj.Criteria != null && questObj.Criteria != "")
                    {
                        if (QuestManager.Instance.ParseCriteria(questObj.Criteria, _character))
                        {
                            objective = questObj;
                            break;
                        }
                    }
                    else
                    {
                        objective = questObj;
                        break;
                    }

                }

                _quests.Add(questId, new CharacterQuestRecord { OwnerId = _character.Id, Quest = questId, isValided = false, IsFinished = false });

                if (_steps.ContainsKey(questId))
                    _steps[questId].Add(new CharacterQuestStepRecord { OwnerId = _character.Id, Quest = questId, Step = stepId, isValided = false, IsFinished = false });
                else
                    _steps.Add(questId, new List<CharacterQuestStepRecord> { new CharacterQuestStepRecord { OwnerId = _character.Id, Quest = questId, Step = stepId, isValided = false, IsFinished = false } });

                _objectives.Add(stepId, new List<CharacterQuestObjectiveRecord> {  new CharacterQuestObjectiveRecord { OwnerId = _character.Id, Step = stepId, Objective = objective.Id,
                                                                                    Type = (short)objective.Type,  IsFinished = false, IsValided = false} });

                var npcsIds = _character.Map.RolePlayActors.Where(x => x is Npc && (x as Npc).Record.HasQuest).Select(x => (x as Npc).Id);
                _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 54, new object[] { questId });
                _character.Client.Send(new QuestStartedMessage((ushort)questId));
                _character.Client.Send(new QuestStepStartedMessage((ushort)questId, (ushort)stepId));
                _character.Client.Send(new MapNpcsQuestStatusUpdateMessage(_character.Map.Id, npcsIds.Where(x => x != npc), npcsIds));
            }
        }

        public void AddQuest(short questId)
        {
            var quest = QuestManager.Instance.GetQuest(questId);
            if (quest != null)
            {
                short stepId = short.Parse(quest.StepIdsCSV.Split(',')[0]);
                var questStep = QuestManager.Instance.GetQuestStep(stepId);
                var questObjectives = QuestManager.Instance.GetAllQuestObjectivesByStepId(stepId);
                QuestObjectiveRecord objective = null;

                foreach (var questObj in questObjectives)
                {
                    if (questObj.Criteria != null && questObj.Criteria != "")
                    {
                        if (QuestManager.Instance.ParseCriteria(questObj.Criteria, _character))
                        {
                            objective = questObj;
                            break;
                        }
                    }
                    else
                    {
                        objective = questObj;
                        break;
                    }

                }

                _quests.Add(questId, new CharacterQuestRecord { OwnerId = _character.Id, Quest = questId, isValided = false, IsFinished = false });

                if (_steps.ContainsKey(questId))
                    _steps[questId].Add(new CharacterQuestStepRecord { OwnerId = _character.Id, Quest = questId, Step = stepId, isValided = false, IsFinished = false });
                else
                    _steps.Add(questId, new List<CharacterQuestStepRecord> { new CharacterQuestStepRecord { OwnerId = _character.Id, Quest = questId, Step = stepId, isValided = false, IsFinished = false } });

                _objectives.Add(stepId, new List<CharacterQuestObjectiveRecord> {  new CharacterQuestObjectiveRecord { OwnerId = _character.Id, Step = stepId, Objective = objective.Id,
                                                                                    Type = (short)objective.Type,  IsFinished = false, IsValided = false} });

                var npcsIds = _character.Map.RolePlayActors.Where(x => x is Npc && (x as Npc).Record.HasQuest).Select(x => (x as Npc).Id);
                _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 54, new object[] { questId });
                _character.Client.Send(new QuestStartedMessage((ushort)questId));
                _character.Client.Send(new QuestStepStartedMessage((ushort)questId, (ushort)stepId));
            }
        }

        public void UpdateObjective(short stepId, short objectiveId, bool isValided = false, bool isTuto = false)
        {
            var objectiveFinished = GetQuestbjectives(stepId, objectiveId);

            if (objectiveFinished != null && objectiveFinished.IsValided)
                return;

            var step = QuestManager.Instance.QuestSteps[stepId];
            var objectives = step.ObjectiveIdsCSV.Split(',');

            var objective = _objectives[stepId].FirstOrDefault(x => x.Objective == objectiveId); // update simple

            if (objective != null)
            {
                int indexObjective = _objectives[stepId].IndexOf(objective);
                objective.IsFinished = true;
                objective.IsValided = isValided;
                _objectives[stepId][indexObjective] = objective;

                if (objectives.Last() == objective.Objective.ToString()) // step finised no valided
                {
                    if (isValided)
                    {
                        _character.Client.Send(new QuestObjectiveValidatedMessage((ushort)step.Quest, (ushort)objectiveId));

                        var allSteps = QuestManager.Instance.GetQuest(step.Quest).StepIdsCSV.Split(',').ToList(); // check if last step
                        if (allSteps.Last() == step.Id.ToString())
                        {
                            var stepQuest = _steps[step.Quest].FirstOrDefault(x => x.Step == step.Id);
                            if (stepQuest != null)
                                UpdateStep(step.Quest, step.Id, 0, stepQuest);
                        }
                        else
                        {
                            int indexStep = allSteps.IndexOf(step.Id.ToString());
                            short nexStep = short.Parse(allSteps[indexStep + 1]);
                            UpdateStep(step.Quest, step.Id, nexStep, null, true);

                            if (isTuto)
                            {
                                var allObjectives = QuestManager.Instance.GetQuestStep(nexStep).ObjectiveIdsCSV.Split(',').ToList();
                                short newObjective = short.Parse(allObjectives[0]);
                                QuestObjectiveRecord newObjectiveRecord = QuestManager.Instance.GetQuestObjective(newObjective);
                                var type = newObjectiveRecord.Type;
                                _objectives.Add(nexStep, new List<CharacterQuestObjectiveRecord> {  new CharacterQuestObjectiveRecord { OwnerId = _character.Id, Step = nexStep, Objective = newObjective,
                                                                                    Type = (short)type,  IsFinished = false, IsValided = false} });
                                AddStepRewards(step.Id);

                                if (step.Id == 1052)
                                    this.AddQuest(492);
                            }
                        }
                    }
                    else
                        _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 55, step.Quest); // update    
                }
                else
                {
                    var allObjectives = QuestManager.Instance.GetQuestStep(stepId).ObjectiveIdsCSV.Split(',').ToList();
                    int indexOfObjectivesStep = allObjectives.IndexOf(objectiveId.ToString());
                    short newObjective = short.Parse(allObjectives[indexOfObjectivesStep + 1]);
                    QuestObjectiveRecord newObjectiveRecord = null;

                    for (int i = indexOfObjectivesStep + 1; i < allObjectives.Count; i++)
                    {
                        newObjective = short.Parse(allObjectives[i]);
                        newObjectiveRecord = QuestManager.Instance.GetQuestObjective(newObjective);
                        if (QuestManager.Instance.ParseCriteria(newObjectiveRecord.Criteria, _character))
                        {
                            newObjective = short.Parse(allObjectives[i]);
                            break;
                        }
                    }

                    var type = newObjectiveRecord.Type;
                    _objectives[stepId].Add(new CharacterQuestObjectiveRecord { OwnerId = _character.Id, Step = stepId, Objective = newObjective, Type = (short)type, IsFinished = false, IsValided = false });
                    _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 55, step.Quest); // update 
                }
            }
        }

        public void UpdateObjective(CharacterQuestObjectiveRecord obj, Npc npc, bool isFinished = true, bool isValided = true) // Bring items to pnj
        {
            var objective = QuestManager.Instance.GetQuestObjective(obj.Objective);
            var paramsObj = objective.ParametersCSV.Split(',');

            if (paramsObj.Length >= 3)
            {
                short npcId = short.Parse(paramsObj[0]);
                if (npc.Record.Id == npcId)
                {
                    int itemGUID = int.Parse(paramsObj[1]);
                    int quantity = int.Parse(paramsObj[2]);

                    var item = _character.Inventory.GetItem(itemGUID);
                    if (item != null)
                    {
                        if (item.Stack >= quantity)
                        {
                            _character.Inventory.RemoveItem(item, quantity);
                            UpdateObjective(obj.Step, obj.Objective, true);
                        }
                        else
                            _character.SendServerMessage("Tu ne possèdes pas la quantité nécessaire"); // By romaain Ventura xD pour le "la" quantité
                    }
                    else
                        _character.SendServerMessage("Tu ne possèdes pas l'item requis");

                }
                else
                    _character.SendNotificationByServerMessage(string.Format("Il faut ramener les ressources au pnj {0}", npc.Record.Name), NotificationEnum.INFORMATION);
            }
        }

        private void UpdateStep(short questId, short stepId, short nextStepId, CharacterQuestStepRecord stepRecord, bool isNextStep = false)
        {
            if (!isNextStep)
            {
                int indexStep = _steps[questId].IndexOf(stepRecord);
                stepRecord.IsFinished = true;
                stepRecord.isValided = true;
                _steps[questId][indexStep] = stepRecord;
                _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 56, questId); // finish quest
                _character.Client.Send(new QuestStepValidatedMessage((ushort)questId, (ushort)stepId));
                EndQuest(questId, stepId);
                AddStepRewards(stepId);
            }
            else
            {
                _steps[questId].Add(new CharacterQuestStepRecord { OwnerId = _character.Id, Quest = questId, Step = nextStepId, isValided = false, IsFinished = false });
                _character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 55, questId); // update 
                _character.Client.Send(new QuestStepValidatedMessage((ushort)questId, (ushort)stepId));
                _character.Client.Send(new QuestStepStartedMessage((ushort)questId, (ushort)nextStepId));
            }
        }

        private void EndQuest(short questId, short stepId, bool isValided = true)
        {
            CharacterQuestRecord quest = _quests[questId];
            quest.IsFinished = true;
            quest.isValided = isValided;
            _quests[questId] = quest;

            _character.Client.Send(new QuestValidatedMessage((ushort)questId));

            if (questId == 492 && isValided)
                UpdateObjective(1061, 3531, true, true);
        }

        private void AddStepRewards(short stepId)
        {
            var step = QuestManager.Instance.GetQuestStep(stepId);

            _character.Inventory.SetKamas(GameRates.ApplyKamas(step.KamasReward));
            _character.AddExperience(GameRates.ApplyXp(step.ExperienceReward));

            if (step.ItemsRewardCSV != "")
            {
                var allItemsQuest = step.ItemsRewardCSV.Split(';');
                foreach (var itemsQuest in allItemsQuest)
                {
                    var items = itemsQuest.Split(',');
                    for (int i = 0; i < items.Length; i += 2)
                    {
                        var item = ItemManager.Instance.CreatePlayerItem(int.Parse(items[i]), int.Parse(items[i + i]));
                        _character.Inventory.AddItem(item);
                    }
                }
            }

            if (step.JobsRewardCSV != "")
            {
                var allJobsQuest = step.JobsRewardCSV.Split(',');
                foreach (var job in allJobsQuest)
                    _character.Jobs.AddJob(sbyte.Parse(job));
            }

            if (step.SpellsRewardCSV != "")
            {
                var allSpellsQuest = step.SpellsRewardCSV.Split(',');
                foreach (var spell in allSpellsQuest)
                    _character.Spells.LearnSpell(short.Parse(spell));
            }
        }
    }
}

