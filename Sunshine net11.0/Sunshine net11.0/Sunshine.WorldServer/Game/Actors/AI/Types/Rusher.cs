using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI.Types
{
    [AIHandler(AIEnum.RUSHER)]
    public class Rusher : AITypeHandler
    {
        public override void Play()
        {
            var fighters = this.GetAllFightersAdjacent(Fighter.Stats.MP.TotalMax).Where(x => !(x is ISummoned));

            var friends = this.GetAllFightersAdjacent(Fighter.Stats.MP.TotalMax, true);

            var fightersFarAway = this.GetAllFightersFarAway(Fighter.Stats.MP.TotalMax).Where(x => !(x is ISummoned));

            this.Move();

            if (this.CanTeleport())
                this.ExecuteTeleport(fightersFarAway);

            if (this.CanPull())
                this.ExecutePull(fightersFarAway);

            if (this.CanSummon())
                this.ExecuteSummon();

            if (this.CanBoost())
                this.ExecuteBoost(friends);

            if (this.CanHeal())
                this.ExecuteHeal(friends);

            if (this.CanDebuffStats())
                this.ExecuteStatsDebuff(fighters);

            if (this.CanRox())
                this.ExecuteRox(fighters);

        }

        private void Move()
        {
            AsyncRandom rdn = new AsyncRandom();

            MapPoint point = null;

            MapPoint[] points = null;

            List<short> cells = new List<short>();

            short lastCell = Fighter.Position.Cell;

            MapPoint lastMapPoint = new MapPoint(lastCell);

            FightActor fighter = GetFirstFighterWithLessHealth(GetAllFightersAdjacent(Fighter.Stats.MP.TotalMax).Where(x => !(x is ISummoned)));

            for (int i = 0; i < Fighter.Stats.MP.TotalMax; i++)
            {
                lastMapPoint = new MapPoint(lastCell);

                points = lastMapPoint.GetAdjacentCells((short entry) => Fight.IsCellFree(entry)).ToArray<MapPoint>();

                if (fighter == null)
                {
                    fighter = GetFirstFighterWithLessHealth(GetAllFightersFarAway(Fighter.Stats.MP.TotalMax).Where(x => !(x is ISummoned)));

                    if (fighter == null || (fighter != null && fighter.Position.Point.DistanceToCell(Fighter.Position.Point) <= 1))
                    {
                        if (points.Length <= 0)
                            break;

                        point = points[rdn.Next(0, points.Length - 1)];
                    }
                    else
                    {
                        if (this.CanTeleport())
                            this.ExecuteTeleport(new FightActor[0], fighter);

                        if (this.CanPull())
                            this.ExecutePull(new FightActor[0], fighter);

                        if (this.CanSummon())
                            this.ExecuteSummon();

                        if (this.CanBoost())
                            this.ExecuteBoost(new FightActor[0]);

                        if (this.CanHeal())
                            this.ExecuteHeal(new FightActor[0]);

                        if (this.CanDebuffStats())
                            this.ExecuteStatsDebuff(new FightActor[0], fighter);

                        if (this.CanRox())
                            this.ExecuteRox(new FightActor[0], fighter);

                        base.Move(Fighter.Position.Cell, fighter.Position.Cell);
                        return;
                    }
                }
                else
                {
                    if (this.CanTeleport())
                        this.ExecuteTeleport(new FightActor[0], fighter);

                    if (this.CanPull())
                        this.ExecutePull(new FightActor[0], fighter);

                    if (this.CanSummon())
                        this.ExecuteSummon();

                    if (this.CanBoost())
                        this.ExecuteBoost(new FightActor[0]);

                    if (this.CanHeal())
                        this.ExecuteHeal(new FightActor[0]);

                    if (this.CanDebuffStats())
                        this.ExecuteStatsDebuff(new FightActor[0], fighter);

                    if (this.CanRox())
                        this.ExecuteRox(new FightActor[0], fighter);

                    base.Move(Fighter.Position.Cell, fighter.Position.Cell);
                    return;
                }

                if (point != null && Fight.IsCellFree(point.CellId))
                {
                    cells.Add(point.CellId);

                    lastCell = point.CellId;

                    if (fighter != null && lastMapPoint.DistanceToCell(fighter.Position.Point) <= 1)
                        break;
                }
            }

            base.Move(Fighter.Position.Cell, cells.Count <= 0 ? (short)-1 : cells.Last(), cells.Count <= 0);
        }
    }
}
