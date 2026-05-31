using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps
{
    public class Element
    {
        public short Cell { get; set; }

        public uint Id { get; set; }

        public Element(short cell, uint element)
        {
            Cell = cell;
            Id = element;
        }
    }
}
