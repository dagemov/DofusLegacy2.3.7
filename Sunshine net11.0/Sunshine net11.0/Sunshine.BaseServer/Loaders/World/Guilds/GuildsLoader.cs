using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.BaseServer.Loaders.World.Guilds
{
    public static class GuildsLoader
    {
        public static void Initialize()
        {
            Logs.Logger.Write("[ World ] Loading Guilds...");

            GuildManager.Instance.Guilds.Clear();
            GuildManager.Instance.GuildMembers.Clear();
            TaxCollectorManager.Instance.TaxCollectors.Clear();

            var guildsRecord = GuildManager.Instance.GetAllGuilds().ToList();

            foreach (var record in guildsRecord)
            {
                try
                {
                    if (!GuildManager.Instance.Guilds.ContainsKey(record.Id))
                        GuildManager.Instance.Guilds.Add(record.Id, new Guild(record));
                }
                catch (Exception ex)
                {
                    Logs.Logger.WriteError($"[ World ] Impossible de charger la guilde {record?.Id} ({record?.Name}) : {ex.Message}");
                }
            }

            GuildManager.Instance.GuildMembers = GuildManager.Instance.GetAllGuildsMembersById();

            var taxCollectors = TaxCollectorManager.Instance.GetAllTaxCollectorsById();
            TaxCollectorManager.Instance.TaxCollectors = taxCollectors;

            Logs.Logger.Write($"[ World ] {GuildManager.Instance.Guilds.Count} Guilds Loaded");
        }
    }
}
