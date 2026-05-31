using Sunshine.MySql.Database.World.Items;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.WorldServer.Game.Items;
using Sunshine.WorldServer.Game.Mounts;
using Sunshine.WorldServer.Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Items.Custom
{
    public sealed class MountCertificate : BasePlayerItem
    {
        public MountCertificate(int id) : base(id)
        {
        }

        public MountCertificate(ItemRecord item) : base(item)
        {
        }

        public int? MountId
        {
            get
            {
                var effect = RawObjectEffects?
                    .OfType<ObjectEffectMount>()
                    .FirstOrDefault();

                return effect?.mountId;
            }
        }

        public void RefreshEffects(int ownerId = 0)
        {
            var mount = MountCertificateFactory.ResolveMount(this, ownerId);
            if (mount == null)
                return;

            RawObjectEffects = MountCertificateFactory.BuildEffects(mount);

            if (Effects == null || Effects.Count == 0)
            {
                Effects = Template.EffectsBase != null
                    ? Template.EffectsBase.Clone()
                    : new List<Effect>();
            }
        }

        public override ObjectItem GetObjectItem()
        {
            RefreshEffects();
            return base.GetObjectItem();
        }

        public override ObjectItemToSellInBid GetObjectItemToSell()
        {
            RefreshEffects();
            return base.GetObjectItemToSell();
        }

        public override BidExchangerObjectInfo GetBidExchangerObjectInfo()
        {
            RefreshEffects();
            return base.GetBidExchangerObjectInfo();
        }

        public override ObjectItemNotInContainer GetObjectItemNotInContainer()
        {
            RefreshEffects();
            return base.GetObjectItemNotInContainer();
        }
    }
}