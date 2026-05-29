using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Guilds;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.TaxCollectors.Inventory
{
    public class TaxInventory
    {
        private TaxCollector _taxCollector { get; set; }

        private Dictionary<int, List<BasePlayerItem>> _items;

        public TaxInventory(TaxCollector taxCollector)
        {
            _taxCollector = taxCollector;
            _items = TaxCollectorManager.Instance.GetTaxCollectorItems(taxCollector.Id);
        }

        public int GatheredKamas { get { return _taxCollector.Record.GatheredKamas; } set { _taxCollector.Record.GatheredKamas = value; } }

        public int GetWeight()
        {
            return GetItems().Sum(x => x.Weight);
        }

        public int GetValue()
        {
            return GetItems().Sum(x => x.Price);
        }

        public BasePlayerItem GetItemUid(int uid)
        {
            foreach (var items in _items.Values)
            {
                var item = items.FirstOrDefault(x => x.Id == uid);
                if (item != null)
                    return item;
            }
            return null;
        }

        public BasePlayerItem GetItem(int guid)
        {
            if (_items.ContainsKey(guid))
                return _items[guid].FirstOrDefault();
            return null;
        }

        public BasePlayerItem GetItem(int guid, CharacterInventoryPositionEnum position)
        {
            if (_items.ContainsKey(guid))
                return _items[guid].FirstOrDefault(x => x.Position == position);
            return null;
        }

        public BasePlayerItem GetItem(int guid, CharacterInventoryPositionEnum position, List<Effect> effects)
        {
            if (_items.ContainsKey(guid))
                return _items[guid].FirstOrDefault(x => x.Position == position
                                                  && IsSameEffects(x.Effects, effects));
            return null;
        }

        public BasePlayerItem GetItem(CharacterInventoryPositionEnum position)
        {
            foreach (var items in _items.Values)
            {
                var item = items.FirstOrDefault(x => x.Position == position);
                if (item != null)
                    return item;
            }
            return null;
        }

        public bool IsSameEffects(List<Effect> effects, List<Effect> secondEffects)
        {
            if (effects.Count == 0 && secondEffects.Count == 0)
                return true;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Value != secondEffects[i].Value)
                    return false;
            }
            return true;
        }

        public IEnumerable<BasePlayerItem> GetItems()
        {
            List<BasePlayerItem> playerItems = new List<BasePlayerItem>();
            foreach (var items in _items.Values)
                items.ForEach(x => playerItems.Add(x));
            return playerItems;
        }

        public void AddItem(BasePlayerItem item, int quantity = 1, bool isJetMax = false)
        {
            var sameItem = GetItem(item.Template.Id, item.Position, item.Effects);
            if (sameItem != null) // Stack
            {
                _items[item.Template.Id].Remove(sameItem);
                sameItem.Stack += quantity;
                _items[item.Template.Id].Add(sameItem);
            }
            else // Add
            {
                item.Stack = quantity;
                if (_items.ContainsKey(item.Template.Id))
                    _items[item.Template.Id].Add(item);
                else
                    _items.Add(item.Template.Id, new List<BasePlayerItem>() { item });
            }
        }

        public void RemoveItem(BasePlayerItem item, int quantity = 1)
        {
            var sameItem = GetItem(item.Template.Id, item.Position, item.Effects);

            if (sameItem != null) // Stack
            {
                _items[item.Template.Id].Remove(sameItem);

                if (sameItem.Stack - quantity <= 0)
                {
                    if (_items[item.Template.Id].Count == 0)
                        _items.Remove(item.Template.Id);
                }
                else
                {
                    sameItem.Stack -= quantity;
                    _items[item.Template.Id].Add(sameItem);
                }
            }
            else // Add
            {
                if (_items.ContainsKey(item.Template.Id))
                {
                    _items[item.Template.Id].Remove(item);

                    if (_items[item.Template.Id].Count == 0)
                        _items.Remove(item.Template.Id);
                }              
            }
        }
    }
}
