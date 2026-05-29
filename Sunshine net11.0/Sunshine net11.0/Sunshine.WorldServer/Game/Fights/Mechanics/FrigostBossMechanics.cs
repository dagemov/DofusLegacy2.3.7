using Sunshine.Protocol.Enums;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Maps.Shapes;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Fights.Mechanics
{
    /// <summary>
    /// Official Frigost 1/2 boss invulnerability unlock rules for the 2.3.7 branch.
    /// The database spells still provide the visual opening states; this class restores
    /// the real vulnerability triggers instead of bypassing Invulnerable on dungeon bosses.
    /// </summary>
    public static class FrigostBossMechanics
    {
        private const int RoyalmouthId = 2854;
        private const int MansotRoyalId = 2848;
        private const int BenLeRipateId = 2877;
        private const int HamrackId = 2955;
        private const int ObsidiantreId = 2924;
        private const int YokaiGivrefouxId = 2888;
        private const int YomiGivrefouxId = 2891;
        private const int TenguGivrefouxId = 2967;
        private const int KorriandreId = 2968;
        private const int FujiGivrefouxId = 2970;
        private const int KolossoId = 2986;
        private const int GlourselesteId = 2864;

        private static readonly HashSet<int> InvulnerableBossIds = new HashSet<int>
        {
            RoyalmouthId,
            MansotRoyalId,
            BenLeRipateId,
            ObsidiantreId,
            TenguGivrefouxId,
            KolossoId,
            GlourselesteId
        };

        private static readonly Dictionary<int, List<VulnerabilityWindow>> VulnerabilitiesByFight = new Dictionary<int, List<VulnerabilityWindow>>();
        private static readonly Dictionary<int, List<TimedStateWindow>> TimedStatesByFight = new Dictionary<int, List<TimedStateWindow>>();

        private sealed class VulnerabilityWindow
        {
            public int BossFighterId;
            public int? ExpireOnActorTurnStartId;
            public int? ExpireOnActorTurnEndId;
            public int BossTurnsRemaining;
            public string Reason;
        }

        private sealed class TimedStateWindow
        {
            public int ActorId;
            public SpellStatesEnum State;
            public int ActorTurnsRemaining;
        }

        public static int ResolveForcedSummonMonsterId(FightActor caster, int requestedMonsterId)
        {
            if (caster != null && IsFrigostPvMFight(caster.Fight) && GetMonsterId(caster) == BenLeRipateId)
                return HamrackId;

            return requestedMonsterId;
        }

        public static bool TryApplyHamrackMpLossBoost(FightActor target, FightActor caster, Spell spell, Effect sourceEffect)
        {
            if (target == null || !target.IsAlive || !IsFrigostPvMFight(target.Fight) || GetMonsterId(target) != HamrackId)
                return false;

            ApplyHamrackMpBoost(target, caster ?? target, spell, sourceEffect);
            return true;
        }

        public static void OnFightStarted(Fight fight)
        {
            if (!IsFrigostPvMFight(fight))
                return;

            foreach (var actor in fight.GetAllFighters().Where(x => x != null && x.IsAlive).ToArray())
            {
                int monsterId = GetMonsterId(actor);

                if (InvulnerableBossIds.Contains(monsterId))
                    EnsureInvulnerable(actor);

                if (monsterId == TenguGivrefouxId || monsterId == YokaiGivrefouxId || monsterId == YomiGivrefouxId)
                    EnsureState(actor, SpellStatesEnum.Heavy);

                if (monsterId == KorriandreId || monsterId == FujiGivrefouxId)
                    RemoveInvulnerable(actor);
            }
        }

        public static void Cleanup(Fight fight)
        {
            if (fight == null)
                return;

            VulnerabilitiesByFight.Remove(fight.Id);
            TimedStatesByFight.Remove(fight.Id);
        }

        public static bool TryPlayBenHamrackMechanic(FightActor actor)
        {
            if (actor == null || !actor.IsAlive || !IsFrigostPvMFight(actor.Fight))
                return false;

            switch (GetMonsterId(actor))
            {
                case BenLeRipateId:
                    return TrySummonHamrack(actor);

                case HamrackId:
                    return TryMoveAndExplodeHamrackOnBen(actor);

                default:
                    return false;
            }
        }

        public static void OnTurnStarted(FightActor actor)
        {
            if (!IsFrigostPvMFight(actor?.Fight))
                return;

            ExpireActorTurnStartWindows(actor);
            HandleTenguOpeningTurn(actor);
            DecrementActorTimedStates(actor);
        }

        public static void OnTurnEnded(FightActor actor)
        {
            if (!IsFrigostPvMFight(actor?.Fight))
                return;

            ExpireActorTurnEndWindows(actor);
            DecrementBossTurnWindows(actor);
        }

        /// <summary>
        /// Called before the generic Invulnerable damage prevention. Return true when
        /// the triggering hit must be consumed without applying its damage.
        /// </summary>
        public static bool BeforeDamage(FightActor target, Damage damage, bool isPoisoned)
        {
            if (target == null || damage == null || !IsFrigostPvMFight(target.Fight))
                return false;

            var source = damage.Source;
            int targetMonsterId = GetMonsterId(target);

            HandleYomiWeaponTrigger(target, source, damage);

            if (targetMonsterId == BenLeRipateId && IsMonster(source, HamrackId) && AreAdjacent(source, target))
            {
                MakeVulnerableForBossTurns(target, 5, "Hamrack");
                return true;
            }

            if (targetMonsterId == KolossoId && source is ISummoned && AreAdjacent(source, target))
            {
                if (target.HasState(SpellStatesEnum.Invulnerable))
                {
                    MakeVulnerableUntilActorTurnStart(target, source, "Invocation");
                    return true;
                }

                return false;
            }

            if (targetMonsterId == GlourselesteId && IsMelee(source, target))
            {
                if (target.HasState(SpellStatesEnum.Invulnerable))
                {
                    MakeVulnerableUntilActorTurnEnd(target, source, "MeleeHit");
                    return true;
                }
            }

            return false;
        }

        public static void OnActorHealed(FightActor healer, FightActor healedTarget)
        {
            if (healedTarget == null || !IsFrigostPvMFight(healedTarget.Fight))
                return;

            var mansot = FindMonster(healedTarget.Fight, MansotRoyalId);
            if (mansot == null || !mansot.IsAlive || !AreAdjacent(healedTarget, mansot))
                return;

            MakeVulnerableForBossTurns(mansot, 6, "HealAtMelee");
        }

        public static void OnForcedMove(FightActor source, FightActor moved, short startCell, short endCell, bool isPushLike, bool didPushbackDamage, FightActor collisionTarget = null)
        {
            if (moved == null || !IsFrigostPvMFight(moved.Fight))
                return;

            bool actuallyMoved = startCell != endCell;
            if (!actuallyMoved && !(didPushbackDamage && collisionTarget != null))
                return;

            int movedMonsterId = GetMonsterId(moved);

            if (actuallyMoved && movedMonsterId == RoyalmouthId && isPushLike)
                MakeVulnerableUntilActorTurnStart(moved, ResolveTriggeringActor(source, moved), "Pushed");

            if (actuallyMoved && movedMonsterId == YokaiGivrefouxId)
            {
                var tengu = FindMonster(moved.Fight, TenguGivrefouxId);
                if (tengu != null && IsDiagonal(moved, tengu))
                    AddTimedState(tengu, SpellStatesEnum.Blurring, 2);
            }

            if (didPushbackDamage && collisionTarget != null && GetMonsterId(collisionTarget) == ObsidiantreId && moved is CharacterFighter)
                MakeVulnerableForBossTurns(collisionTarget, 3, "PushbackCollision");
        }

        public static void OnActorDied(FightActor dead, FightActor killer)
        {
            if (dead == null || !IsFrigostPvMFight(dead.Fight))
                return;

            if (GetMonsterId(dead) == HamrackId)
            {
                var ben = FindMonster(dead.Fight, BenLeRipateId);
                if (ben != null && AreAdjacent(dead, ben))
                    MakeVulnerableForBossTurns(ben, 5, "HamrackDeath");
            }
        }

        private static void HandleYomiWeaponTrigger(FightActor target, FightActor source, Damage damage)
        {
            if (GetMonsterId(target) != YomiGivrefouxId || damage?.Spell == null || damage.Spell.Id != 0)
                return;

            var tengu = FindMonster(target.Fight, TenguGivrefouxId);
            if (tengu != null && IsAligned(target, tengu))
                AddTimedState(tengu, SpellStatesEnum.Fulguration, 2);
        }

        private static void HandleTenguOpeningTurn(FightActor actor)
        {
            if (GetMonsterId(actor) != TenguGivrefouxId)
                return;

            if (!actor.HasState(SpellStatesEnum.Fulguration) || !actor.HasState(SpellStatesEnum.Blurring))
                return;

            RemoveState(actor, SpellStatesEnum.Fulguration);
            RemoveState(actor, SpellStatesEnum.Blurring);
            RemoveTimedState(actor, SpellStatesEnum.Fulguration);
            RemoveTimedState(actor, SpellStatesEnum.Blurring);
            MakeVulnerableForBossTurns(actor, 4, "YomiYokai");
        }

        private static void MakeVulnerableForBossTurns(FightActor boss, int bossTurns, string reason)
        {
            if (boss == null || bossTurns <= 0)
                return;

            ApplyVulnerableState(boss);
            ReplaceWindow(boss, new VulnerabilityWindow
            {
                BossFighterId = boss.Id,
                BossTurnsRemaining = bossTurns,
                Reason = reason
            });
        }

        private static void MakeVulnerableUntilActorTurnStart(FightActor boss, FightActor actor, string reason)
        {
            if (boss == null || actor == null)
                return;

            ApplyVulnerableState(boss);
            ReplaceWindow(boss, new VulnerabilityWindow
            {
                BossFighterId = boss.Id,
                ExpireOnActorTurnStartId = actor.Id,
                Reason = reason
            });
        }

        private static void MakeVulnerableUntilActorTurnEnd(FightActor boss, FightActor actor, string reason)
        {
            if (boss == null || actor == null)
                return;

            ApplyVulnerableState(boss);
            ReplaceWindow(boss, new VulnerabilityWindow
            {
                BossFighterId = boss.Id,
                ExpireOnActorTurnEndId = actor.Id,
                Reason = reason
            });
        }

        private static void ReplaceWindow(FightActor boss, VulnerabilityWindow window)
        {
            var windows = GetVulnerabilityWindows(boss.Fight);
            windows.RemoveAll(x => x.BossFighterId == boss.Id);
            windows.Add(window);
        }

        private static void ExpireActorTurnStartWindows(FightActor actor)
        {
            var fight = actor.Fight;
            var windows = GetVulnerabilityWindows(fight);
            var expired = windows.Where(x => x.ExpireOnActorTurnStartId == actor.Id).ToArray();
            foreach (var window in expired)
                ExpireWindow(fight, windows, window);
        }

        private static void ExpireActorTurnEndWindows(FightActor actor)
        {
            var fight = actor.Fight;
            var windows = GetVulnerabilityWindows(fight);
            var expired = windows.Where(x => x.ExpireOnActorTurnEndId == actor.Id).ToArray();
            foreach (var window in expired)
                ExpireWindow(fight, windows, window);
        }

        private static void DecrementBossTurnWindows(FightActor actor)
        {
            var fight = actor.Fight;
            var windows = GetVulnerabilityWindows(fight);
            var bossWindows = windows.Where(x => x.BossFighterId == actor.Id && x.BossTurnsRemaining > 0).ToArray();
            foreach (var window in bossWindows)
            {
                window.BossTurnsRemaining--;
                if (window.BossTurnsRemaining <= 0)
                    ExpireWindow(fight, windows, window);
            }
        }

        private static void ExpireWindow(Fight fight, List<VulnerabilityWindow> windows, VulnerabilityWindow window)
        {
            var boss = fight?.GetAllFighters().FirstOrDefault(x => x != null && x.Id == window.BossFighterId && x.IsAlive);
            if (boss != null && InvulnerableBossIds.Contains(GetMonsterId(boss)))
                RestoreInvulnerableState(boss);

            windows.Remove(window);
        }

        private static void AddTimedState(FightActor actor, SpellStatesEnum state, int actorTurns)
        {
            if (actor == null || actorTurns <= 0)
                return;

            EnsureState(actor, state);
            var states = GetTimedStates(actor.Fight);
            states.RemoveAll(x => x.ActorId == actor.Id && x.State == state);
            states.Add(new TimedStateWindow
            {
                ActorId = actor.Id,
                State = state,
                ActorTurnsRemaining = actorTurns
            });
        }

        private static void DecrementActorTimedStates(FightActor actor)
        {
            var states = GetTimedStates(actor.Fight);
            var actorStates = states.Where(x => x.ActorId == actor.Id).ToArray();
            foreach (var state in actorStates)
            {
                state.ActorTurnsRemaining--;
                if (state.ActorTurnsRemaining <= 0)
                {
                    RemoveState(actor, state.State);
                    states.Remove(state);
                }
            }
        }

        private static void RemoveTimedState(FightActor actor, SpellStatesEnum state)
        {
            if (actor?.Fight == null)
                return;

            GetTimedStates(actor.Fight).RemoveAll(x => x.ActorId == actor.Id && x.State == state);
        }

        private static void EnsureInvulnerable(FightActor actor)
        {
            EnsureState(actor, SpellStatesEnum.Invulnerable);
        }

        private static void RemoveInvulnerable(FightActor actor)
        {
            RemoveState(actor, SpellStatesEnum.Invulnerable);
        }

        private static void EnsureState(FightActor actor, SpellStatesEnum state)
        {
            if (actor != null && !actor.HasState(state))
                actor.AddState(state);
        }

        private static void RemoveState(FightActor actor, SpellStatesEnum state)
        {
            if (actor == null)
                return;

            foreach (var buff in actor.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == state).ToArray())
                actor.RemoveBuff(buff);

            if (actor.HasState(state))
                actor.RemoveState(state);
        }

        private static List<VulnerabilityWindow> GetVulnerabilityWindows(Fight fight)
        {
            if (fight == null)
                return new List<VulnerabilityWindow>();

            if (!VulnerabilitiesByFight.TryGetValue(fight.Id, out var windows))
            {
                windows = new List<VulnerabilityWindow>();
                VulnerabilitiesByFight[fight.Id] = windows;
            }

            return windows;
        }

        private static List<TimedStateWindow> GetTimedStates(Fight fight)
        {
            if (fight == null)
                return new List<TimedStateWindow>();

            if (!TimedStatesByFight.TryGetValue(fight.Id, out var states))
            {
                states = new List<TimedStateWindow>();
                TimedStatesByFight[fight.Id] = states;
            }

            return states;
        }

        private static bool TrySummonHamrack(FightActor ben)
        {
            if (ben == null || ben.Fight == null || ben.Position == null || ben.Map == null)
                return false;

            var existingHamrack = ben.Fight.GetAllFighters()
                .FirstOrDefault(x => x != null && x.IsAlive && x.IsFriendlyWith(ben) && GetMonsterId(x) == HamrackId);

            if (existingHamrack != null)
                return false;

            short cell = FindBestHamrackSummonCell(ben);
            if (cell < 0)
                return false;

            if (!MonsterManager.Instance.Monsters.TryGetValue(HamrackId, out var template) || template == null)
                return false;

            if (!MonsterManager.Instance.MonstersGrade.TryGetValue(HamrackId, out var grades) || grades == null || grades.Count == 0)
                return false;

            int gradeIndex = 0;
            if (ben is MonsterFighter benMonster && benMonster.Monster?.Grade != null)
                gradeIndex = Math.Max(0, Math.Min(grades.Count - 1, benMonster.Monster.Grade.GradeId - 1));

            var monster = new Monster(template, grades[gradeIndex]);
            var hamrack = new SummonedMonster(monster, ben, new ObjectPosition(ben.Fight.Map, cell, ben.Position.Direction));

            ben.AddSummon(hamrack);

            int indexSummoner = ben.Fight.TimeLine.Fighters.IndexOf(ben);
            if (indexSummoner >= 0)
                ben.Fight.TimeLine.Fighters.Insert(indexSummoner + 1, hamrack);
            else
                ben.Fight.TimeLine.AddFighter(hamrack);

            if (ben.IsAttacker())
                ben.Fight.Team.AddAttacker(ben, hamrack);
            else
                ben.Fight.Team.AddDefender(ben, hamrack);

            ContextHandler.SendGameFightUpdateTeamMessage(ben.Fight.Clients, ben.Team);
            ContextHandler.SendGameFightTurnListMessage(ben.Fight.Clients, ben.Fight);
            ActionsHandler.SendGameActionFightSummonMessage(ben.Fight.Clients, hamrack);
            ben.Fight.TriggerMarks(hamrack.Position.Cell, hamrack, TriggerTypeEnum.MOVE);

            return true;
        }

        private static short FindBestHamrackSummonCell(FightActor ben)
        {
            var adjacent = new Lozenge(1, 1).GetCells(ben.Position.Cell, ben.Map)
                .Where(x => ben.Fight.IsCellFree(x))
                .OrderBy(x => x)
                .ToArray();

            if (adjacent.Length > 0)
                return adjacent[0];

            var widerZone = new Lozenge(1, 6).GetCells(ben.Position.Cell, ben.Map)
                .Where(x => ben.Fight.IsCellFree(x))
                .OrderBy(x => ben.Position.Point.DistanceToCell(MapPoint.GetPoint(x)))
                .ThenBy(x => x)
                .ToArray();

            return widerZone.Length > 0 ? widerZone[0] : (short)-1;
        }

        private static bool TryMoveAndExplodeHamrackOnBen(FightActor hamrack)
        {
            if (hamrack == null || hamrack.Fight == null || hamrack.Position == null || hamrack.Map == null)
                return false;

            var ben = FindMonster(hamrack.Fight, BenLeRipateId);
            if (ben == null || ben.Position == null || !ben.IsAlive)
                return false;

            bool moved = false;
            if (!AreAdjacent(hamrack, ben))
                moved = TryMoveAdjacentTo(hamrack, ben);

            if (!hamrack.IsAlive || hamrack.Fight == null || ben.Fight == null)
                return true;

            if (!AreAdjacent(hamrack, ben))
                return moved;

            ExplodeHamrackOnBen(hamrack, ben);
            return true;
        }

        private static bool TryMoveAdjacentTo(FightActor actor, FightActor target)
        {
            if (actor == null || target == null || actor.Fight == null || actor.Map == null || actor.Position == null || target.Position == null)
                return false;

            int remainingMp = actor.Stats.MP.Total;
            if (remainingMp <= 0)
                return false;

            var candidateCells = new Lozenge(1, 1).GetCells(target.Position.Cell, target.Map)
                .Where(x => actor.Fight.IsCellFree(x))
                .ToArray();

            if (candidateCells.Length == 0)
                return false;

            var pathFinder = new Pathfinder(actor.Fight.Map.CellsInfoProvider);
            Path bestPath = null;
            int bestCost = int.MaxValue;

            foreach (short cell in candidateCells)
            {
                var path = pathFinder.FindPath(actor.Position.Cell, cell, false, remainingMp);
                if (path == null || path.IsEmpty() || path.EndCell != cell)
                    continue;

                if (path.MPCost < bestCost || (path.MPCost == bestCost && cell < (bestPath != null ? bestPath.EndCell : short.MaxValue)))
                {
                    bestPath = path;
                    bestCost = path.MPCost;
                }
            }

            if (bestPath == null || bestPath.IsEmpty())
                return false;

            actor.StartMove(bestPath);
            return true;
        }

        private static void ExplodeHamrackOnBen(FightActor hamrack, FightActor ben)
        {
            if (hamrack == null || ben == null || !hamrack.IsAlive || !ben.IsAlive)
                return;

            ApplyVulnerableState(ben);
            ReplaceWindow(ben, new VulnerabilityWindow
            {
                BossFighterId = ben.Id,
                BossTurnsRemaining = 5,
                Reason = "HamrackExplosion"
            });

            hamrack.InflictDamage(Math.Max(1, hamrack.Stats.Health.Total + hamrack.Stats[StatsEnum.Shield].TotalMax), hamrack);
        }

        private static void ApplyVulnerableState(FightActor boss)
        {
            RemoveInvulnerable(boss);
            EnsureState(boss, SpellStatesEnum.Vulnerable);
        }

        private static void RestoreInvulnerableState(FightActor boss)
        {
            RemoveState(boss, SpellStatesEnum.Vulnerable);
            EnsureInvulnerable(boss);
        }


        private static void ApplyHamrackMpBoost(FightActor hamrack, FightActor caster, Spell spell, Effect sourceEffect)
        {
            const short boostAmount = 3;
            const short duration = 1;

            if (hamrack == null)
                return;

            var boostEffect = sourceEffect != null ? sourceEffect.Clone() : new Effect();
            boostEffect.Id = EffectsEnum.Effect_AddMP_128;
            boostEffect.DiceNum = (uint)boostAmount;
            boostEffect.DiceFace = (uint)boostAmount;
            boostEffect.Value = boostAmount;
            boostEffect.Delay = 0;
            boostEffect.Duration = duration;
            boostEffect.Target = SpellTargetType.ONLY_SELF;
            boostEffect.ZoneShape = SpellShapeEnum.P;
            boostEffect.ZoneMinSize = 0;
            boostEffect.ZoneSize = 0;

            hamrack.AddBuff(new StatsBuff(
                caster ?? hamrack,
                hamrack,
                EnsureBuffSpell(spell),
                boostEffect,
                duration,
                true,
                StatsEnum.MP,
                boostAmount,
                (short)EffectsEnum.Effect_AddMP_128));
        }

        private static Spell EnsureBuffSpell(Spell spell)
        {
            if (spell != null && spell.Template != null)
                return spell;

            return new Spell(0, 1)
            {
                Template = new global::Sunshine.MySql.Database.World.Spells.SpellTemplate
                {
                    SpellId = 0,
                    MaxStack = 0
                },
                Effects = new List<Effect>(),
                CriticalEffects = new List<Effect>(),
                StatesForbidden = new short[0],
                StatesRequired = new short[0]
            };
        }

        private static FightActor ResolveTriggeringActor(FightActor source, FightActor moved)
        {
            if (source != null)
                return source;

            return moved?.Fight?.FighterPlaying;
        }

        private static FightActor FindMonster(Fight fight, int monsterId)
        {
            return fight?.GetAllFighters().FirstOrDefault(x => x != null && x.IsAlive && GetMonsterId(x) == monsterId);
        }

        private static bool IsMonster(FightActor actor, int monsterId)
        {
            return actor != null && GetMonsterId(actor) == monsterId;
        }

        private static int GetMonsterId(FightActor actor)
        {
            if (actor is IMonster monster && monster.Monster?.Record != null)
                return monster.Monster.Record.Id;

            return 0;
        }

        private static bool AreAdjacent(FightActor a, FightActor b)
        {
            if (a?.Position?.Point == null || b?.Position?.Point == null)
                return false;

            return a.Position.Point.DistanceToCell(b.Position.Point) == 1;
        }

        private static bool IsMelee(FightActor source, FightActor target)
        {
            return AreAdjacent(source, target);
        }

        private static bool IsAligned(FightActor a, FightActor b)
        {
            if (a?.Position?.Point == null || b?.Position?.Point == null)
                return false;

            return a.Position.Point.IsInLine(b.Position.Point);
        }

        private static bool IsDiagonal(FightActor a, FightActor b)
        {
            if (a?.Position?.Point == null || b?.Position?.Point == null)
                return false;

            int ax = a.Position.Point.X;
            int ay = a.Position.Point.Y;
            int bx = b.Position.Point.X;
            int by = b.Position.Point.Y;
            return Math.Abs(ax - bx) == Math.Abs(ay - by) && ax != bx;
        }

        private static bool IsFrigostPvMFight(Fight fight)
        {
            return fight is FightPvM;
        }
    }
}
