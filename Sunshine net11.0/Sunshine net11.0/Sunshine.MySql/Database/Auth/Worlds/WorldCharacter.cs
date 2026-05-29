using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Auth.Worlds
{
    [Table("worlds_characters")]
    public class WorldCharacter
    {
        public int Account { get; set; }
        public int Owner { get; set; }
        public int World { get; set; }
    }
}
