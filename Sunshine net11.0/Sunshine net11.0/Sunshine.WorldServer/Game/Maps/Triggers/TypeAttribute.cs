using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Triggers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeHandler : Attribute
    {
        public byte Id { get; set; }

        public TypeHandler(byte id)
        {
            Id = id;
        }
    }
}
