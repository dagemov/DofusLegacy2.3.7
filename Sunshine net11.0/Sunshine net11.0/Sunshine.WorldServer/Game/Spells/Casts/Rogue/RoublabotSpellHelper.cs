using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Maps;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Spells.Casts.Rogue
{
    internal static class RoublabotSpellHelper
    {
        public static FightActor ResolveOwner(FightActor caster)
        {
            if (caster is SlaveFighter slave && slave.IsRoublabot && slave.Summoner != null)
                return slave.Summoner;

            return caster;
        }

        public static BombFighter GetFriendlyBombOnCell(FightActor caster, short cell)
        {
            var fight = caster?.Fight;
            var owner = ResolveOwner(caster);
            if (fight == null || owner == null)
                return null;

            var bomb = fight.GetOneFighter(cell) as BombFighter;
            if (bomb == null || !bomb.IsAlive || bomb.IsExploded)
                return null;

            return bomb.Summoner == owner ? bomb : null;
        }

        public static BombFighter GetAdjacentFriendlyBomb(FightActor caster, short targetedCell)
        {
            if (caster?.Position?.Point == null)
                return null;

            var targetedPoint = MapPoint.GetPoint(targetedCell);
            if (targetedPoint == null || targetedPoint.CellId == caster.Position.Cell)
                return null;

            var orientation = caster.Position.Point.OrientationTo(targetedPoint, true);
            var adjacent = caster.Position.Point.GetCellInDirection(orientation, 1);
            return adjacent == null ? null : GetFriendlyBombOnCell(caster, adjacent.CellId);
        }

        public static BombFighter GetFirstFriendlyBombInLine(FightActor caster, short targetedCell, int maxDistance)
        {
            if (caster?.Position?.Point == null)
                return null;

            var targetedPoint = MapPoint.GetPoint(targetedCell);
            if (targetedPoint == null || targetedPoint.CellId == caster.Position.Cell)
                return null;

            var orientation = caster.Position.Point.OrientationTo(targetedPoint, true);
            maxDistance = System.Math.Max(1, maxDistance);

            for (int i = 1; i <= maxDistance; i++)
            {
                var point = caster.Position.Point.GetCellInDirection(orientation, (short)i);
                if (point == null)
                    break;

                var bomb = GetFriendlyBombOnCell(caster, point.CellId);
                if (bomb != null)
                    return bomb;
            }

            return null;
        }

        public static bool MoveBombToCell(FightActor caster, BombFighter bomb, short targetedCell)
        {
            if (caster?.Fight == null || bomb == null || !bomb.IsAlive || bomb.IsExploded || bomb.Position?.Point == null)
                return false;

            if (bomb.HasState(SpellStatesEnum.Unmovable) || bomb.HasState(SpellStatesEnum.Rooted) || bomb.HasState(SpellStatesEnum.Gravity))
                return false;

            var targetedPoint = MapPoint.GetPoint(targetedCell);
            if (targetedPoint == null)
                return false;

            short startCell = bomb.Position.Cell;
            short endCell = startCell;
            var cells = new MapPoint(startCell).GetCellsOnLineBetween(targetedPoint);

            for (int index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                if (cell == null || cell.CellId == startCell)
                    continue;

                if (!caster.Fight.IsCellFree(cell.CellId))
                    break;

                endCell = cell.CellId;

                if (caster.Fight.ShouldTriggerOnMove(cell.CellId, bomb))
                    break;
            }

            if (endCell == startCell)
                return false;

            var direction = new MapPoint(startCell).OrientationTo(targetedPoint, true);
            Game.Effects.Spells.Moves.ForcedMovementHelper.DropCarriedActorIfNeeded(bomb);

            var slideClients = Game.Effects.Spells.Moves.ForcedMovementHelper.SendSlide(caster, bomb, startCell, endCell);

            bomb.Position.Cell = endCell;
            bomb.Position.Direction = direction;
            Game.Effects.Spells.Moves.ForcedMovementHelper.RefreshAfterForcedMove(caster, bomb, slideClients);

            BombManager.Instance.CheckWalls(caster.Fight, bomb.Summoner);
            caster.Fight.TriggerMarks(bomb.Position.Cell, bomb, TriggerTypeEnum.MOVE);
            caster.Fight.CheckFightEnd();
            return true;
        }

        public static bool Detonate(FightActor caster, Spell spell, bool critical, short targetedCell)
        {
            var owner = ResolveOwner(caster);
            if (owner == null)
                return false;

            var bombs = new HashSet<BombFighter>();

            foreach (var bomb in RogueBombSpellHelper.GetAffectedFriendlyBombs(caster, spell, critical, targetedCell, EffectsEnum.Effect_ActivateBomb, EffectsEnum.Effect_1043))
            {
                if (bomb != null && bomb.Summoner == owner)
                    bombs.Add(bomb);
            }

            var directBomb = GetFriendlyBombOnCell(caster, targetedCell);
            if (directBomb != null)
                bombs.Add(directBomb);

            foreach (var bomb in bombs.Where(x => x != null).OrderBy(x => x.Position != null && caster.Position != null ? x.Position.Point.DistanceToCell(caster.Position.Point) : 0).ToArray())
            {
                if (!bomb.IsAlive || bomb.IsExploded || bomb.Summoner != owner)
                    continue;

                if (bomb.WasJustSummonedForSameAction(owner))
                    continue;

                bomb.Explode(caster);
            }

            return bombs.Count > 0;
        }
    }
}
