using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Npcs;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Actors.Npcs.Replies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Loaders.World.Npcs
{
    public static class NpcsLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading NpcSpawns...");

            IEnumerable<NpcSpawn> npcSpawns = NpcManager.Instance.GetNpcSpawns();
            foreach(var npcSpawn in npcSpawns)
            {
                Npc npc = new Npc(NpcManager.Instance.GetNpc(npcSpawn.Npc), npcSpawn);
                MapManager.Instance.GetMap(npcSpawn.Map).EnterActor(npc);

                if (NpcManager.Instance.Npcs.ContainsKey(npcSpawn.Map))
                    NpcManager.Instance.Npcs[npcSpawn.Map].Add(npc);
                else
                    NpcManager.Instance.Npcs.Add(npcSpawn.Map, new List<Npc> { npc });
            }

            var replies = Assembly.GetAssembly(typeof(Reply)).GetTypes().Where(x => x.BaseType != null && x.BaseType.Name == "Reply");

            foreach (var reply in replies)
            {
                var replyAttribute = reply.GetCustomAttribute(typeof(ReplyHandler));
                if (replyAttribute != null)
                {
                    var attribute = replyAttribute as ReplyHandler;
                    var currentReply = reply.GetConstructor(Type.EmptyTypes);
                    var function = Expression.Lambda<Func<Reply>>(Expression.New(currentReply));
                    NpcManager.Instance.Replies.Add(attribute.Reply, function.Compile());
                }
            }

            Logs.Logger.Write($"[ World ] {npcSpawns.Count()} Npcs Spawned");
        }
    }
}
