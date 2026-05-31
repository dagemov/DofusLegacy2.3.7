using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Guilds
{
    public class GuildMember
    {
        public GuildMemberRecord Record { get; set; }

        public Character Owner { get; set; }

        public Guild Guild { get { return GuildManager.Instance.GetGuild(Record.Guild); } }

        public WorldClient Client { get { return Owner?.Client; } }

        public GuildMember(GuildMemberRecord record, Character owner)
        {
            Record = record;
            Owner = owner;
        }

        public bool IsConnected()
        {
            return Owner != null && Owner.Client != null;
        }

        public bool IsBoss { get { return Record.Rank == 1; } }

        public int Rank { get { return Record.Rank; } set { Record.Rank = value; } }

        public GuildRightsBitEnum Rights { get { return (GuildRightsBitEnum)Record.Rights; } set { Record.Rights = (uint)value; } }

        public long GivenExperience { get { return Record.GivenExperience; } set { Record.GivenExperience = value; } }

        public sbyte GivenPercent { get { return Record.GivenPercent; } set { Record.GivenPercent = value; } }

        public bool HasRight(GuildRightsBitEnum right)
        {
            return Rights.HasFlag(right);
        }

        public Protocol.Types.GuildMember GetGuildMember()
        {
            if (Owner == null)
                return new Protocol.Types.GuildMember(Record.Owner, 0, "Inconnu", 0, false, (short)Rank, GivenExperience, GivenPercent, (uint)Rights, 0, 0, 0, 0);

            return new Protocol.Types.GuildMember(Owner.Id, (byte)Owner.Level, Owner.Name, (sbyte)Owner.Breed, Owner.Sex, (short)Rank, GivenExperience, GivenPercent, (uint)Rights, IsConnected() ? (sbyte)1 : (sbyte)0, (sbyte)Owner.Alignment.Side, 0, 0);
        }
    }
}
