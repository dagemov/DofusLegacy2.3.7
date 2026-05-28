using Dapper;
using Dapper.Contrib.Extensions;
using Sunshine.Mysql.Database;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Guilds;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Managers
{
    public class GuildManager : Singleton<GuildManager>
    {
        public Dictionary<int, Guild> Guilds = new Dictionary<int, Guild>(); // by guild id

        public Dictionary<int, List<GuildMember>> GuildMembers = new Dictionary<int, List<GuildMember>>(); // By guild id

        public IEnumerable<GuildRecord> GetAllGuilds()
        {
            return DatabaseManager.Connection.Query<GuildRecord>($"SELECT * FROM guilds");
        }

        public Dictionary<int, List<GuildMember>> GetAllGuildsMembersById()
        {
            Dictionary<int, List<GuildMember>> m_guild = new Dictionary<int, List<GuildMember>>();

            var guildsMembers = DatabaseManager.Connection.Query<GuildMemberRecord>($"SELECT * FROM guilds_members");

            foreach(var member in guildsMembers)
            {              
                if (m_guild.ContainsKey(member.Guild))
                    m_guild[member.Guild].Add(new GuildMember(member, CharacterManager.Instance.GetCharacter(member.Owner)));
                else
                    m_guild.Add(member.Guild, new List<GuildMember> { new GuildMember(member, CharacterManager.Instance.GetCharacter(member.Owner)) });
            }

            return m_guild;
        }

        public void CreateGuild(Guild guild, GuildMember member)
        {
            if (!Guilds.ContainsKey(guild.Id))
            {
                Guilds.Add(guild.Id, guild);
                AddMember(member);
            }
            else
                Logs.Logger.WriteError(string.Format("Guild {0} already exist", guild.Name));
        }

        public void DeleteGuild(Guild guild)
        {
            if (Guilds.ContainsKey(guild.Id))
            {
                Guilds.Remove(guild.Id);
                if (guild.TaxCollectors.Count > 0)
                    TaxCollectorManager.Instance.TaxCollectors.Remove(guild.Id);
            }
            else
                Logs.Logger.WriteError(string.Format("Guild {0} already exist", guild.Name));
        }

        public void AddMember(GuildMember member)
        {
            if (GuildMembers.ContainsKey(member.Record.Guild))
                GuildMembers[member.Record.Guild].Add(member);
            else
                GuildMembers.Add(member.Record.Guild, new List<GuildMember> { member });

            if (member.IsConnected())
            {
                GuildHandler.SendGuildJoinedMessage(member.Owner.Client, member);
                GuildHandler.SendGuildInformationsMembersMessage(member.Owner.Client, member.Guild);
                GuildHandler.SendGuildInformationsGeneralMessage(member.Owner.Client, member.Guild);
                member.Owner.RefreshActor();
            }
        }

        public void RemoveMember(GuildMember member)
        {
            if (GuildMembers.ContainsKey(member.Record.Guild))
                GuildMembers[member.Record.Guild].Remove(member);

            if (member.IsConnected())
            {
                member.Owner.GuildMember = null;
                member.Owner.RefreshActor();
                member.Owner.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 176, new object[0]);
                GuildHandler.SendGuildLeftMessage(member.Owner.Client);
            }
        }

        public void RefreshMember(GuildMember member)
        {
            GuildMembers[member.Record.Guild].Remove(member);
            GuildMembers[member.Record.Guild].Add(member);
        }

        public Guild GetGuild(int guildId)
        {
            if (Guilds.ContainsKey(guildId))
                return Guilds[guildId];
            return null;
        }

        public Guild GetGuildByName(string name)
        {
            return Guilds.Values.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        }

        public GuildMember GetGuildMember(Character member)
        {
            var members = GuildMembers.Values.FirstOrDefault(x => x.FirstOrDefault(y => y.Record.Owner == member.Id) != null);

            if (members != null && members.Count > 0)
            {
                var chrMember = members.FirstOrDefault(x => x.Record.Owner == member.Id);
                GuildMembers[chrMember.Guild.Id].Remove(chrMember);
                chrMember.Owner = member;
                GuildMembers[chrMember.Guild.Id].Add(chrMember);
                return chrMember;
            }
            return null;
        }

        public List<GuildMember> GetGuildMembers(int guildId)
        {
            if (GuildMembers.ContainsKey(guildId))
                return GuildMembers[guildId];
            return null;
        }

        public int GenerateId()
        {
            return Guilds.Count <= 0 ? 1 : Guilds.Last().Key + 1;
        }

        public void Save()
        {
            DatabaseManager.Connection.Execute("DELETE FROM guilds");
            DatabaseManager.Connection.Execute("DELETE FROM guilds_members");
            DatabaseManager.Connection.Execute("DELETE FROM worlds_taxcollectors");
            DatabaseManager.Connection.Execute("DELETE FROM taxcollectors_items");

            string cmd = "INSERT INTO guilds (Id, Date, Name, Experience, Boost, Prospecting, Wisdom, Pods, MaxTaxCollectors, SpellsCSV, EmblemBackgroundShape, EmblemBackgroundColor, EmblemForegroundShape, EmblemForegroundColor" +
                         ") VALUES(@Id, @Date, @Name, @Experience, @Boost, @Prospecting, @Wisdom, @Pods, @MaxTaxCollectors, @SpellsCSV, @EmblemBackgroundShape, @EmblemBackgroundColor, @EmblemForegroundShape, @EmblemForegroundColor)";
            DatabaseManager.Connection.Execute(cmd, Guilds.Values);

            foreach (var guild in Guilds)
            {
                foreach (var taxCollector in guild.Value.TaxCollectors)
                {
                    DatabaseManager.Connection.Insert(taxCollector.Record);

                    foreach (var item in taxCollector.Inventory.GetItems())
                    {
                        TaxCollectorItemsRecord taxItems = new TaxCollectorItemsRecord
                        {
                            Id = taxCollector.Id,
                            Item = item.Template.Id,
                            Stack = item.Stack,
                            Effects = EffectManager.Instance.SetEffects(new BigEndianWriter(), item.Effects)
                        };

                        DatabaseManager.Connection.Insert(taxItems);
                    }
                }
                foreach (var member in GuildMembers[guild.Key])
                    DatabaseManager.Connection.Insert(member.Record);
            }        
        }
    }
}
