using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps
{
    public class Cell
    {
        private Character _character; 

        public Cell(Character character)
        {
           _character = character;
        }

        public Map Map { get { return _character.Map; } }

        public short Id
        {
            get { return _character.Record.CellId; }
            set { _character.Record.CellId = value; }
        }              
    }
}
