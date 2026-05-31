using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class EffectHandler : Attribute
    {
        public EffectsEnum Effect { get; set; }

        public EffectHandler(EffectsEnum effect)
        {
            Effect = effect;
        }
    }
}
