using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Interactives;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(1), SkillHandler(63), SkillHandler(64), SkillHandler(113), SkillHandler(115), SkillHandler(116),
     SkillHandler(117), SkillHandler(118), SkillHandler(119), SkillHandler(120),
     SkillHandler(123), SkillHandler(163), SkillHandler(164), SkillHandler(165),
     SkillHandler(166), SkillHandler(167)]
    public class SkillWorkshop : Skill, IDialog
    {
        public override void Execute()
        {
            var workshop = Client.Character.Map.Interactives.FirstOrDefault(x => x.Element == Element);
            if (workshop == null)
                return;

            if (workshop.Parameters == null || workshop.Parameters.Count == 0)
            {
                Client.Character.SendServerMessage("Atelier invalide : paramètres manquants.");
                return;
            }

            Client.Character.Dialog = this;
            InteractiveHandler.SendInteractiveUsedMessage(Client.Character.Map.Clients, Client.Character, this);

            var jobId = (sbyte)workshop.Parameters[0];
            Job = Client.Character.Jobs.GetJob(jobId);

            var jobLevel = Job != null ? ExperienceManager.Instance.GetJobLevelExperienceFloor(Job.Experience) : (sbyte)100;
            var caseAvailable = Job != null ? JobManager.Instance.GetJobSlot(jobLevel) : (sbyte)8;

            Client.Character.SetTrade(ExchangeTypeEnum.CRAFT);
            Client.Character.Trade.Open(new List<object> { jobId, caseAvailable, Id });
        }
    }
}
