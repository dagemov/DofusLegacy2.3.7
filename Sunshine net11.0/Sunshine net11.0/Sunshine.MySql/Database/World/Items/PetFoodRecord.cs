using Dapper.Contrib.Extensions;
using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.MySql.Database.World.Items
{
    public enum PetFoodType
    {
        Items = 0,
        Monsters = 1,
        ItemsCategories = 2
    }

    [Table("pets_foods")]
    public class PetFoodRecord
    {
        [Key]
        public int Id { get; set; }
        public short PetId { get; set; }
        public short EffectId { get; set; }
        public short EffectValue { get; set; }
        public short BoostedValue { get; set; }
        public PetFoodType FoodType { get; set; }
        public string FoodInformationsCSV { get; set; }
        public byte StatIncreaseAmount { get; set; }

        [Write(false)]
        public Dictionary<short, short> FoodInformations { get; set; } = new Dictionary<short, short>();

        [Write(false)]
        public EffectsEnum EffectEnum => (EffectsEnum)EffectId;

        public void EnsureParsed()
        {
            FoodInformations.Clear();
            if (string.IsNullOrWhiteSpace(FoodInformationsCSV))
                return;

            foreach (var part in FoodInformationsCSV.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var bits = part.Split(',');
                if (!short.TryParse(bits[0], out var id))
                    continue;

                short extra = 0;
                if (bits.Length > 1)
                    short.TryParse(bits[1], out extra);

                FoodInformations[id] = extra;
            }
        }
    }
}
