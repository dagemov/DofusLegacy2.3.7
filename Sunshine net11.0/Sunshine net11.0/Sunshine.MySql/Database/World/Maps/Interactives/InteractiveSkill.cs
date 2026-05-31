using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Maps.Interactives
{
    [Table("interactives_skills")]
    public class InteractiveSkill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Interactive { get; set; }
        public int ParentJob { get; set; }
        public bool IsForgemagus { get; set; }
        public int ModifiableItemType { get; set; }
        public int GatheredRessourceItem { get; set; }
        public string CraftableItemIdsCSV { get; set; }
        public string UseAnimation { get; set; }
        public bool IsRepair { get; set; }
        public int Cursor { get; set; }
        public bool AvailableInHouse { get; set; }
    }
}
