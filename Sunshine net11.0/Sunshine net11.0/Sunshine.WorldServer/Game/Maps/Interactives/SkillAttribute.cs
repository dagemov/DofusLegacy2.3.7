using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Interactives
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SkillHandler : Attribute
    {
        public int Id { get; set; }

        public SkillHandler(int id)
        {
            Id = id;
        }
    }
}
