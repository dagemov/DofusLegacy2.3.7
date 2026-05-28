using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Dungeons;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class DungeonManager : Singleton<DungeonManager>    
    {
        public Dictionary<int, DungeonSpawn> GetAllDungeons()
        {
            try
            {
                return DatabaseManager.Connection.Query<DungeonSpawn>("SELECT * FROM dungeons").ToDictionary(x => x.Map);
            }
            catch
            {
                return new Dictionary<int, DungeonSpawn>();
            }
        }

        public List<short> GetSearchDungeonIds()
        {
            var searchIds = ReadDungeonIdsFromTable("dungeons_search");
            if (searchIds.Count > 0)
                return searchIds;

            return ReadDungeonIdsFromTable("dungeons");
        }

        private List<short> ReadDungeonIdsFromTable(string tableName)
        {
            var result = new HashSet<short>();

            try
            {
                var columns = DatabaseManager.Connection.Query($"SHOW COLUMNS FROM `{tableName}`")
                    .Select(x => ((IDictionary<string, object>)x)["Field"]?.ToString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (columns.Count == 0)
                    return result.OrderBy(x => x).ToList();

                var orderedCandidates = columns
                    .Where(x => x.IndexOf("map", StringComparison.OrdinalIgnoreCase) < 0)
                    .Where(x => x.IndexOf("cell", StringComparison.OrdinalIgnoreCase) < 0)
                    .OrderByDescending(x => string.Equals(x, "DungeonId", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "Dungeon", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(x => string.Equals(x, "Id", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(x => x.IndexOf("dungeon", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ThenByDescending(x => x.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (orderedCandidates.Count == 0)
                    orderedCandidates = columns
                        .OrderByDescending(x => x.IndexOf("dungeon", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ThenByDescending(x => x.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                foreach (var column in orderedCandidates)
                {
                    foreach (var row in DatabaseManager.Connection.Query($"SELECT `{column}` AS Value FROM `{tableName}`"))
                    {
                        short parsed;
                        var raw = ((IDictionary<string, object>)row)["Value"];
                        if (raw != null && short.TryParse(raw.ToString(), out parsed) && parsed > 0)
                            result.Add(parsed);
                    }

                    if (result.Count > 0 && column.IndexOf("dungeon", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
            }
            catch
            {
            }

            return result.OrderBy(x => x).ToList();
        }
    }
}
