using System;
using System.Collections.Generic;
using Sunshine.Protocol.IO.Tools;
using Sunshine.Protocol.Tools.D2o;

namespace Sunshine.Protocol.Tools.D2o.Classes
{
    [D2OClass("Weapon", "com.ankamagames.dofus.datacenter.items", true)]
    [Serializable]
    public class Weapon : IDataObject, IIndexedData
    {
        public int favoriteSubAreasBonus;
        public int weight;
        public int range;
        public bool bonusIsSecret;
        public int criticalHitBonus;
        public int minRange;
        public int descriptionId;
        public List<uint> recipeIds;
        public bool etheral;
        public int appearanceId;
        public int id;
        public bool cursed;
        public int level;
        public bool castTestLos;
        public List<uint> favoriteSubAreas;
        public int criticalFailureProbability;
        public bool hideEffects;
        public bool targetable;
        public string criteria;
        public int criticalHitProbability;
        public bool twoHanded;
        public int itemSetId;
        public int nameId;
        public int price;
        public int apCost;
        public bool usable;
        public bool castInLine;
        public List<object> possibleEffects;
        public int useAnimationId;
        public int iconId;
        public int typeId;

        int IIndexedData.Id => id;
    }
}
