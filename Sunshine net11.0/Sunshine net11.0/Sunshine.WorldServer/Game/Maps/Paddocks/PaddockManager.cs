using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Maps.Paddocks;
using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sunshine.WorldServer.Game.Maps.Paddocks
{
    public class PaddockManager : Singleton<PaddockManager>
    {
        private readonly object _sync = new object();
        private HashSet<string> _existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<int, Paddock> _paddocksById = new Dictionary<int, Paddock>();
        private Dictionary<int, Paddock> _paddocksByMapId = new Dictionary<int, Paddock>();

        public IReadOnlyDictionary<int, Paddock> Paddocks => _paddocksById;

        public bool HasOwnerIdColumn => _existingColumns.Contains("OwnerId");
        public bool HasCellIdSpawnMountColumn => _existingColumns.Contains("CellIdSpawnMount");

        public void Load()
        {
            lock (_sync)
            {
                _existingColumns = LoadExistingColumns();

                WorldMapPaddockRecord[] records;
                try
                {
                    records = DatabaseManager.Connection
                        .Query<WorldMapPaddockRecord>("SELECT * FROM world_maps_paddock")
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ PADDOCKS ] Impossible de charger world_maps_paddock : {ex.Message}");
                    _paddocksById = new Dictionary<int, Paddock>();
                    _paddocksByMapId = new Dictionary<int, Paddock>();
                    return;
                }

                var byId = new Dictionary<int, Paddock>();
                var byMap = new Dictionary<int, Paddock>();

                foreach (var record in records)
                {
                    if (!IsValid(record))
                        continue;

                    var paddock = new Paddock(record);
                    byId[paddock.Id] = paddock;
                    byMap[paddock.MapId] = paddock;
                }

                _paddocksById = byId;
                _paddocksByMapId = byMap;

                Logs.Logger.WriteInfo($"[ PADDOCKS ] {_paddocksById.Count} enclos chargés depuis world_maps_paddock.");
            }
        }

        private HashSet<string> LoadExistingColumns()
        {
            try
            {
                return new HashSet<string>(
                    DatabaseManager.Connection.Query("SHOW COLUMNS FROM world_maps_paddock")
                        .Select(x => (string)x.Field),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private bool IsValid(WorldMapPaddockRecord record)
        {
            if (record == null)
                return false;

            if (record.Map == null)
            {
                Logs.Logger.WriteError($"[ PADDOCKS ] Enclos {record.Id} ignoré : map {record.MapId} introuvable.");
                return false;
            }

            return true;
        }

        public Paddock GetPaddockById(int paddockId)
        {
            Paddock paddock;
            return _paddocksById.TryGetValue(paddockId, out paddock) ? paddock : null;
        }

        public Paddock GetPaddockByMap(int mapId)
        {
            Paddock paddock;
            return _paddocksByMapId.TryGetValue(mapId, out paddock) ? paddock : null;
        }

        public IEnumerable<Paddock> GetPaddocksByGuild(int guildId)
        {
            return _paddocksById.Values
                .Where(x => x.GuildId.HasValue && x.GuildId.Value == guildId)
                .ToArray();
        }

        public IEnumerable<Paddock> GetPaddocksToSell()
        {
            return _paddocksById.Values
                .Where(x => !x.IsPublicPaddock && x.OnSale && x.Map != null)
                .OrderBy(x => x.SalePrice)
                .ThenBy(x => x.MapId)
                .ToArray();
        }

        public int CountByGuild(int guildId)
        {
            return _paddocksById.Values.Count(x => x.GuildId.HasValue && x.GuildId.Value == guildId);
        }

        public bool TryGetConfiguredSpawnCell(int mapId, out short cellId)
        {
            cellId = 0;

            if (!HasCellIdSpawnMountColumn)
                return false;

            var paddock = GetPaddockByMap(mapId);
            if (paddock == null || !paddock.CellIdSpawnMount.HasValue)
                return false;

            var value = paddock.CellIdSpawnMount.Value;
            if (value < 0 || value >= 560)
                return false;

            cellId = (short)value;
            return true;
        }

        public short GetConfiguredSpawnCellOrDefault(int mapId, short defaultCellId = 0)
        {
            short cellId;
            return TryGetConfiguredSpawnCell(mapId, out cellId) ? cellId : defaultCellId;
        }

        public void Save()
        {
            foreach (var paddock in _paddocksById.Values.Where(x => x != null && x.IsDirty).ToArray())
                Save(paddock);
        }

        public void Save(Paddock paddock)
        {
            if (paddock == null || !paddock.IsDirty)
                return;

            var updates = new List<string>
            {
                "GuildId = @GuildId",
                "MapId = @MapId",
                "MaxOutdoorMount = @MaxOutdoorMount",
                "MaxItems = @MaxItems",
                "Abandonned = @Abandonned",
                "OnSale = @OnSale",
                "Locked = @Locked",
                "Price = @Price",
                "isPublic = @isPublic",
                "TpCell = @TpCell"
            };

            if (HasOwnerIdColumn)
                updates.Add("OwnerId = @OwnerId");

            if (HasCellIdSpawnMountColumn)
                updates.Add("CellIdSpawnMount = @CellIdSpawnMount");

            var sql = new StringBuilder();
            sql.Append("UPDATE world_maps_paddock SET ");
            sql.Append(string.Join(", ", updates));
            sql.Append(" WHERE Id = @Id");

            DatabaseManager.Connection.Execute(sql.ToString(), paddock.Record);
            paddock.IsDirty = false;
        }
    }
}