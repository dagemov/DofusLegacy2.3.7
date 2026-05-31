using Sunshine.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Interactives.Skills
{
    public class SkillManager : Singleton<SkillManager>
    {
        public Dictionary<int, Func<Skill>> Skills = new Dictionary<int, Func<Skill>>();
    }
}
