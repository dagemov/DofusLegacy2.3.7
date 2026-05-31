using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Items
{
    public abstract class ItemRecord
    {
        public abstract int Id { get; }
        public abstract byte TypeId { get; }
        public abstract short Level { get; }
        public abstract short AppearanceId { get; }
        public abstract short ItemSetId { get; }
        public abstract string Criteria { get; }
        public abstract int Weight { get; }
        public abstract double Price { get; }
        public abstract bool TwoHanded { get; }
        public abstract bool Usable { get; }
        public abstract bool Targetable { get; }
        public abstract string RecipeIdsCSV { get; }
        public List<Effect> EffectsBase { get; set; }
        public List<List<Effect>> EffectSets { get; set; }
        public object Name { get; internal set; }

        public ItemRecord Clone()
        {
            return (ItemRecord)this.MemberwiseClone();
        }
    }
}
