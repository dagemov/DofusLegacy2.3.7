using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Maps.Paddocks;
using Sunshine.WorldServer.Game.Dialogs;
using Sunshine.WorldServer.Handlers.Basic;
using Sunshine.WorldServer.Handlers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Guilds
{
    public class Guild
    {
        public GuildRecord Record { get; set; }

        public List<GuildMember> Members { get { return GuildManager.Instance.GetGuildMembers(Record.Id); } }

        public List<TaxCollector> TaxCollectors { get { return TaxCollectorManager.Instance.GetTaxCollectors(Record.Id); } }

        public Guild(GuildRecord record)
        {
            Record = record;
            Level = ExperienceManager.Instance.GetGuildLevelExperience(record.Experience);
        }

        public int Id { get { return Record.Id; } }

        public string Name { get { return Record.Name; } set { Record.Name = value; } }

        public DateTime Date { get { return Record.Date; } set { Record.Date = value; } }

        public short Boost { get { return Record.Boost; } set { Record.Boost = value; } }

        public short Prospecting { get { return Record.Prospecting; } set { Record.Prospecting = value; } }

        public long Experience { get { return Record.Experience; } set { Record.Experience = value; } }

        public short Wisdom { get { return Record.Wisdom; } set { Record.Wisdom = value; } }

        public short Pods { get { return Record.Pods; }  set { Record.Pods = value; } }

        public sbyte MaxTaxCollectors { get { return Record.MaxTaxCollectors; } set { Record.MaxTaxCollectors = value; } }

        public int EmblemBackgroundShape { get { return Record.EmblemBackgroundShape; } set { Record.EmblemBackgroundShape = value; } }

        public int EmblemBackgroundColor { get { return Record.EmblemBackgroundColor; } set { Record.EmblemBackgroundColor = value; } }

        public int EmblemForegroundShape { get { return Record.EmblemForegroundShape; } set { Record.EmblemForegroundShape = value; } }

        public int EmblemForegroundColor { get { return Record.EmblemForegroundColor; } set { Record.EmblemForegroundColor = value; } }

        public byte Level { get; set; }

        public double ExpLevelFloor { get { return ExperienceManager.Instance.GetGuildExperienceFloor(Experience); } }

        public double ExpNextLevelFloor { get { return ExperienceManager.Instance.GetNextGuildExperienceLevelFloor(Level); } }

        public short TaxCollectorHealth { get { return (short)(3000 + (short)(20 * this.Level)); } }

        public short TaxCollectorDamageBonus { get { return (short)this.Level; } }

        public short HireCost { get { return (short)(1000 + (int)(this.Level * 100)); } }

        public int MaxPaddocks { get { return (int)Math.Floor(Level / 10.0); } }

        public List<Paddock> Paddocks { get { return PaddockManager.Instance.GetPaddocksByGuild(Id).ToList(); } }

        public string SpellsCSV { get { return Record.SpellsCSV; } set { Record.SpellsCSV = value; } }

        public string SpellsLevelsCSV { get { return Record.SpellsLevelsCSV; } set { Record.SpellsLevelsCSV = value; } }

        public IEnumerable<short> Spells { get { return SpellsCSV.Split(',').Select(x => short.Parse(x)); } }

        public IEnumerable<sbyte> SpellsLevels { get { return SpellsLevelsCSV.Split(',').Select(x => sbyte.Parse(x)); } }

        private object _lock = new object();

        public bool CanAddMember()
        {
            return Members.Count < 50;
        }

        public GuildMember GetGuildMember(int member)
        {
            return Members.FirstOrDefault(x => x.Owner.Id == member);
        }

        public GuildMember GetBoss()
        {
            return Members.FirstOrDefault(x => x.IsBoss);
        }

        public void UpdateMember(GuildMember boss, GuildMember member, short rank, sbyte experienceGivenPercent, uint rights)
        {
            lock (_lock)
            {
                if (member.Owner.Id != boss.Owner.Id && boss.IsBoss && rank == 1) // Update Boss
                {
                    boss.Rank = 0;
                    boss.Rights = (uint)GuildRightsBitEnum.GUILD_RIGHT_NONE;
                    GuildManager.Instance.RefreshMember(boss);
                    for (int i = 0; i < Members.Count; i++)
                    {
                        if (Members[i].IsConnected())
                            BasicHandler.SendTextInformationMessage(Members[i].Client, TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 199, new string[] { member.Owner.Name, boss.Owner.Name, Name });
                    }

                    GuildHandler.SendGuildInformationsMemberUpdateMessage(boss.Owner.Client, boss);
                }
                else
                {
                    if (boss.HasRight(GuildRightsBitEnum.GUILD_RIGHT_MANAGE_XP_CONTRIBUTION) || (boss.Owner.Id == member.Owner.Id && boss.HasRight(GuildRightsBitEnum.GUILD_RIGHT_MANAGE_MY_XP_CONTRIBUTION)))
                        member.GivenPercent = experienceGivenPercent;

                    if (boss.HasRight(GuildRightsBitEnum.GUILD_RIGHT_MANAGE_RANKS) || boss.Owner.Id == member.Owner.Id)
                        member.Rank = rank;

                    if (boss.HasRight(GuildRightsBitEnum.GUILD_RIGHT_MANAGE_RIGHTS) || member.Owner.Id == boss.Owner.Id)
                        member.Rights = (GuildRightsBitEnum)rights;
                }

                GuildManager.Instance.RefreshMember(member);

                GuildHandler.SendGuildInformationsMemberUpdateMessage(boss.Owner.Client, member);
                GuildHandler.SendGuildInformationsMemberUpdateMessage(member.Owner.Client, member);
            }
        }     
        
        public void KickMember(GuildMember kicker, GuildMember kicked)
        {
            lock (_lock)
            {
                if (kicker.HasRight(GuildRightsBitEnum.GUILD_RIGHT_BAN_MEMBERS) || kicker.IsBoss || kicker.Owner.Id == kicked.Owner.Id)
                {
                    if (kicked.IsBoss && Members.Count > 1)
                        return;

                    if (kicked.Owner.Id != kicker.Owner.Id)
                        kicker.Owner.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 177, new object[] { kicked.Owner.Name });

                    for (int i = 0; i < kicker.Guild.Members.Count; i++)
                    {
                        if (kicker.Guild.Members[i].IsConnected())
                            GuildHandler.SendGuildMemberLeavingMessage(kicker.Guild.Members[i].Client, kicked, true);
                    }

                    GuildManager.Instance.RemoveMember(kicked);

                    if (kicked.IsBoss && Members.Count <= 0)
                        GuildManager.Instance.DeleteGuild(kicked.Guild);
                }
            }
        }

        public void AddEarnedExperience(long experience)
        {
            lock (_lock)
            {
                this.Experience += experience;

                byte guildLevel = ExperienceManager.Instance.GetGuildLevelExperience(this.Experience);
                if (guildLevel != this.Level)
                {
                    if (guildLevel > this.Level)
                        this.Boost += (short)((guildLevel - this.Level) * 5);

                    this.Level = guildLevel;
                    this.OnLevelChanged();
                }
            }
        }

        private void OnLevelChanged()
        {           
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i].IsConnected())
                {
                    BasicHandler.SendTextInformationMessage(Members[i].Client, TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 208, new object[] { this.Level });
                    Members[i].Client.Send(new GuildLevelUpMessage(this.Level));
                }
            }           
        }

        public bool UpgradeSpell(int spellId)
        {
            // 1,1,1,1,1,1,1,1,1,1,1
            if (Spells.Contains((short)spellId) && Boost >= 5)
            {
                string levelSpells = "";

                var levels = SpellsLevels.ToList();

                var indexLevel = Spells.ToList().IndexOf((short)spellId);

                for (int i = 0; i < 11; i++)
                {
                    if (i == indexLevel)
                        levelSpells += (levels[indexLevel] + 1) + ",";
                    else
                        levelSpells += "1,";
                }

                levelSpells = levelSpells.Remove(levelSpells.Length - 1, 1);

                SpellsLevelsCSV = levelSpells;

                Boost -= 5;

                return true;
            }
            return false;
        }

        public GuildEmblem GetGuildEmblem()
        {
            return new GuildEmblem((short)EmblemForegroundShape, EmblemForegroundColor, (short)EmblemBackgroundShape, EmblemBackgroundColor);
        }

        public GuildInformations GetGuildInformations()
        {
            return new GuildInformations(Id, Name, GetGuildEmblem());
        }

        public BasicGuildInformations GetBasicGuildInformations()
        {
            return new BasicGuildInformations(Id, Name);
        }

        public void RefreshTaxCollector(TaxCollector taxCollector, GuildMember member, bool isWin)
        {
            if (isWin)
            {
                TaxCollectorHandler.SendTaxCollectorMovementRemoveMessage(member.Client, taxCollector);
                TaxCollectorHandler.SendTaxCollectorMovementAddMessage(member.Client, taxCollector);
            }
        }
    }
}
