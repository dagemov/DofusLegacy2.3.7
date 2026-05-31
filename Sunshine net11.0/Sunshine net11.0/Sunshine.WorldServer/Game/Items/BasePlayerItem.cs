using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Items
{
    public class BasePlayerItem
    {
        public ItemRecord Template { get; set; }

        public BasePlayerItem(ItemRecord item)
        {
            Template = item.Clone();
            RawObjectEffects = new List<ObjectEffect>();
        }

        public BasePlayerItem(int id)
        {
            Template = ItemManager.Instance.Items[id].Clone();
            RawObjectEffects = new List<ObjectEffect>();
        }

        public int Id { get; set; }
        public short Level => Template.Level;
        public ItemTypeEnum Type => (ItemTypeEnum)Template.TypeId;
        public virtual short AppearanceId => Template.AppearanceId;
        public string Criteria => Template.Criteria;
        public int Weight => Template.Weight;
        public CharacterInventoryPositionEnum Position { get; set; }
        public int Stack { get; set; }
        public List<Effect> Effects { get; set; }
        public List<List<Effect>> EffectSets { get; set; }
        public int Price { get; set; }
        public bool Selled { get; set; }

        // NOUVEAU
        public List<ObjectEffect> RawObjectEffects { get; set; }

        private const int ForcedNonExchangeableTemplateId = 12124;

        public bool HasEffect(EffectsEnum effectId)
        {
            if (Effects != null && Effects.Any(x => x != null && x.Id == effectId))
                return true;

            return RawObjectEffects != null && RawObjectEffects.Any(x => x != null && x.actionId == (short)effectId);
        }

        public void EnsureRuntimeEffects()
        {
            if (Template == null || Template.Id != ForcedNonExchangeableTemplateId)
                return;

            if (Effects == null)
                Effects = new List<Effect>();

            if (!Effects.Any(x => x != null && x.Id == EffectsEnum.Effect_NonExchangeable_982))
                Effects.Add(new Effect { Id = EffectsEnum.Effect_NonExchangeable_982, Value = 0 });

            if (RawObjectEffects != null && RawObjectEffects.Count > 0 && !RawObjectEffects.Any(x => x != null && x.actionId == (short)EffectsEnum.Effect_NonExchangeable_982))
                RawObjectEffects.Add(new ObjectEffectInteger((short)EffectsEnum.Effect_NonExchangeable_982, 0));
        }

        public bool IsLinkedToAccount()
        {
            EnsureRuntimeEffects();
            return (Template != null && Template.Id == ForcedNonExchangeableTemplateId) || HasEffect(EffectsEnum.Effect_NonExchangeable_982);
        }

        public bool IsLinkedToPlayer()
        {
            return HasEffect(EffectsEnum.Effect_NonExchangeable_981);
        }

        public bool IsExchangeable()
        {
            return !IsLinkedToAccount() && !IsLinkedToPlayer();
        }

        private IEnumerable<ObjectEffect> GetNetworkEffects()
        {
            EnsureRuntimeEffects();

            if (RawObjectEffects != null && RawObjectEffects.Count > 0)
                return RawObjectEffects;

            return (Effects ?? new List<Effect>()).Select(x => (ObjectEffect)x.GetObjectEffectInteger());
        }

        public bool IsEquiped()
        {
            return Position >= CharacterInventoryPositionEnum.ACCESSORY_POSITION_AMULET
                && Position <= CharacterInventoryPositionEnum.INVENTORY_POSITION_MOUNT;
        }

        public virtual ObjectItem GetObjectItem()
        {
            return new ObjectItem(
                (byte)Position,
                (short)Template.Id,
                0,
                false,
                GetNetworkEffects(),
                Id,
                Stack);
        }

        public virtual ObjectItemToSellInBid GetObjectItemToSell()
        {
            return new ObjectItemToSellInBid(
                (short)Template.Id,
                0,
                false,
                GetNetworkEffects(),
                Template.Id,
                Stack,
                Price,
                0);
        }

        public virtual BidExchangerObjectInfo GetBidExchangerObjectInfo()
        {
            return new BidExchangerObjectInfo(
                Id,
                0,
                false,
                GetNetworkEffects(),
                BidHouseManager.Instance.BidsHousePrices[this]);
        }

        public virtual ObjectItemNotInContainer GetObjectItemNotInContainer()
        {
            return new ObjectItemNotInContainer(
                (short)Template.Id,
                0,
                false,
                GetNetworkEffects(),
                Id,
                Stack // ← obligatoire
            );
        }

        public BasePlayerItem Clone()
        {
            return (BasePlayerItem)MemberwiseClone();
        }
        public bool HasRawObjectEffects()
        {
            return RawObjectEffects != null && RawObjectEffects.Count > 0;
        }
        public bool HasSameRawEffects(BasePlayerItem other)
        {
            if (other == null)
                return false;

            if (!HasRawObjectEffects() && !other.HasRawObjectEffects())
                return true;

            if (HasRawObjectEffects() != other.HasRawObjectEffects())
                return false;

            if (RawObjectEffects.Count != other.RawObjectEffects.Count)
                return false;

            for (int i = 0; i < RawObjectEffects.Count; i++)
            {
                var a = RawObjectEffects[i];
                var b = other.RawObjectEffects[i];

                if (a.TypeId != b.TypeId || a.actionId != b.actionId)
                    return false;

                var aMount = a as ObjectEffectMount;
                var bMount = b as ObjectEffectMount;

                if (aMount != null && bMount != null)
                {
                    if (aMount.mountId != bMount.mountId)
                        return false;

                    if (aMount.modelId != bMount.modelId)
                        return false;

                    if (Math.Abs(aMount.date - bMount.date) > 0.01)
                        return false;

                    continue;
                }

                var aInt = a as ObjectEffectInteger;
                var bInt = b as ObjectEffectInteger;

                if (aInt != null && bInt != null)
                {
                    if (aInt.value != bInt.value)
                        return false;

                    continue;
                }

                // Si type non géré, on refuse l'égalité par sécurité
                return false;
            }

            return true;
        }
    }
}