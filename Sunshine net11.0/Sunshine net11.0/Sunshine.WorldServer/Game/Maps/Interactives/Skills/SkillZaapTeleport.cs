using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(114)]
    public class SkillZaapTeleport : Skill, IDialog
    {
        public override void Execute()
        {
            Client.Character.Dialog = this;
            Client.Send(new ZaapListMessage(0, Client.Character.Zaaps.Select(x => x.Id), Client.Character.Zaaps.Select(x => (short)x.SubAreaId),
                                            Client.Character.Zaaps.Select(x => (short)100), Client.Character.Map.Id));
        }

        public void Teleport(Map map)
        {
            var zaap = map.Interactives.FirstOrDefault(x => x.Type == 16);
            if (zaap != null)
            {
                if (Client.Character.Inventory.Kamas < 100)
                    return;

                var cell = map.Elements.FirstOrDefault(x => x.Id == zaap.Element).Cell;            
                Client.Character.Teleport(map.Id, cell);
                Client.Character.Inventory.SetKamas(-100);
            }
            DialogHandler.SendLeaveDialogMessage(Client);
            Client.Character.Dialog = null;
        }
    }
}
