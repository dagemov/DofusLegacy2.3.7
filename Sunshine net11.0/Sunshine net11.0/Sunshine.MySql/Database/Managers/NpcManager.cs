using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Npcs;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Npcs;
using Sunshine.WorldServer.Game.Actors.Npcs.Replies;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class NpcManager : Singleton<NpcManager>
    {
        public Dictionary<int, List<Npc>> Npcs = new Dictionary<int, List<Npc>>();

        public Dictionary<int, Func<Reply>> Replies = new Dictionary<int, Func<Reply>>();

        public IEnumerable<NpcTemplate> GetAllNpcs()
        {
            return DatabaseManager.Connection.Query<NpcTemplate>("SELECT * FROM npcs");
        }

        public NpcTemplate GetNpc(int id)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<NpcTemplate>($"SELECT * FROM npcs WHERE Id ='{id}'");
        }

        public NpcMessage GetNpcMessage(int id)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<NpcMessage>($"SELECT * FROM npcs_messages WHERE NpcId ='{id}'");
        }

        public IEnumerable<NpcReplie> GetNpcReplies(int id, int mapId)
        {
            return DatabaseManager.Connection.Query<NpcReplie>($"SELECT * FROM npcs_replies WHERE Npc ='{id}' AND Map = '{mapId}'").OrderBy(x => x.MessageId);
        }

        public IEnumerable<NpcSpawn> GetNpcSpawns()
        {
            return DatabaseManager.Connection.Query<NpcSpawn>($"SELECT * FROM worlds_npcs");
        }

        public Dictionary<int, NpcShop> GetNpcShops(int id)
        {
            return DatabaseManager.Connection.Query<NpcShop>($"SELECT * FROM npcs_items WHERE NpcId ='{id}'").ToDictionary(x => x.Item);
        }
    }
}
