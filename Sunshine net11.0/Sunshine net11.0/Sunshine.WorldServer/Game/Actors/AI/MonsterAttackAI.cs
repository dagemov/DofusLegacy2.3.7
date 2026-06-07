using Sunshine.MySql.Database.World.Spells;
using Sunshine.BaseServer.Configuration;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Maps.Shapes;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Fights.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI
{
    public static class MonsterAttackAI
    {
        private static int ActionDelayMs => Math.Max(0, GameConfig.GetInt("MonsterActionDelayMs", 60));
        private static int MovementDelayMs => Math.Max(ActionDelayMs, GameConfig.GetInt("MonsterMovementDelayMs", 90));
        private static int ForcedMovementDelayMs => Math.Max(MovementDelayMs, GameConfig.GetInt("MonsterForcedMovementDelayMs", 120));

        public static async Task PlayAsync(Monster monster, FightActor fighter)
        {
            if (monster == null || fighter == null || fighter.Fight == null || fighter.Map == null || fighter.Position == null || !fighter.IsAlive)
                return;

            if (FrigostBossMechanics.TryPlayBenHamrackMechanic(fighter))
                return;

            var spells = (monster.Spells ?? new List<Spell>())
                .Where(x => x != null && x.Template != null)
                .Distinct()
                .OrderBy(x => x.Template.ApCost)
                .ThenBy(x => x.Template.Range)
                .ToList();

            if (spells.Count == 0)
            {
                await TryMoveCloserToEnemyAsync(fighter);
                return;
            }

            var attackSpells = spells.Where(IsAttackSpell).OrderByDescending(GetSpellPriority).ToList();
            var supportSpells = spells.Where(x => !IsAttackSpell(x) && (IsBoostSpell(x) || IsHealSpell(x) || IsSummonSpell(x))).OrderByDescending(GetSpellPriority).ToList();
            var fallbackSpells = spells.Where(x => !attackSpells.Contains(x) && !supportSpells.Contains(x)).OrderByDescending(GetSpellPriority).ToList();

            bool movedInLoop = false;
            bool played = true;
            int safety = 20;

            while (played && safety-- > 0 && fighter.IsAlive && fighter.IsFighterTurn())
            {
                played = false;

                if (await TryCastBestSpellAsync(fighter, attackSpells))
                {
                    played = true;
                    continue;
                }

                if (await TryMoveAndCastAsync(fighter, attackSpells))
                {
                    played = true;
                    movedInLoop = true;
                    continue;
                }

                if (await TryCastBestSpellAsync(fighter, fallbackSpells))
                {
                    played = true;
                    continue;
                }

                if (await TryMoveAndCastAsync(fighter, fallbackSpells))
                {
                    played = true;
                    movedInLoop = true;
                    continue;
                }

                if (await TryCastSupportAsync(fighter, supportSpells))
                {
                    played = true;
                    continue;
                }

                if (await TryMoveCloserToEnemyAsync(fighter))
                {
                    played = true;
                    movedInLoop = true;
                    continue;
                }
            }

            // Siempre intentar acercarse al enemigo al final (by charly)
            if (fighter.IsAlive && fighter.IsFighterTurn() && fighter.Stats.MP.Total > 0)
                await TryMoveCloserToEnemyAsync(fighter);

            // Stump-style final loop: intentar cada hechizo restante con move+cast
            if (fighter.IsAlive && fighter.IsFighterTurn() && fighter.Stats.AP.Total > 0)
            {
                foreach (var spell in spells)
                {
                    if (!fighter.IsAlive || !fighter.IsFighterTurn() || fighter.Stats.AP.Total <= 0)
                        break;

                    if (!CanUseSpell(fighter, spell))
                        continue;

                    var enemies = GetEnemies(fighter).ToList();
                    if (enemies.Count == 0)
                        break;

                    var target = enemies.First();
                    var targetCell = target.Position.Cell;

                    if (fighter.CanCastSpell(spell, targetCell) == SpellCastResult.OK)
                    {
                        int apBefore = fighter.Stats.AP.Total;
                        fighter.CastSpell(spell, targetCell);
                        if (fighter.Stats.AP.Total < apBefore)
                        {
                            await PauseAfterActionAsync(fighter, spell);
                            // MoveNearTo despues de castear
                            if (fighter.IsAlive && fighter.IsFighterTurn() && fighter.Stats.MP.Total > 0)
                                await TryMoveCloserToEnemyAsync(fighter);
                        }
                    }
                    else
                    {
                        // MoveNearTo primero, luego castear si es posible
                        if (fighter.Stats.MP.Total > 0)
                            await TryMoveCloserToEnemyAsync(fighter);

                        if (fighter.IsAlive && fighter.IsFighterTurn() && fighter.CanCastSpell(spell, targetCell) == SpellCastResult.OK)
                        {
                            int apBefore = fighter.Stats.AP.Total;
                            fighter.CastSpell(spell, targetCell);
                            if (fighter.Stats.AP.Total < apBefore)
                                await PauseAfterActionAsync(fighter, spell);
                        }
                    }
                }
            }

            // Si aun tiene PM y no se movio, movimiento aleatorio
            if (fighter.IsAlive && fighter.IsFighterTurn() && fighter.Stats.MP.Total > 0 && !movedInLoop)
                await TryRandomMoveAsync(fighter);
        }

        private static async Task PauseAfterActionAsync(FightActor fighter, Spell spell = null, bool moved = false)
        {
            if (fighter == null || fighter.Fight == null || fighter.Fight.State != FightStateEnum.Fighting)
                return;

            int delay = moved ? MovementDelayMs : ActionDelayMs;
            if (spell != null && UsesForcedMovement(spell))
                delay = ForcedMovementDelayMs;

            if (delay <= 0)
                return;

            await Task.Delay(delay);
        }

        private static bool UsesForcedMovement(Spell spell)
        {
            var effects = (spell?.Effects ?? new List<Effect>())
                .Concat(spell?.CriticalEffects ?? new List<Effect>());

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                switch ((EffectsEnum)effect.Id)
                {
                    case EffectsEnum.Effect_Teleport:
                    case EffectsEnum.Effect_Push:
                    case EffectsEnum.Effect_PullForward:
                    case EffectsEnum.Effect_PushBack:
                    case EffectsEnum.Effect_BePulled:
                    case EffectsEnum.Effect_Push_1103:
                        return true;
                }
            }

            return false;
        }

        private static async Task<bool> TryCastBestSpellAsync(FightActor fighter, List<Spell> spells)
        {
            if (spells == null || spells.Count == 0)
                return false;

            foreach (var target in GetEnemies(fighter))
            {
                foreach (var spell in spells)
                {
                    if (!CanUseSpell(fighter, spell))
                        continue;

                    if (fighter.CanCastSpell(spell, target.Position.Cell) != SpellCastResult.OK)
                        continue;

                    int apBefore = fighter.Stats.AP.Total;
                    fighter.CastSpell(spell, target.Position.Cell);
                    if (fighter.Stats.AP.Total < apBefore)
                    {
                        CombatTelemetry.LogTurnEvent(
                            "AiActionSelected",
                            fighter.Fight,
                            fighter,
                            detail: $"actionType=CastSpell spellId={spell.Id} targetCell={target.Position.Cell}");
                        await PauseAfterActionAsync(fighter, spell);
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task<bool> TryCastSupportAsync(FightActor fighter, List<Spell> spells)
        {
            if (spells == null || spells.Count == 0)
                return false;

            foreach (var spell in spells)
            {
                if (!CanUseSpell(fighter, spell))
                    continue;

                if (fighter.CanCastSpell(spell, fighter.Position.Cell) == SpellCastResult.OK)
                {
                    int apBefore = fighter.Stats.AP.Total;
                    fighter.CastSpell(spell, fighter.Position.Cell);
                    if (fighter.Stats.AP.Total < apBefore)
                    {
                        await PauseAfterActionAsync(fighter, spell);
                        return true;
                    }
                }

                foreach (var ally in GetAllies(fighter))
                {
                    if (fighter.CanCastSpell(spell, ally.Position.Cell) != SpellCastResult.OK)
                        continue;

                    int apBefore = fighter.Stats.AP.Total;
                    fighter.CastSpell(spell, ally.Position.Cell);
                    if (fighter.Stats.AP.Total < apBefore)
                    {
                        await PauseAfterActionAsync(fighter, spell);
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task<bool> TryMoveAndCastAsync(FightActor fighter, List<Spell> spells)
        {
            if (spells == null || spells.Count == 0)
                return false;

            int remainingMp = fighter.Stats.MP.Total;
            if (remainingMp <= 0)
                return false;

            var fighterPoint = fighter.Position.Point;
            var pathFinder = new Pathfinder(fighter.Fight.Map.CellsInfoProvider);
            Path bestPath = null;
            Spell bestSpell = null;
            FightActor bestTarget = null;
            int bestCost = int.MaxValue;
            int bestDistance = int.MaxValue;

            foreach (var target in GetEnemies(fighter))
            {
                foreach (var spell in spells)
                {
                    if (!CanUseSpell(fighter, spell))
                        continue;

                    for (short cell = 0; cell < 560; cell++)
                    {
                        if (!IsValidMoveCell(fighter, cell))
                            continue;

                        var cellCoord = global::Sunshine.WorldServer.Game.Maps.MapPoint.CellIdToCoord((uint)cell);
                        if (Math.Abs(fighterPoint.X - cellCoord.X) + Math.Abs(fighterPoint.Y - cellCoord.Y) > remainingMp)
                            continue;

                        if (!CanCastFromCell(fighter, spell, cell, target.Position.Cell))
                            continue;

                        var path = pathFinder.FindPath(fighter.Position.Cell, cell, false, remainingMp);
                        if (path == null || path.IsEmpty())
                            continue;

                        if (path.EndCell != cell)
                            continue;

                        int distanceAfterMove = Math.Abs(target.Position.Point.X - path.EndPathPosition.Point.X) + Math.Abs(target.Position.Point.Y - path.EndPathPosition.Point.Y);
                        if (path.MPCost < bestCost || (path.MPCost == bestCost && distanceAfterMove < bestDistance))
                        {
                            bestCost = path.MPCost;
                            bestDistance = distanceAfterMove;
                            bestPath = path;
                            bestSpell = spell;
                            bestTarget = target;
                        }
                    }
                }
            }

            if (bestPath == null || bestPath.IsEmpty())
                return false;

            fighter.StartMove(bestPath);
            await PauseAfterActionAsync(fighter, moved: true);

            if (!fighter.IsAlive || !fighter.IsFighterTurn())
                return true;

            if (bestSpell != null && bestTarget != null)
            {
                if (fighter.CanCastSpell(bestSpell, bestTarget.Position.Cell) == SpellCastResult.OK)
                {
                    int apBefore = fighter.Stats.AP.Total;
                    fighter.CastSpell(bestSpell, bestTarget.Position.Cell);
                    if (fighter.Stats.AP.Total < apBefore)
                    {
                        await PauseAfterActionAsync(fighter, bestSpell);
                        return true;
                    }
                }
            }

            return true;
        }

        private static async Task<bool> TryMoveCloserToEnemyAsync(FightActor fighter)
        {
            int remainingMp = fighter.Stats.MP.Total;
            if (remainingMp <= 0)
                return false;

            var enemies = GetEnemies(fighter).ToList();
            if (enemies.Count == 0)
                return false;

            var target = enemies.First();
            var targetCell = target.Position.Cell;
            var targetPoint = target.Position.Point;
            var pathFinder = new Pathfinder(fighter.Fight.Map.CellsInfoProvider);

            // 1) Greedy: intentar paso a paso hacia el enemigo usando direccion directa
            var fighterPoint = fighter.Position.Point;
            var movedViaGreedy = false;
            for (int step = 0; step < remainingMp; step++)
            {
                if (!fighter.IsAlive || !fighter.IsFighterTurn() || fighter.Stats.MP.Total <= 0)
                    break;

                var currentCell = fighter.Position.Cell;
                var currentPoint = fighter.Position.Point;

                // Encontrar la direccion que mas reduce la distancia al enemigo
                var bestDir = DirectionsEnum.DIRECTION_EAST;
                int bestDirDist = Math.Abs(currentPoint.X - targetPoint.X) + Math.Abs(currentPoint.Y - targetPoint.Y);
                bool foundStep = false;

                foreach (var dir in new[] {
                    DirectionsEnum.DIRECTION_NORTH_EAST,
                    DirectionsEnum.DIRECTION_SOUTH_EAST,
                    DirectionsEnum.DIRECTION_SOUTH_WEST,
                    DirectionsEnum.DIRECTION_NORTH_WEST })
                {
                    var neighbor = currentPoint.GetNearestCellInDirection(dir);
                    if (neighbor == null)
                        continue;

                    var nextCell = neighbor.CellId;
                    if (!IsValidMoveCell(fighter, (short)nextCell))
                        continue;

                    int dist = Math.Abs(neighbor.X - targetPoint.X) + Math.Abs(neighbor.Y - targetPoint.Y);
                    if (dist < bestDirDist)
                    {
                        bestDirDist = dist;
                        bestDir = dir;
                        foundStep = true;
                    }
                }

                if (!foundStep)
                    break;

                var stepCell = currentPoint.GetNearestCellInDirection(bestDir);
                if (stepCell == null)
                    break;

                var stepPath = pathFinder.FindPath(fighter.Position.Cell, stepCell.CellId, false, 1);
                if (stepPath == null || stepPath.IsEmpty() || stepPath.MPCost <= 0)
                    break;

                if (stepPath.EndCell != stepCell.CellId)
                    break;

                fighter.StartMove(stepPath);
                movedViaGreedy = true;
                await PauseAfterActionAsync(fighter, moved: true);
            }

            if (movedViaGreedy)
                return true;

            // 2) Pathfind directo hacia el enemigo (el pathfinder trunca por MP)
            var directPath = pathFinder.FindPath(fighter.Position.Cell, targetCell, false, remainingMp);
            if (directPath != null && !directPath.IsEmpty() && directPath.MPCost > 0)
            {
                fighter.StartMove(directPath);
                await PauseAfterActionAsync(fighter, moved: true);
                return true;
            }

            // 3) Fallback: buscar la celda reachable mas cercana al enemigo
            Path bestPath = null;
            int bestDist = int.MaxValue;

            for (short cell = 0; cell < 560; cell++)
            {
                if (!IsValidMoveCell(fighter, cell))
                    continue;

                var cellCoord = global::Sunshine.WorldServer.Game.Maps.MapPoint.CellIdToCoord((uint)cell);
                if (Math.Abs(fighterPoint.X - cellCoord.X) + Math.Abs(fighterPoint.Y - cellCoord.Y) > remainingMp)
                    continue;

                var path = pathFinder.FindPath(fighter.Position.Cell, cell, false, remainingMp);
                if (path == null || path.IsEmpty())
                    continue;

                if (path.EndCell != cell)
                    continue;

                int dist = Math.Abs(targetPoint.X - cellCoord.X) + Math.Abs(targetPoint.Y - cellCoord.Y);
                if (bestPath == null || dist < bestDist || (dist == bestDist && path.MPCost < bestPath.MPCost))
                {
                    bestDist = dist;
                    bestPath = path;
                }
            }

            if (bestPath == null || bestPath.IsEmpty() || bestPath.MPCost <= 0)
                return false;

            fighter.StartMove(bestPath);
            await PauseAfterActionAsync(fighter, moved: true);
            return true;
        }

        private static async Task<bool> TryRandomMoveAsync(FightActor fighter)
        {
            int remainingMp = fighter.Stats.MP.Total;
            if (remainingMp <= 0)
                return false;

            var pathFinder = new Pathfinder(fighter.Fight.Map.CellsInfoProvider);
            List<Path> candidates = new List<Path>();

            for (short cell = 0; cell < 560; cell++)
            {
                if (!IsValidMoveCell(fighter, cell))
                    continue;

                var cellCoord = global::Sunshine.WorldServer.Game.Maps.MapPoint.CellIdToCoord((uint)cell);
                if (Math.Abs(fighter.Position.Point.X - cellCoord.X) + Math.Abs(fighter.Position.Point.Y - cellCoord.Y) > remainingMp)
                    continue;

                var path = pathFinder.FindPath(fighter.Position.Cell, cell, false, remainingMp);
                if (path == null || path.IsEmpty())
                    continue;

                if (path.EndCell != cell)
                    continue;

                candidates.Add(path);
            }

            if (candidates.Count == 0)
                return false;

            // Preferir celdas en direccion al enemigo mas cercano
            var enemies = GetEnemies(fighter).ToList();
            if (enemies.Count > 0)
            {
                var nearestEnemy = enemies.First();
                candidates = candidates
                    .OrderBy(p => Math.Abs(nearestEnemy.Position.Point.X - p.EndPathPosition.Point.X) + Math.Abs(nearestEnemy.Position.Point.Y - p.EndPathPosition.Point.Y))
                    .ThenBy(p => Math.Abs(p.MPCost))
                    .ToList();

                var best = candidates.First();
                fighter.StartMove(best);
            }
            else
            {
                // Sin enemigos, mover a una celda aleatoria
                var rng = new Random();
                var chosen = candidates[rng.Next(candidates.Count)];
                fighter.StartMove(chosen);
            }

            await PauseAfterActionAsync(fighter, moved: true);
            return true;
        }

        private static IEnumerable<FightActor> GetEnemies(FightActor fighter)
        {
            return fighter.Fight.GetAllFighters(x => x != fighter && x.IsAlive && x.IsEnnemyWith(fighter) && x.IsVisibleFor(fighter))
                .OrderBy(x => x.Position.Point.DistanceToCell(fighter.Position.Point))
                .ThenBy(x => x.Stats.Health.Total);
        }

        private static IEnumerable<FightActor> GetAllies(FightActor fighter)
        {
            return fighter.Fight.GetAllFighters(x => x != fighter && x.IsAlive && x.IsFriendlyWith(fighter))
                .OrderByDescending(x => x.Level)
                .ThenBy(x => x.Position.Point.DistanceToCell(fighter.Position.Point));
        }

        private static bool CanUseSpell(FightActor fighter, Spell spell)
        {
            return spell != null &&
                   spell.Template != null &&
                   fighter.Stats.AP.Total >= spell.Template.ApCost &&
                   fighter.SpellHistory != null &&
                   fighter.SpellHistory.CanCastSpell(spell, fighter.Position.Cell);
        }

        private static bool IsValidMoveCell(FightActor fighter, short cell)
        {
            return cell >= 0 &&
                   cell < 560 &&
                   cell != fighter.Position.Cell &&
                   fighter.Map.CellsInfoProvider.IsCellWalkable(cell) &&
                   fighter.Fight.IsCellFree(cell);
        }

        private static bool CanCastFromCell(FightActor fighter, Spell spell, short fromCell, short targetCell)
        {
            if (spell == null || spell.Template == null)
                return false;

            if (fighter.Stats.AP.Total < spell.Template.ApCost)
                return false;

            bool targetCellFree = fighter.Fight.IsCellFree(targetCell);
            if ((spell.Template.NeedFreeCell && !targetCellFree) || (spell.Template.NeedTakenCell && targetCellFree))
                return false;

            if (spell.StatesForbidden.Any(x => fighter.HasState((SpellStatesEnum)x)))
                return false;

            if (spell.StatesRequired.Any(x => !fighter.HasState((SpellStatesEnum)x)))
                return false;

            var castZone = GetCastZone(fighter, spell.Template, fromCell);
            if (!castZone.Contains(targetCell))
                return false;

            return fighter.SpellHistory.CanCastSpell(spell, targetCell);
        }

        private static short[] GetCastZone(FightActor fighter, SpellTemplate spellTemplate, short fromCell)
        {
            long maxRange = spellTemplate.Range;
            if (spellTemplate.RangeCanBeBoosted)
            {
                maxRange += fighter.Stats[StatsEnum.Range].Total + fighter.Stats[StatsEnum.Range].Context;
                if (maxRange < spellTemplate.MinRange)
                    maxRange = spellTemplate.MinRange;
                maxRange = Math.Min(maxRange, 280);
            }

            IShape shape;
            if (spellTemplate.CastInDiagonal && spellTemplate.CastInLine)
            {
                shape = new Cross((byte)spellTemplate.MinRange, (byte)maxRange) { AllDirections = true };
            }
            else if (spellTemplate.CastInLine)
            {
                shape = new Cross((byte)spellTemplate.MinRange, (byte)maxRange);
            }
            else if (spellTemplate.CastInDiagonal)
            {
                shape = new Cross((byte)spellTemplate.MinRange, (byte)maxRange) { Diagonal = true };
            }
            else
            {
                shape = new Lozenge((byte)spellTemplate.MinRange, (byte)maxRange);
            }

            return shape.GetCells(fromCell, fighter.Map);
        }

        private static int GetSpellPriority(Spell spell)
        {
            if (spell == null || spell.Template == null)
                return 0;

            int score = spell.Template.ApCost * 10;
            if (IsAttackSpell(spell))
                score += 100;
            if (IsSummonSpell(spell))
                score += 40;
            if (IsBoostSpell(spell) || IsHealSpell(spell))
                score += 20;
            score += (int)Math.Min(spell.Template.Range, 10);
            return score;
        }

        private static bool IsAttackSpell(Spell spell)
        {
            return spell.Effects.Any(x =>
                x.Id == EffectsEnum.Effect_DamageAir ||
                x.Id == EffectsEnum.Effect_DamageNeutral ||
                x.Id == EffectsEnum.Effect_DamageNeutralPerAP ||
                x.Id == EffectsEnum.Effect_DamageNeutralPerMP ||
                x.Id == EffectsEnum.Effect_StealHPAir ||
                x.Id == EffectsEnum.Effect_DamageAirPerAP ||
                x.Id == EffectsEnum.Effect_DamageAirPerMP ||
                x.Id == EffectsEnum.Effect_DamageEarth ||
                x.Id == EffectsEnum.Effect_StealHPEarth ||
                x.Id == EffectsEnum.Effect_DamageEarthPerAP ||
                x.Id == EffectsEnum.Effect_DamageEarthPerMP ||
                x.Id == EffectsEnum.Effect_DamageFire ||
                x.Id == EffectsEnum.Effect_StealHPFire ||
                x.Id == EffectsEnum.Effect_DamageFirePerAP ||
                x.Id == EffectsEnum.Effect_DamageFirePerMP ||
                x.Id == EffectsEnum.Effect_DamageWater ||
                x.Id == EffectsEnum.Effect_StealHPWater ||
                x.Id == EffectsEnum.Effect_DamageWaterPerAP ||
                x.Id == EffectsEnum.Effect_DamageWaterPerMP ||
                x.Id == EffectsEnum.Effect_StealHPNeutral);
        }

        private static bool IsBoostSpell(Spell spell)
        {
            return spell.Effects.Any(x =>
                x.Id == EffectsEnum.Effect_AddAgility ||
                x.Id == EffectsEnum.Effect_AddAirDamageBonus ||
                x.Id == EffectsEnum.Effect_AddChance ||
                x.Id == EffectsEnum.Effect_AddWaterDamageBonus ||
                x.Id == EffectsEnum.Effect_AddDamageBonus ||
                x.Id == EffectsEnum.Effect_AddVitality ||
                x.Id == EffectsEnum.Effect_AddRange ||
                x.Id == EffectsEnum.Effect_AddRange_136 ||
                x.Id == EffectsEnum.Effect_AddDamageBonusPercent ||
                x.Id == EffectsEnum.Effect_AddFireDamageBonus ||
                x.Id == EffectsEnum.Effect_AddEarthDamageBonus ||
                x.Id == EffectsEnum.Effect_AddNeutralDamageBonus ||
                x.Id == EffectsEnum.Effect_AddAP_111 ||
                x.Id == EffectsEnum.Effect_RegainAP ||
                x.Id == EffectsEnum.Effect_AddMP ||
                x.Id == EffectsEnum.Effect_AddMP_128 ||
                x.Id == EffectsEnum.Effect_AddLock ||
                x.Id == EffectsEnum.Effect_AddDodge ||
                x.Id == EffectsEnum.Effect_IncreaseAPAvoid ||
                x.Id == EffectsEnum.Effect_IncreaseMPAvoid ||
                x.Id == EffectsEnum.Effect_AddArmorDamageReduction ||
                x.Id == EffectsEnum.Effect_AddGlobalDamageReduction_105 ||
                x.Id == EffectsEnum.Effect_AddDamageReflection ||
                x.Id == EffectsEnum.Effect_AddGlobalDamageReduction ||
                x.Id == EffectsEnum.Effect_AddEarthResistPercent ||
                x.Id == EffectsEnum.Effect_AddEarthElementReduction ||
                x.Id == EffectsEnum.Effect_AddWaterResistPercent ||
                x.Id == EffectsEnum.Effect_AddWaterElementReduction ||
                x.Id == EffectsEnum.Effect_AddFireResistPercent ||
                x.Id == EffectsEnum.Effect_AddFireElementReduction ||
                x.Id == EffectsEnum.Effect_AddAirResistPercent ||
                x.Id == EffectsEnum.Effect_AddAirElementReduction ||
                x.Id == EffectsEnum.Effect_AddNeutralResistPercent ||
                x.Id == EffectsEnum.Effect_AddNeutralElementReduction);
        }

        private static bool IsHealSpell(Spell spell)
        {
            return spell.Effects.Any(x =>
                x.Id == EffectsEnum.Effect_HealHP_108 ||
                x.Id == EffectsEnum.Effect_HealHP_143 ||
                x.Id == EffectsEnum.Effect_HealHP_81);
        }

        private static bool IsSummonSpell(Spell spell)
        {
            return spell.Effects.Any(x =>
                x.Id == EffectsEnum.Effect_Summon ||
                x.Id == EffectsEnum.Effect_SummonBomb ||
                x.Id == EffectsEnum.Effect_SummonOsa ||
                x.Id == EffectsEnum.Effect_SummonSlave);
        }
    }
}
