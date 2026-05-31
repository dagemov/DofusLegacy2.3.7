using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;
namespace Sunshine.WorldServer.Game.Fights.History
{
	public class SpellHistoryEntry
	{
		public Spell Spell
		{
			get;
			private set;
		}

		public FightActor Caster
		{
			get;
			private set;
		}

		public FightActor Target
		{
			get;
			private set;
		}

		public int CastRound
		{
			get;
			private set;
		}

		public SpellHistoryEntry(Spell spell, FightActor caster, FightActor target, int castRound)
		{
			this.Spell = spell;
			this.Caster = caster;
			this.Target = target;
			this.CastRound = castRound;
		}

        public int GetMinCastInterval()
        {
            if (Caster.CustomCoolDown.ContainsKey((ushort)Spell.Id))
            {
                var cast = Caster.CustomCoolDown[(ushort)Spell.Id];
                Caster.CustomCoolDown.Remove((ushort)Spell.Id);
                return cast;
            }
            return Spell.Template.MinCastInterval;
        }


        public int GetElapsedRounds(int currentRound)
		{
			return currentRound - this.CastRound;
		}

        public bool IsGlobalCooldownActive(int currentRound)
        {
            return GetElapsedRounds(currentRound) < GetMinCastInterval();
        }
    }
}
