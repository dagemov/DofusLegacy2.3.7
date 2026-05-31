using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Npcs;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Npcs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Commands.Moderator
{
    [CommandHandler("npc", RoleEnum.Moderator)]
    public class NpcCommand : WorldCommand
    {
        public override void Execute()
        {
            if (Parameters.Length < 2)
                Client.Character.SendServerMessage("Unknown npc !", Color.Red);
            else
            {
                var npc = int.TryParse((string)Parameters[1], out int npcId) ? int.Parse((string)Parameters[1]) : -1;
                var template = NpcManager.Instance.GetNpc(npc);
                if (npc == -1 || template == null)
                {
                    Client.Character.SendServerMessage($"Npc {npc} doesn't exist !");
                    return;

                }
                switch (Parameters[0].ToString().ToLower())
                {
                    case "spawn":
                        Npc actor = new Npc(template, new NpcSpawn
                        {
                            Npc = template.Id,
                            Cell = Client.Character.Cell.Id,
                            Direction = (byte)Client.Character.Direction,
                            Map = Client.Character.Map.Id
                        });
                        if (NpcManager.Instance.Npcs.ContainsKey(Client.Character.Map.Id))
                            NpcManager.Instance.Npcs[Client.Character.Map.Id].Add(actor);
                        else
                            NpcManager.Instance.Npcs.Add(Client.Character.Map.Id, new List<Npc> { actor });

                        Client.Character.Map.EnterActor(actor);
                        Client.Character.SendServerMessage($"Npc {npc} spawned.");
                        break;

                    case "unspawn":
                        if (NpcManager.Instance.Npcs.ContainsKey(Client.Character.Map.Id))
                        {
                            var lastActor = NpcManager.Instance.Npcs[Client.Character.Map.Id].FirstOrDefault(x => x.Record.Id == npc);
                            if (lastActor == null)
                            {
                                Client.Character.SendServerMessage($"Npc {npc} doesn't exist !");
                                return;
                            }
                            NpcManager.Instance.Npcs[Client.Character.Map.Id].Remove(lastActor);
                            Client.Character.Map.LeaveActor(lastActor);
                            Client.Character.SendServerMessage($"Npc {npc} unspawned.");
                        }
                        else
                            Client.Character.SendServerMessage($"Npc {npc} doesn't exist !");
                        break;
                }

            }
        }

        public override string Description
        {
            get { return "Allows spawn npc."; }
        }
    }
}
