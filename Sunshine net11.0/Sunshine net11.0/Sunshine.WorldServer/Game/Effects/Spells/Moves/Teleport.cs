using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Handlers.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_Teleport)]
    public class Teleport : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Fight.GetOneFighter(TargetedCell) == null)
            {
                Caster.Position.Cell = TargetedCell;
                ActionsHandler.SendGameActionFightTeleportOnSameMapMessage(Fight.Clients, Caster, Caster, TargetedCell);

                if (Caster is BombFighter movedBomb)
                    BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);

                Fight.TriggerMarks(Caster.Position.Cell, Caster, TriggerTypeEnum.MOVE);
            }
        }
    }
}
