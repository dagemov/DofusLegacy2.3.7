using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Spells
{
    [Table("spells_levels")]
    public class SpellTemplate
    {
        public int SpellId { get; set; }
        public int SpellBreed { get; set; }
        public int ApCost { get; set; }
        public int Range { get; set; }
        public bool CastInLine { get; set; }
        public bool CastInDiagonal { get; set; }
        public bool CastTestLos { get; set; }
        public int CriticalHitProbability { get; set; }
        public string StatesRequiredCSV { get; set; }
        public int CriticalFailureProbability { get; set; }
        public bool NeedFreeCell { get; set; }
        public bool NeedFreeTrapCell { get; set; }
        public bool NeedTakenCell { get; set; }
        public bool RangeCanBeBoosted { get; set; }
        public int MaxStack { get; set; }
        public int MaxCastPerTurn { get; set; }
        public int MaxCastPerTarget { get; set; }
        public int MinCastInterval { get; set; }
        public int InitialCooldown { get; set; }
        public int GlobalCooldown { get; set; }
        public int MinPlayerLevel { get; set; }
        public bool CriticalFailureEndsTurn { get; set; }
        public bool HideEffects { get; set; }
        public int MinRange { get; set; }
        public string StatesForbiddenCSV { get; set; }
        public string Effects { get; set; }
        public string CriticalEffects { get; set; }

        // Rollback/Stump binary spell effects fallback
        public byte[] BinaryEffect { get; set; }
        public byte[] BinaryEffects { get; set; }
        public byte[] BinaryCriticalEffect { get; set; }
        public byte[] BinaryCriticalEffects { get; set; }
    }
}
