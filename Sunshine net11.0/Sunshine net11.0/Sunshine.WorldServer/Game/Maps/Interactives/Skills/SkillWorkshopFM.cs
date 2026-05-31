using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Characters.Jobs;
using Sunshine.WorldServer.Handlers.Interactives;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(168), SkillHandler(169)]
    public class SkillWorkshopFM : Skill, IDialog
    {
        public override void Execute()
        {
            Client.Character.Dialog = this;
            var workshop = Client.Character.Map.Interactives.FirstOrDefault(x => x.Element == Element);
            if (workshop == null)
                return;

            InteractiveHandler.SendInteractiveUsedMessage(Client.Character.Map.Clients, Client.Character, this);

            var jobId = (sbyte)workshop.Parameters[0];
            Job = Client.Character.Jobs.GetJob(jobId);

            JobHandler.SendVisibleJobDataMessage(Client);

            Client.Character.SetTrade(ExchangeTypeEnum.FORGEMAGIE);
            Client.Character.Trade.Open(new List<object> { jobId, (sbyte)3, Id });
        }
    }
}
