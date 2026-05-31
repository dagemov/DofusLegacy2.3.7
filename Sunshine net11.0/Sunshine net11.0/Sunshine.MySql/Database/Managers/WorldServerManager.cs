using Sunshine.MySql.Database.Auth;
using System;
using System.Collections.Generic;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.Servers;
using Sunshine.WorldServer.Client;
using Sunshine.Protocol.Utils;
using Sunshine.MySql.Database.Auth.Accounts;

namespace Sunshine.MySql.Database.Managers
{
    public class WorldServerManager : Singleton<WorldServerManager>
    {
        public Auth.Worlds.World GetWorld(int serverId)
        {
            return DatabaseManager.Connection.QueryFirstOrDefault<Auth.Worlds.World>($"SELECT * FROM worlds WHERE Id = '{serverId}'");
        }

        public Auth.Worlds.World[] GetWorlds()
        {
            return DatabaseManager.Connection.Query<Auth.Worlds.World>($"SELECT * FROM worlds").ToArray();
        }

        public void UpdateWorld(Auth.Worlds.World world)
        {
            string cmd = @"UPDATE worlds SET Status = @Status WHERE Id = @Id";
            DatabaseManager.Connection.Execute(cmd, world);
        }

        public Character GetCharacter(string name)
        {
            var client = ServersManager.Instance.GetClients<WorldClient>().FirstOrDefault(x => x.Character != null && x.Character.Name.ToLower() == name.ToLower());
            return client?.Character;
        }

        public Character GetCharacter(int id)
        {
            var client = ServersManager.Instance.GetClients<WorldClient>().FirstOrDefault(x => x.Character != null && x.Character.Id == id);
            return client?.Character;
        }

        public WorldClient GetWorldClient(Account account)
        {
            return ServersManager.Instance.GetClients<WorldClient>().FirstOrDefault(x => x.Account != null && x.Account.Id == account.Id);
        }
    }
}
