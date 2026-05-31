using Sunshine.Protocol.Utils;
using Sunshine.Servers;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights
{
    public class FightManager : Singleton<FightManager>
    {
        private readonly UniqueIdProvider _idProvider = new UniqueIdProvider();
        private readonly ReversedUniqueIdProvider _IdReverseProvider = new ReversedUniqueIdProvider(0);

        public T SetFight<T>(Character leader) where T : class
        {
            switch(typeof(T).Name)
            {
                case "FightPvM":
                    return new FightPvM(_idProvider.Pop(), leader) as T;

                case "FightDuel":
                    return new FightDuel(_idProvider.Pop(), leader) as T;

                case "FightAgression":
                    return new FightAgression(_idProvider.Pop(), leader) as T;

                case "FightPvT":
                    return new FightPvT(_idProvider.Pop(), leader) as T;

                default:
                    return null;
            }
        }

        public Fight GetFight(int fightId)
        {
            return ServersManager.Instance.GetClients<WorldClient>().FirstOrDefault(x => x.Character.IsInFight() && x.Character.Fight.Id == fightId).Character.Fight;
        }

        public void RemoveFight(Fight fight)
        {
            _idProvider.Push(fight.Id);
            fight.Map.Fights.Remove(fight);
        }

        public int GenerateId(bool isReverse)
        {
            if (isReverse)
                return _IdReverseProvider.Pop();
            else
                return _idProvider.Pop();
        }
    }
}
