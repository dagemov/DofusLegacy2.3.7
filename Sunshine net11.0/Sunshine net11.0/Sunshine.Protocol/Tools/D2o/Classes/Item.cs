using System;
using System.Collections.Generic;
using Sunshine.Protocol.IO.Tools;
using Sunshine.Protocol.Tools.D2o;

namespace Sunshine.Protocol.Tools.D2o.Classes
{
    [D2OClass("Item", "com.ankamagames.dofus.datacenter.items", true)]
    [Serializable]
    public class Item : IDataObject, IIndexedData
    {
        public int id;
        public int nameId;
        public int typeId;
        public int descriptionId;
        public int iconId;
        public int level;
        public int weight;
        public bool cursed;
        public int useAnimationId;
        public bool usable;
        public bool targetable;
        public int price;
        public bool twoHanded;
        public bool etheral;
        public int itemSetId;
        public string criteria;
        public bool hideEffects;
        public int appearanceId;
        public List<uint> recipeIds;
        public bool bonusIsSecret;
        public List<object> possibleEffects;
        public List<uint> favoriteSubAreas;
        public int favoriteSubAreasBonus;

        int IIndexedData.Id => id;
    }
}
