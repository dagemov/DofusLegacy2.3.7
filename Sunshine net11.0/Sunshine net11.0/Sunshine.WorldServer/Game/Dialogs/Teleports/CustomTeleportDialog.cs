using Sunshine.MySql.Database.World.Teleports;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Teleports;
using Sunshine.WorldServer.Handlers.Dialogs;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sunshine.WorldServer.Game.Dialogs.Teleports
{
    public class CustomTeleportDialog : IDialog
    {
        private readonly Character _character;
        private readonly string _panelName;
        private readonly Dictionary<int, CustomTeleportDestinationRecord> _destinations;
        private readonly List<Map> _maps;

        public CustomTeleportDialog(Character character, IEnumerable<CustomTeleportDestinationRecord> destinations, string panelName)
        {
            _character = character;
            _panelName = panelName;
            _destinations = destinations == null
                ? new Dictionary<int, CustomTeleportDestinationRecord>()
                : destinations
                    .GroupBy(x => x.TeleportMapId)
                    .ToDictionary(x => x.Key, x => x.First());
            _maps = _destinations.Keys
                .Select(x => MySql.Database.Managers.MapManager.Instance.GetMap(x))
                .Where(x => x != null)
                .ToList();
        }

        public void Open()
        {
            if (_maps.Count <= 0)
            {
                _character.SendServerMessage($"Aucune destination configurée pour {_panelName}.", Color.Red);
                return;
            }

            _character.Dialog = this;
            _character.Client.Send(new ZaapListMessage(0,
                _maps.Select(x => x.Id),
                _maps.Select(x => (short)x.SubAreaId),
                _maps.Select(x => GetCost(x.Id)),
                _character.Map != null ? _character.Map.Id : 0));
        }

        public void Teleport(Map map)
        {
            if (map == null)
                return;

            if (!_destinations.ContainsKey(map.Id))
                return;

            var destination = _destinations[map.Id];

            if (destination.KamasCost > 0 && _character.Inventory.Kamas < destination.KamasCost)
            {
                _character.SendServerMessage($"Tu n'as pas assez de kamas pour cette destination ({destination.KamasCost}).", Color.Red);
                return;
            }

            if (!CustomTeleportService.HasRequiredItem(_character, destination))
            {
                _character.SendServerMessage("Tu ne possèdes pas l'objet requis pour cette destination.", Color.Red);
                return;
            }

            short cellId = CustomTeleportService.ResolveTeleportCell(map, destination);

            if (!CustomTeleportService.TryConsumeRequiredItem(_character, destination))
            {
                _character.SendServerMessage("Tu ne possèdes plus l'objet requis pour cette destination.", Color.Red);
                return;
            }

            if (destination.KamasCost > 0)
                _character.Inventory.SetKamas(-destination.KamasCost);

            _character.Teleport(map.Id, cellId);
            DialogHandler.SendLeaveDialogMessage(_character.Client);
        }

        private short GetCost(int mapId)
        {
            return _destinations.ContainsKey(mapId) ? (short)_destinations[mapId].KamasCost : (short)0;
        }
    }
}
