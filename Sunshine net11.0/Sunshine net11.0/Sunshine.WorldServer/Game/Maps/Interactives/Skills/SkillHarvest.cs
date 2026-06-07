using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Maps.Interactives;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using Sunshine.WorldServer.Handlers.Characters.Jobs;
using Sunshine.WorldServer.Handlers.Interactives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(42), SkillHandler(45), SkillHandler(46), SkillHandler(50), SkillHandler(52), SkillHandler(53), SkillHandler(54),
     SkillHandler(57), SkillHandler(58), SkillHandler(159), SkillHandler(191), SkillHandler(6), SkillHandler(10),
     SkillHandler(33), SkillHandler(34), SkillHandler(35), SkillHandler(37), SkillHandler(38), SkillHandler(39),
     SkillHandler(40), SkillHandler(41), SkillHandler(139), SkillHandler(141), SkillHandler(154), SkillHandler(155),
     SkillHandler(158), SkillHandler(174), SkillHandler(190), SkillHandler(24), SkillHandler(25), SkillHandler(26),
     SkillHandler(28), SkillHandler(29), SkillHandler(30), SkillHandler(31), SkillHandler(55), SkillHandler(56),
     SkillHandler(161), SkillHandler(162), SkillHandler(192), SkillHandler(68), SkillHandler(69), SkillHandler(71),
     SkillHandler(72), SkillHandler(73), SkillHandler(74), SkillHandler(160), SkillHandler(188), SkillHandler(124),
     SkillHandler(125), SkillHandler(126), SkillHandler(127), SkillHandler(128), SkillHandler(129), SkillHandler(130),
     SkillHandler(131), SkillHandler(136), SkillHandler(140), SkillHandler(189), SkillHandler(102)]

    public class SkillHarvest : Skill, IDialog
    {
        public override void Execute()
        {
            var outil = Client.Character.Map.Interactives.FirstOrDefault(x => x.Element == Element && x.GetStatedElement == null);
            if (outil != null)
            {
                var harvest = Client.Character.Map.Interactives.FirstOrDefault(x => x.GetStatedElement != null &&
                                                                            x.GetStatedElement.elementId == Element);

                Job = Client.Character.Jobs.GetJob((sbyte)outil.Parameters[0]);      
                
                if (harvest != null && Job != null)
                {
                    if (harvest.GetStatedElement.elementState == (int)InteractiveStateEnum.STATE_ANIMATED ||
                        harvest.GetObstacles.Any(x => x.state == (sbyte)MapObstacleStateEnum.OBSTACLE_OPENED))
                        return;

                    Level = ExperienceManager.Instance.GetJobLevelExperienceFloor(Job.Experience);

                    if (!JobManager.Instance.HasLevelToCollect(Id, Job, Level))
                    {
                        Client.Character.SendNotificationByServerMessage("Vous n'avez pas le niveau nécessaire", NotificationEnum.INFORMATION);
                        return;
                    }

                    double time = JobManager.Instance.GetTimeToCollect(Id, Job, Level);                
                    InteractiveHandler.SendInteractiveUsedMessage(Client.Character.Map.Clients, Client.Character, this, (short)((Timer.Interval = time) / 100));
                    for (int i = 0; i < harvest.Parameters.Count; i++)
                        harvest.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_OPENED;
                    harvest.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_ANIMATED;
                    Client.Character.Map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(harvest.GetStatedElement)));
                    //Client.Character.Map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(harvest.GetObstacles)));
                    Timer.Elapsed += (sender, e) => DeleteHarvest(harvest, e);
                    Timer.Start();
                }
                else
                    Client.Character.SendNotificationByServerMessage("Vous ne possédez pas le métier nécessaire", NotificationEnum.ERREUR);
            }
        }

        private void LootItem()
        {
            if (Job == null)
                return;

            List<InteractiveSkill> jobSkills;
            if (!JobManager.Instance.JobSkills.TryGetValue(Job.Job, out jobSkills) || jobSkills == null)
                return;

            var item = jobSkills.FirstOrDefault(x => x != null && x.Id == Id);
            if (item != null && item.GatheredRessourceItem != -1)
            {
                var ressource = ItemManager.Instance.CreatePlayerItem(item.GatheredRessourceItem);
                var quantity = JobManager.Instance.GetRandomQuantity(ressource, Level);

                if (Client?.Account?.Vip == true)
                    quantity = System.Math.Max(1, quantity * 2);

                if (Client.Character.Inventory.IsFull(ressource, quantity))
                {
                    Client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 144);
                    return;
                }

                Client.Character.Inventory.AddItem(ressource, quantity);
                JobHandler.SendObjectFoundWhileRecoltingMessage(Client, ressource.Template.Id, quantity);
                Client.Character.Jobs.AddExperience(Job.Job, JobManager.Instance.GetExperience(ressource));
            }
        }

        private void DeleteHarvest(object sender, ElapsedEventArgs e)
        {
            Timer.Dispose();
            var harvest = sender as Interactive;
            for (int i = 0; i < harvest.Parameters.Count; i++)
                harvest.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_OPENED;
            harvest.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_ACTIVATED;
            Client.Character.Map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(harvest.GetStatedElement)));
            //Client.Character.Map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(harvest.GetObstacles)));
            InteractiveHandler.SendInteractiveUseEndedMessage(Client, this);
            LootItem();
            Timer = new Timer(30000);
            Timer.Elapsed += (x, y) => RespawnHarvest(harvest, e);
            Timer.Start();
        }

        private void RespawnHarvest(object sender, ElapsedEventArgs e)
        {
            Timer.Dispose();
            var harvest = sender as Interactive;
            for (int i = 0; i < harvest.Parameters.Count; i++)
                harvest.GetObstacles[i].state = (sbyte)MapObstacleStateEnum.OBSTACLE_CLOSED;
            harvest.GetStatedElement.elementState = (int)InteractiveStateEnum.STATE_NORMAL;
            Client.Character.Map.Clients.ForEach(x => x.Send(new StatedElementUpdatedMessage(harvest.GetStatedElement)));
            //Client.Character.Map.Clients.ForEach(x => x.Send(new MapObstacleUpdateMessage(harvest.GetObstacles)));
        }
    }
}
