using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    [SkillHandler(157)]
    public class SkillZaapiTeleport : Skill, IDialog
    {
        public override void Execute()
        {
            if (!MapManager.Instance.Zaapis.ContainsKey(Client.Character.Map.Id))
                return;

            Client.Character.Dialog = this;

            var currentZaapi = MapManager.Instance.Zaapis[Client.Character.Map.Id];

            var zaapis = MapManager.Instance.Zaapis.Where(x => x.Value.IsBonta == currentZaapi.IsBonta);
            var maps = zaapis.Select(x => MapManager.Instance.GetMap(x.Key));
            Client.Send(new TeleportDestinationsListMessage(1, maps.Select(x => x.Id), maps.Select(x => (short)x.SubAreaId),
                                            maps.Select(x => (short)20)));
        }

        public void Teleport(Map map)
        {
            var zaapi = map.Interactives.FirstOrDefault(x => x.Type == 106);
            if (zaapi != null)
            {
                if (Client.Character.Inventory.Kamas < 20)
                    return;

                var cell = map.Elements.FirstOrDefault(x => x.Id == zaapi.Element).Cell;
                Client.Character.Teleport(map.Id, cell);
                Client.Character.Inventory.SetKamas(-20);
            }
            DialogHandler.SendLeaveDialogMessage(Client);
            Client.Character.Dialog = null;
        }
    }
}
