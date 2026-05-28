using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Prisms;
using Sunshine.WorldServer.Handlers.Dialogs;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Dialogs.Prisms
{
    public class PrismDialog : IDialog
    {
        private readonly Character m_character;
        private readonly List<Map> m_destinations;

        public PrismDialog(Character character, IEnumerable<Map> destinations)
        {
            m_character = character;
            m_destinations = destinations == null ? new List<Map>() : destinations.Distinct().ToList();
        }

        public void Open()
        {
            m_character.Dialog = this;
            m_character.Client.Send(new TeleportDestinationsListMessage(2,
                m_destinations.Select(x => x.Id),
                m_destinations.Select(x => (short)x.SubAreaId),
                m_destinations.Select(x => (short)0)));
        }

        public Map GetDestinationById(int mapId)
        {
            return m_destinations.FirstOrDefault(x => x.Id == mapId);
        }

        public void Teleport(Map map)
        {
            if (map == null)
                return;

            short cellId = 0;
            map.EnsureFightCells();

            var authoritativePrism = PrismManager.Instance.GetAuthoritativePrismForMap(map, false);
            if (authoritativePrism != null && !authoritativePrism.WasDefeated)
                cellId = authoritativePrism.CellId;
            else if (map.BlueCells != null && map.BlueCells.Count > 0)
                cellId = map.BlueCells[0];

            m_character.Teleport(map.Id, cellId);
            DialogHandler.SendLeaveDialogMessage(m_character.Client);
        }
    }
}
