using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.Buffs
{
    public abstract class Buff
    {
        public int Id { get; set; }

        public FightActor Caster { get; set; }

        public FightActor Target { get; set; }

        public BuffTypeEnum Type { get; set; }

        public Spell Spell { get; set; }

        public Effect Effect { get; set; }
        
        public short Duration { get; set; }
       
        public bool Dispellable { get; set; }

        public short? ActionId { get; set; }
 
        public bool IsBuffEnded()
        {
            return this.Duration <= 0;
        }

        public virtual short GetActionId()
        {
            return ActionId ?? (short)Effect.Id;
        }

        public virtual short GetUpdateActionId()
        {
            return (short)ActionsEnum.ACTION_CHARACTER_UPDATE_BOOST;
        }

        public abstract void Apply();

        public abstract void Dispell();

        public abstract AbstractFightDispellableEffect GetAbstractFightDispellableEffect();

    }
}
