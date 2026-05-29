using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Parties
{
    public interface IParty
    {
        int Id { get; }
        Character Leader { get; set; }
        int MaxMemberCount { get; }
        PartyTypeEnum Type { get; }
        void AcceptMember(Character member);
    }
}
