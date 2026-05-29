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
            if (monster == null || fighter == null || fighter.Fight == null || fighter.Map == null || !fighter.IsAlive)
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
                    continue;
                }
            }
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

            var pathFinder = new Pathfinder(fighter.Fight.Map.CellsInfoProvider);
            Path bestPath = null;
            int currentDistance = (int)enemies.Min(x => x.Position.Point.DistanceToCell(fighter.Position.Point));
            int bestDistance = currentDistance;

            for (short cell = 0; cell < 560; cell++)
            {
                if (!IsValidMoveCell(fighter, cell))
                    continue;

                var path = pathFinder.FindPath(fighter.Position.Cell, cell, false, remainingMp);
                if (path == null || path.IsEmpty())
                    continue;

                if (path.EndCell != cell)
                    continue;

                var point = new global::Sunshine.WorldServer.Game.Maps.MapPoint(cell);
                int distance = (int)enemies.Min(x => x.Position.Point.DistanceToCell(point));

                if (bestPath == null || distance < bestDistance || (distance == bestDistance && path.MPCost < bestPath.MPCost))
                {
                    bestDistance = distance;
                    bestPath = path;
                }
            }

            if (bestPath == null || bestPath.IsEmpty() || bestDistance >= currentDistance)
                return false;

            fighter.StartMove(bestPath);
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
                maxRange += fighter.Stats[StatsEnum.Range].TotalMax;
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
