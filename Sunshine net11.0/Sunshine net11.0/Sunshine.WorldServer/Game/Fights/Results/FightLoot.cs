using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.Results
{
    public class FightLoot
    {
        private RolePlayActor _actor;

        private FightActor _fighter;

        private List<short> _items;

        private int _kamas;

        public FightLoot(FightActor fighter)
        {
            _fighter = fighter;
            _items = new List<short>();
        }

        public FightLoot(RolePlayActor actor)
        {
            _actor = actor;
            _items = new List<short>();
        }

        public void AddItem(int itemId, int quantity)
        {
            if (_items.Contains((short)itemId))
            {
                var indexStack = _items.IndexOf((short)itemId);
                _items[indexStack + 1] += (short)quantity;
            }
            else
            {
                _items.Add((short)itemId);
                _items.Add((short)quantity);
            }
        }

        public void AddItem(BasePlayerItem item, int quantity)
        {
            if (_items.Contains((short)item.Template.Id))
            {
                var indexStack = _items.IndexOf((short)item.Template.Id);
                _items[indexStack + 1] += (short)quantity;
            }
            else
            {
                _items.Add((short)item.Template.Id);
                _items.Add((short)quantity);
            }
        }

        public void AddKamas(int kamas)
        {
            _kamas += kamas;
        }

        public void ClearLoot()
        {
            _items.Clear();
            _kamas = 0;
        }

        public Protocol.Types.FightLoot GetLoot()
        {
            return new Protocol.Types.FightLoot(_items, _kamas);
        }
    }
}
