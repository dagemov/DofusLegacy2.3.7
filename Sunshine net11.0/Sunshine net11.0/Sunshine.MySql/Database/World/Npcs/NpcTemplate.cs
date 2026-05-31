using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.MySql.Database.World.Npcs
{
    [Table("npcs")]
    public class NpcTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }       
        public string EntityLook { get; set; }
        public bool Gender { get; set; }
        public bool HasQuest { get; set; }
        public string DialogMessagesIdCSV { get; set; }
        public string DialogRepliesIdCSV { get; set; }
        public string ActionsIdCSV { get; set; }
        public int Token { get; set; }
    }
}
