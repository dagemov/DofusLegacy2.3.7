using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Handlers.Actions;

namespace Sunshine.WorldServer.Game.Effects.Spells.Moves
{
    [EffectHandler(EffectsEnum.Effect_Dodge)]
    public class Dodge : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Fight.GetOneFighter(TargetedCell) == null)
            {
                Caster.Position.Cell = TargetedCell;
                ActionsHandler.SendGameActionFightTeleportOnSameMapMessage(Fight.Clients, Caster, Caster, TargetedCell);
                Fight.TriggerMarks(Caster.Position.Cell, Caster, TriggerTypeEnum.MOVE);
            }
        }
    }
}
