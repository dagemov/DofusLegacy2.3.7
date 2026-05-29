using Sunshine.MySql.Database.World.Maps.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sunshine.WorldServer.Game.Maps.Triggers
{
    public class Trigger
    {
        private TriggerSpawn _record;

        public Trigger(TriggerSpawn record)
        {
            _record = record;
            Parameters = record.ParametersCSV == null || record.ParametersCSV == "" ? new List<int>() :
                     record.ParametersCSV.Split(',').Select(x => int.Parse(x)).ToList();
        }

        public int Map { get { return _record.Map; } }

        public short Cell { get { return _record.Cell; } }

        public byte Type { get { return _record.Type; } }

        public List<int> Parameters { get; set; }
    }
}
