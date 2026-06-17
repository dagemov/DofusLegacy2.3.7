using System.Collections.Generic;
using System.Linq;
using Dapper;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Spells;
using Sunshine.Protocol.Utils;

namespace Sunshine.MySql.Database.Managers
{
    /// <summary>
    /// Data access for spell metadata side tables. Phase 1 only handles `effect_metadata`.
    /// </summary>
    public class MetadataRepository : Singleton<MetadataRepository>
    {
        public void EnsureTable()
        {
            EffectMetadataBootstrap.EnsureEffectMetadataTable(DatabaseManager.Connection);
        }

        public List<EffectMetadataRecord> GetAllEffectMetadata()
        {
            return DatabaseManager.Connection.Query<EffectMetadataRecord>("SELECT * FROM effect_metadata").ToList();
        }
    }
}
