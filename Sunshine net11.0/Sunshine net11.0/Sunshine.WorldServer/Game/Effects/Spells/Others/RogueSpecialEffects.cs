using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Others
{
    [EffectHandler(EffectsEnum.Effect_1024)]
    [EffectHandler(EffectsEnum.Effect_CreateIllusions)]
    public class CreateIllusions : SpellEffectHandler
    {
        public override void Apply()
        {
            if (Caster == null || Fight == null || Fight.Map == null || Caster.Position?.Point == null)
                return;

            var originalPoint = Caster.Position.Point;
            var targetedPoint = MapPoint.GetPoint(TargetedCell);
            if (targetedPoint == null || originalPoint.CellId == targetedPoint.CellId)
                return;

            var occupied = Fight.GetOneFighter(TargetedCell);
            if (occupied != null && occupied != Caster && occupied.IsAlive)
                return;

            short distance = (short)originalPoint.DistanceToCell(targetedPoint);
            if (distance <= 0)
                return;

            var direction = originalPoint.OrientationTo(targetedPoint, false);
            bool isEvenDirection = ((short)direction % 2) == 0;
            int maxIllusions = DiceNum > 0 ? (int)DiceNum : 3;
            int created = 0;

            Caster.Position.Cell = TargetedCell;
            Caster.Position.Direction = direction;
            ActionsHandler.SendGameActionFightTeleportOnSameMapMessage(Fight.Clients, Caster, Caster, TargetedCell);

            foreach (DirectionsEnum dir in Enum.GetValues(typeof(DirectionsEnum)))
            {
                if (created >= maxIllusions)
                    break;

                if (dir == direction)
                    continue;

                if (isEvenDirection != ((((short)dir) % 2) == 0))
                    continue;

                var destinationPoint = originalPoint.GetCellInDirection(dir, distance);
                if (destinationPoint == null)
                    continue;

                short cellId = destinationPoint.CellId;
                if (cellId < 0 || cellId >= Fight.Map.Cells.Length)
                    continue;

                if (!Fight.Map.Cells[cellId].Walkable || !Fight.IsCellFree(cellId))
                    continue;

                var image = new RogueImageFighter(Caster, new ObjectPosition(Fight.Map, cellId, direction));
                Caster.AddSummon(image);

                int indexSummoner = Fight.TimeLine.Fighters.IndexOf(Caster);
                if (indexSummoner >= 0)
                    Fight.TimeLine.Fighters.Insert(indexSummoner + 1, image);
                else
                    Fight.TimeLine.AddFighter(image);

                if (Caster.IsAttacker())
                    Fight.Team.AddAttacker(Caster, image);
                else
                    Fight.Team.AddDefender(Caster, image);

                ActionsHandler.SendGameActionFightSummonMessage(Fight.Clients, image);
                Fight.TriggerMarks(image.Position.Cell, image, TriggerTypeEnum.MOVE);
                created++;
            }

            if (created > 0)
            {
                ContextHandler.SendGameFightUpdateTeamMessage(Fight.Clients, Caster.Team);
                ContextHandler.SendGameFightTurnListMessage(Fight.Clients, Fight);
            }
        }
    }


    [EffectHandler(EffectsEnum.Effect_792)]
    public class Effect792Fallback : SpellEffectHandler
    {
        public override void Apply()
        {
        }
    }

    [EffectHandler(EffectsEnum.Effect_1031)]
    public class SkipTurnNow : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors().Where(x => x != null && x.IsAlive).ToArray())
            {
                if (actor.IsFighterTurn())
                    actor.EndTurn();
            }
        }
    }

    [EffectHandler(EffectsEnum.Effect_1060)]
    public class Effect1060Fallback : SpellEffectHandler
    {
        public override void Apply()
        {
        }
    }
}
