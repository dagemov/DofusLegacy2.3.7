using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Others
{
    [EffectHandler((EffectsEnum)1026)]
    public class Roulette : SpellEffectHandler
    {
        public override void Apply()
        {
            // Roulette est désormais pilotée par RouletteHandler
            // avec la logique SaveKrosmoz :
            // un seul effet choisi aléatoirement + un effet final d'annonce.
        }
    }
}
