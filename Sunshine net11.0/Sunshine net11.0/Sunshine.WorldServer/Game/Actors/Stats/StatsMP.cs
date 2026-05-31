using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Actors.Stats
{
    public class StatsMP : StatsData
    {
        public StatsMP(int baseValue)
            : base(baseValue, null)
        {
        }

        public int Used { get; set; }

        public override int Total
        {
            get
            {
                return (TotalMax - Used) > 0 ? (TotalMax - Used) : 0;
            }
        }

        public override int TotalMax
        {
            get
            {
                return Base + Equiped + Context;
            }
        }
    }
}
