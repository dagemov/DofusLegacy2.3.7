using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Actions
{
    public class NpcSellAction : NpcAction
    {
        private Character _character;
        private Npc _npc;

        public NpcSellAction(Npc npc, Character character)
        {
            _npc = npc;
            _character = character;
        }

        public override void Execute()
        {
            _character.Dialog = this;
            InventoryHandler.SendExchangeStartedBidSellerMessage(_character.Client);
        }
    }
}
