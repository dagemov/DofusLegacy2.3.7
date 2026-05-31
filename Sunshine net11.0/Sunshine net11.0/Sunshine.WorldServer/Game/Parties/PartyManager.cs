using Sunshine.BaseClient;
using Sunshine.Protocol.Utils;
using Sunshine.Servers;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Parties
{
    public class PartyManager : Singleton<PartyManager>
    {
        private readonly UniqueIdProvider _idProvider = new UniqueIdProvider();

        public Party SetParty(Character leader)
        {
            return new Party(_idProvider.Pop(), leader);
        }

        public Party GetParty(int partyId)
        {
            return ServersManager.Instance.GetClients<WorldClient>().FirstOrDefault(x => x.Character.IsInParty && x.Character.Party.Id == partyId).Character.Party;
        }

        public void RemoveParty(IParty party)
        {
            _idProvider.Push(party.Id);
        }
    }
}
