using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Exchanges
{
    public abstract class Trade
    {
        public abstract ExchangeTypeEnum Type { get; }
        public Dictionary<RolePlayActor, List<Tuple<BasePlayerItem, int>>> Inventories { get; set; }
        public int Replay { get; set; }
        public abstract void Open(List<object> parameters = null);
        public abstract void Close(List<object> parameters = null);
        public abstract void Cancel(List<object> parameters = null);
        public abstract void SetKamas(Character trader, int quantity);
        public abstract void MoveItem(Character trader, int objectUid, int quantity);
        public abstract void SetReadyStatus(Character trader, bool isReady);
        public abstract void Apply();
    }
}
