using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Stats
{
    public class StatsData
    {
        private int _base;
        private int _equiped;
        private int _context;
        private int? _limit;

        public StatsData(int baseValue, int? limitValue)
        {
            _base = baseValue;
            _limit = limitValue;
        }

        public virtual int Base
        {
            get { return _base; }
            set { _base = value; }
        }

        public virtual int Equiped
        {
            get { return _equiped; }
            set { _equiped = value; }
        }

        public virtual int Context
        {
            get { return _context; }
            set { _context = value; }
        }

        public virtual int? Limit
        {
            get { return _limit.HasValue ? _limit : null; }
            set { _limit = value; }          
        }
        
        public virtual int Total
        {
            get
            {
                if (Limit.HasValue && Base + Equiped > Limit)
                    return Limit.Value;
                else
                    return (int)(Base + Equiped);
            }
        }

        public virtual int TotalMax
        {
            get
            {
                if (Limit.HasValue && Total + Context > Limit)
                    return Limit.Value;
                else
                    return (int)((Total + Context) >= 0 ? (int)(Total + Context) : 0);
            }
        }
        
        public StatsData Clone()
        {
            return (StatsData)this.MemberwiseClone();
        }

        public static implicit operator CharacterBaseCharacteristic(StatsData stats)
        {
            return new CharacterBaseCharacteristic((short)stats.Base, (short)stats.Equiped, 0, (short)stats.Context);
        }
    }
}
