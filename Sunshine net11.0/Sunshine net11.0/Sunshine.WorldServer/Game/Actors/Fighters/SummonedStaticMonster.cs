using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Maps;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    /// <summary>
    /// Static summons (blocking trees, etc.) — cannot take a turn; aligned with Rollback SummonedStaticMonster.
    /// </summary>
    public sealed class SummonedStaticMonster : SummonedMonster
    {
        public SummonedStaticMonster(Monster monster, FightActor summoner, ObjectPosition position)
            : base(monster, summoner, position)
        {
        }

        public override bool CanPlayTurn => false;
    }
}
