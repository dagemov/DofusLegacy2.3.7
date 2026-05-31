using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Recipes
{
    [Table("recipes")]
    public class RecipeTemplate
    {
        public int Result { get; set; }
        public int ResultLevel { get; set; }
        public string IngredientIdsCSV { get; set; }
        public string QuantitiesCSV { get; set; }
        public int Skill { get; set; }
    }
}
