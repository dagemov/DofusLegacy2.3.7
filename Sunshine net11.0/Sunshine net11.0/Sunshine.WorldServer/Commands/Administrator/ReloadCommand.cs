using Sunshine.BaseServer.Loaders.World.Maps.Interactives;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Interactives;
using Sunshine.WorldServer.Game.Maps.PaddockInstances;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Commands.Administrator
{
    [CommandHandler("reload", RoleEnum.Administrator)]
    public class ReloadCommand : WorldCommand
    {
        public override string Description => "Recharge une partie du monde (.reload interactives).";

        public override void Execute()
        {
            var target = Parameters != null && Parameters.Length > 0
                ? (Parameters[0] ?? string.Empty).ToString().Trim().ToLowerInvariant()
                : string.Empty;

            if (target != "interactives")
            {
                Client.Character.SendServerMessage("Usage: .reload interactives");
                return;
            }

            InteractiveManager.Instance.Interactives.Clear();
            JobManager.Instance.JobSkills = InteractiveManager.Instance.GetAllInteractiveSkills();
            InteractivesLoader.Initialize();
            PaddockInstanceManager.Instance.Load();

            var spawnsByMap = InteractiveManager.Instance.GetAllInteractiveSpawns();
            foreach (var map in MapManager.Instance.Maps.Values)
            {
                map.Interactives = spawnsByMap.ContainsKey(map.Id) ? spawnsByMap[map.Id] : new List<Interactive>();

                var interactiveElements = map.Interactives
                    .Where(x => x.GetInteractiveElement != null)
                    .Select(x => x.GetInteractiveElement)
                    .ToList();

                var statedElements = map.Interactives
                    .Where(x => x.GetStatedElement != null)
                    .Select(x => x.GetStatedElement)
                    .ToList();

                foreach (var mapClient in map.Clients.ToList())
                {
                    mapClient.Send(new InteractiveMapUpdateMessage(interactiveElements));
                    mapClient.Send(new StatedMapUpdateMessage(statedElements));
                }
            }

            Client.Character.SendServerMessage("Interactives rechargées sur tout le serveur.");
        }
    }
}
