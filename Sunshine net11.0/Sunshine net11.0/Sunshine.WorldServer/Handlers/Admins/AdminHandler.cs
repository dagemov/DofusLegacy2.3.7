using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Handlers.Admins
{
    public class AdminHandler : WorldPacketHandler
    {
        [WorldHandler(5662)]
        public static void HandleAdminQuietCommandMessage(WorldClient client, AdminQuietCommandMessage message)
        {
            if (client?.Account == null || client.Character == null)
                return;

            if (client.Account.Role == (sbyte)RoleEnum.Player)
                return;

            var content = message?.content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return;

            if (content.StartsWith("moveto", StringComparison.OrdinalIgnoreCase))
            {
                var rawArgument = content.Length > 6 ? content.Substring(6).Trim() : string.Empty;
                if (!int.TryParse(rawArgument, out int position))
                {
                    client.Character.SendServerMessage("Commande invalide. Utilise : moveto <mapId>");
                    return;
                }

                var map = MapManager.Instance.GetMap(position);
                if (map == null)
                {
                    client.Character.SendServerMessage($"Carte introuvable : {position}");
                    return;
                }

                var cell = map.Cells.FirstOrDefault(x => x.Walkable && x.Id > 100);

                if (cell.Id == 0) // ou une autre valeur par défaut
                {
                    client.Character.SendServerMessage($"Aucune cellule valide trouvée sur la carte {position}");
                    return;
                }



                client.Character.Teleport(position, cell.Id);
                return;
            }

            client.Character.SendServerMessage($"unknown command : {content} !");
        }

        [WorldHandler(76)]
        public static void HandleAdminCommandMessage(WorldClient client, AdminCommandMessage message)
        {
            if (client.Account.Role == (sbyte)RoleEnum.Player)
                return;       
        }
    }
}
