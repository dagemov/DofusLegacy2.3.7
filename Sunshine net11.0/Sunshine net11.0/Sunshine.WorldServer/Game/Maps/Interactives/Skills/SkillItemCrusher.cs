using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Game.Exchanges;
using Sunshine.WorldServer.Handlers.Interactives;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(121), SkillHandler(181)]
    public class SkillItemCrusher : Skill, IDialog
    {
        private const sbyte DefaultJobId = 1;
        private const sbyte MaxSlots = 10;

        public override void Execute()
        {
            var crusher = Client.Character.Map.Interactives.FirstOrDefault(x => x.Element == Element);
            if (crusher == null)
                return;

            Client.Character.Dialog = this;
            InteractiveHandler.SendInteractiveUsedMessage(Client.Character.Map.Clients, Client.Character, this);

            var trade = new ItemCrusherTrade(Client.Character);
            Client.Character.SetTrade(ExchangeTypeEnum.STORAGE, trade);
            Client.Character.Trade.Open(new List<object> { DefaultJobId, MaxSlots, Id });
        }
    }
}
