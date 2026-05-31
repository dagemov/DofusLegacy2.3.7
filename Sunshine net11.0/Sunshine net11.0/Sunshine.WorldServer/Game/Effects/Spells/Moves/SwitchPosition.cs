using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_SwitchPosition)]
    public class SwitchPosition : SpellEffectHandler
    {
        public override void Apply()
        {
            FightActor fightActor = base.GetAffectedActors().FirstOrDefault<FightActor>();
            if (fightActor != null)
                Caster.ExchangePositions(fightActor);
        }
    }
}
