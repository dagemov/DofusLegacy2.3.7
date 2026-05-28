using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.Auth.Worlds
{
    [Table("worlds")]
    public class World
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public bool Selectable { get; set; }
        public int Status { get; set; }
        public int RequiredRole { get; set; }
        public int CharCount { get; set; }
        public int CharCapacity { get; set; }
    }
}
