using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Exchanges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Actions
{
    public class NpcTradeAction : NpcAction
    {
        private Character _character;
        private Npc _npc;

        public NpcTradeAction(Npc npc, Character character)
        {
            _npc = npc;
            _character = character;
        }

        public override void Execute()
        {
            _character.Dialog = this;

            var shopItems = NpcManager.Instance.GetNpcShops(_npc.Record.Id);
            bool opensBankStorage = _npc.Record.Id == 100 || shopItems == null || !shopItems.Any();

            if (opensBankStorage)
                _character.SetTrade(ExchangeTypeEnum.STORAGE, null, _character);
            else
                _character.SetTrade(ExchangeTypeEnum.NPC_TRADE, null, _npc);

            _character.Trade.Open();
        }
    }
}
