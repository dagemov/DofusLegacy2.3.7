using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using System;
using System.Collections.Generic;
using Sunshine.Protocol.Utils.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Sunshine.WorldServer.Game.Fights.Results;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using Sunshine.WorldServer.Game.Fights.Telemetry;
using Sunshine.Protocol.Utils;
using Sunshine.MySql.Database.Managers;
using Sunshine.Logs;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Effects;

namespace Sunshine.WorldServer.Game.Fights
{
    public abstract class Fight
    {
        private List<MarkTrigger> m_triggers = new List<MarkTrigger>();

        private UniqueIdProvider m_triggerIdProvider = new UniqueIdProvider();

        protected ReversedUniqueIdProvider m_contextualIdProvider = new ReversedUniqueIdProvider(0);

        private readonly object _endFightSyncRoot = new object();
        private int _markTriggerEventCounter;
        private int _visualSequenceDepth;

        public int CurrentMarkTriggerEventId { get; private set; }

        public bool IsVisualSequenceActive => _visualSequenceDepth > 0;

        private bool _endFightScheduled;
        private bool _isEndingFight;

        private readonly object _turnTransitionLock = new object();
        private int _turnTransitionRound;
        private int _turnTransitionFighterId;
        private bool _turnEndStarted;
        private bool _turnAdvanceStarted;
        private ReadyChecker _readyChecker;

        public ReadyChecker Checker =>
            _readyChecker ?? (_readyChecker = new ReadyChecker(
                () => TryAdvanceTurn("ReadyCheckerSuccess"),
                laggers => TryAdvanceTurn("ReadyCheckerTimeout", laggers)));

        public abstract int Id { get; }

        public abstract FightTeam Team { get; }

        public abstract Map Map { get; }

        public abstract FightTypeEnum Type { get; }

        public abstract FightCommonInformations GetFightCommonInformations { get; }

        public abstract void AddFighter(FightActor fighter, bool isAttacker = false);

        public abstract int GetPlacementTimeLeft(FightActor fighter = null);

        public abstract void CheckAllStatus();

        public abstract System.Timers.Timer Timer { get; set; }

        public Character Leader { get; set; }

        public short MaxMemberCount { get; set; }

        public FightResults Results { get; set; }

        public DateTime FightStartedAtUtc { get; private set; }

        public bool CheckFightEnd()
        {
            if (State == FightStateEnum.Ended)
                return true;

            if (Clients.Count <= 0)
            {
                EndFight();
                return true;
            }

            if (Team.AreAllDead())
            {
                ScheduleEndFight(2666);
                return true;
            }

            return false;
        }

        private void ScheduleEndFight(int delayMs)
        {
            lock (_endFightSyncRoot)
            {
                if (_endFightScheduled || State == FightStateEnum.Ended)
                    return;

                _endFightScheduled = true;
            }

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs);
                }
                catch
                {
                }
                finally
                {
                    EndFight();
                }
            });
        }

        public void ShowBlades()
        {
            if (State != FightStateEnum.Placement || Team.Attackers.Count <= 0 || Team.Defenders.Count <= 0)
                return;

            ContextHandler.SendGameRolePlayShowChallengeMessage(Map.Clients, this);
        }

        public void HideBlades()
        {
            if (Map?.Clients == null)
                return;

            ContextHandler.GameRolePlayRemoveChallengeMessage(Map.Clients, this);
        }

        public bool IsStarted()
        {
            return State == FightStateEnum.Fighting;
        }

        public bool IsCellFree(short cell)
        {
            if (Map == null || Map.Cells == null || cell < 0 || cell >= Map.Cells.Length)
                return false;

            return Map.IsValidFightPlacementCell(cell) && this.GetOneFighter(cell) == null;
        }

        public FightStateEnum State { get; set; }

        public IEnumerable<int> GetAliveFightersIds()
        {
            return TimeLine.Fighters.Where(x => x.IsAlive).Select(x => x.Id);
        }

        public IEnumerable<int> GetDeadFightersIds()
        {
            return TimeLine.Leavers.Select(x => x.Id);
        }

        public List<WorldClient> Clients { get; set; }

        public TimeLine TimeLine { get; set; }

        public List<FightActor> Winners { get; set; }

        public List<FightActor> Losers { get; set; }

        public IEnumerable<FightActor> GetAllFighters()
        {
            var fighters = TimeLine != null && TimeLine.Fighters != null ? TimeLine.Fighters : new List<FightActor>();
            var leavers = TimeLine != null && TimeLine.Leavers != null ? TimeLine.Leavers : new List<FightActor>();
            return fighters.Concat(leavers).Where(x => x != null);
        }

        public IEnumerable<FightActor> GetAllFighters(Predicate<FightActor> predicate)
        {
            return from entry in GetAllFighters()
                   where predicate(entry)
                   select entry;
        }

        public IEnumerable<T> GetAllFighters<T>(System.Predicate<T> predicate) where T : FightActor
        {
            return from entry in TimeLine.Fighters.OfType<T>()
                   where predicate(entry)
                   select entry;
        }

        public int GetNextContextualId()
        {
            return this.m_contextualIdProvider.Pop();
        }

        public void FreeContextualId(int id)
        {
            this.m_contextualIdProvider.Push(id);
        }

        public FightActor GetOneFighter(int id)
        {
            if (TimeLine == null || TimeLine.Fighters == null)
                return null;

            return TimeLine.Fighters.FirstOrDefault(x => x != null && x.Id == id);
        }

        public FightActor GetOneFighter(short cell)
        {
            if (TimeLine == null || TimeLine.Fighters == null)
                return null;

            return TimeLine.Fighters.FirstOrDefault(x => x != null && x.IsAlive && x.Position != null && x.Position.Cell == cell);
        }

        public bool ContainsCharacter(int characterId)
        {
            return GetAllFighters().OfType<CharacterFighter>().Any(x => x.Character != null && x.Character.Id == characterId);
        }

        public FightActor FighterPlaying { get; set; }

        public FightActor GetFighterPlaying()
        {
            if (FighterPlaying == null)
            {
                ContextHandler.SendGameFightNewRoundMessage(Clients, TimeLine.RoundNumber++);
                return TimeLine.Fighters.FirstOrDefault();
            }
            else
            {
                int indexFighter = TimeLine.Fighters.IndexOf(FighterPlaying);
                if (TimeLine.Fighters.Count > indexFighter + 1)
                    return TimeLine.Fighters[indexFighter + 1];
                else
                {
                    ContextHandler.SendGameFightNewRoundMessage(Clients, TimeLine.RoundNumber++);
                    return TimeLine.Fighters.FirstOrDefault();
                }
            }
        }

        public void StartFight()
        {
            FightStartedAtUtc = DateTime.UtcNow;
            CombatTelemetry.LogTurnEvent("FightStarted", this);
            State = FightStateEnum.Fighting;
            HideBlades();
            SortFighters();
            NormalizeFightersHealthAtFightStart();
            ContextHandler.SendGameEntitiesDispositionMessage(Clients, GetAllFighters());
            ContextHandler.SendGameFightStartMessage(Clients);
            ContextHandler.SendGameFightTurnListMessage(Clients, this);
            ContextHandler.SendGameFightShowFighterMessage(Clients, GetAllFighters().ToList());
            ApplyFightStartMonsterBossMechanics();
            FrigostBossMechanics.OnFightStarted(this);
            ContextHandler.SendGameFightSynchronizeMessage(Clients, GetAllFighters());
            FighterPlaying = GetFighterPlaying();
            if (FighterPlaying != null)
            {
                CombatTelemetry.LogTurnEvent("TurnOwner", this, FighterPlaying, detail: "source=StartFight");
                FighterPlaying.StartTurn();
            }
            else
                EndFight();
        }


        private void NormalizeFightersHealthAtFightStart()
        {
            foreach (var fighter in GetAllFighters().ToArray())
                fighter?.NormalizeFightHealth(!(fighter is CharacterFighter));
        }

        public int GetCompletedRounds()
        {
            return Math.Max(1, TimeLine?.RoundNumber ?? 1);
        }

        public int GetFightDurationMilliseconds()
        {
            if (FightStartedAtUtc == default(DateTime))
                return 0;

            var duration = DateTime.UtcNow - FightStartedAtUtc;
            if (duration.TotalMilliseconds < 0)
                return 0;

            return (int)duration.TotalMilliseconds;
        }

        public string GetFightDurationText()
        {
            var duration = TimeSpan.FromMilliseconds(GetFightDurationMilliseconds());
            if (duration.TotalSeconds < 1)
                return "0s";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}m {duration.Seconds}s";
            return $"{duration.Seconds}s";
        }

        private void NotifyFightEndSummary()
        {
            string message = $"Combat terminé : {GetCompletedRounds()} tour(s) - {GetFightDurationText()}.";
            foreach (var fighter in GetAllFighters().OfType<CharacterFighter>())
            {
                try
                {
                    fighter?.Character?.SendServerMessage(message);
                }
                catch
                {
                }
            }
        }

        private void ApplyFightStartMonsterBossMechanics()
        {
            if (!(this is Types.FightPvM))
                return;

            var monsters = GetAllFighters().OfType<MonsterFighter>().Where(x => x != null && x.Monster != null && x.Monster.Record != null).ToList();
            if (monsters.Count == 0)
                return;

            foreach (var monster in monsters)
            {
                int monsterId = monster.Monster.Record.Id;
                short selfCell = monster.Position != null ? monster.Position.Cell : (short)0;

                switch (monsterId)
                {
                    case 2854: // Royalmouth
                        TryForceCastOpeningSpell(monster, 2356, selfCell);
                        break;
                    case 2848: // Mansot Royal
                        TryForceCastOpeningSpell(monster, 2607, selfCell);
                        break;
                    case 2877: // Ben le Ripate
                        TryForceCastOpeningSpell(monster, 2615, selfCell);
                        break;
                    case 2955: // Hamrack
                        TryForceCastOpeningSpell(monster, 2617, selfCell);
                        break;
                    case 2924: // Obsidiantre
                        TryForceCastOpeningSpell(monster, 679, selfCell);
                        break;
                    case 2951: // Pougnette
                        TryForceCastOpeningSpell(monster, 2602, selfCell);
                        break;
                    case 2968: // Korriandre
                        TryForceCastOpeningSpell(monster, 2569, selfCell);
                        break;
                    case 2967: // Tengu Givrefoux
                        TryForceCastOpeningSpell(monster, 2677, selfCell);
                        break;
                    case 2986: // Kolosso
                        TryForceCastOpeningSpell(monster, 2523, selfCell);
                        break;
                    case 2864: // Glourséleste (best effort from SaveKrosmoz comment)
                        TryForceCastOpeningSpell(monster, 11168, selfCell);
                        break;
                }
            }

            var fuji = monsters.FirstOrDefault(x => x.Monster.Record.Id == 2970);
            if (fuji != null)
            {
                foreach (var ally in Team.Defenders.Where(x => x != null && x.IsAlive))
                {
                    short targetCell = ally.Position != null ? ally.Position.Cell : (short)0;
                    TryForceCastOpeningSpell(fuji, 2676, targetCell);
                }
            }
        }

        private void RemoveDungeonBossInvulnerabilities()
        {
            if (!(this is Types.FightPvM) || Map == null || !Map.IsDungeon())
                return;

            var bosses = GetAllFighters()
                .OfType<MonsterFighter>()
                .Where(x => x != null && x.IsDungeonBoss())
                .ToArray();

            foreach (var boss in bosses)
            {
                var invulnerableBuffs = boss.GetBuffs(x =>
                    x is Game.Fights.Buffs.Spells.StateBuff stateBuff &&
                    stateBuff.State == SpellStatesEnum.Invulnerable).ToArray();

                foreach (var buff in invulnerableBuffs)
                    boss.RemoveBuff(buff);

                if (boss.HasState(SpellStatesEnum.Invulnerable))
                    boss.RemoveState(SpellStatesEnum.Invulnerable);
            }
        }

        private void TryForceCastOpeningSpell(FightActor caster, int spellId, short cell)
        {
            try
            {
                if (caster == null || caster.Fight != this)
                    return;

                if (!SpellManager.Instance.Spells.TryGetValue(spellId, out var spellLevels) || spellLevels == null || spellLevels.Count == 0)
                    return;

                var spell = spellLevels[0];
                if (spell == null)
                    return;

                foreach (var effect in spell.Effects ?? new List<Effect>())
                    EffectDispatcher.Dispatch(caster, spell, effect, cell);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to apply opening monster spell {spellId} for fighter {(caster != null ? caster.Id : 0)}. {ex}");
            }
        }

        private void FinalizeFighterAfterFight(FightActor fighter)
        {
            if (fighter == null)
                return;

            var characterFighter = fighter as CharacterFighter;
            int remainingLife = characterFighter != null ? characterFighter.GetCurrentLife() : 0;
            bool wasDead = characterFighter != null && characterFighter.IsDead();

            try
            {
                fighter.ResetFightProperties();
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to reset fighter {fighter.Id} after end fight. {ex}");
            }

            if (characterFighter == null || characterFighter.Character == null || characterFighter.Character.Client == null)
                return;

            RestoreCharacterToRoleplay(characterFighter, remainingLife, wasDead);
        }

        private void RestoreCharacterToRoleplay(CharacterFighter characterFighter, int fightRemainingLife, bool wasDead)
        {
            var character = characterFighter?.Character;
            var client = character?.Client;

            if (character == null || client == null)
                return;

            bool moveToNextDungeonRoom = this is FightPvM &&
                                         Map != null &&
                                         Map.IsDungeon() &&
                                         Map.Dungeon != null &&
                                         Winners != null &&
                                         Winners.Contains(characterFighter) &&
                                         characterFighter.IsAttacker();

            character.Fighter = null;
            character.Fight = null;
            Clients.Remove(client);

            try
            {
                ContextHandler.SendGameContextDestroyMessage(client);
                ContextHandler.SendGameContextCreateMessage(client, GameContextEnum.ROLE_PLAY);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to switch character {character.Id} back to roleplay context. {ex}");
            }

            try
            {
                character.ApplyLifeAfterFight(fightRemainingLife, wasDead, true);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to apply remaining life for character {character.Id} after fight. {ex}");
            }

            try
            {
                if (moveToNextDungeonRoom)
                {
                    character.ClearFightReturnPosition();
                    character.Direction = Map.Dungeon.NextDirection;
                    character.Teleport(Map.Dungeon.NextMap, Map.Dungeon.NextCell);
                }
                else
                {
                    character.TeleportToFightReturnPosition();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to teleport character {character.Id} after fight. {ex}");
            }

            try
            {
                ContextRoleplayHandler.SendMapComplementaryInformationsDataMessage(client);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to send map complementary informations to character {character.Id} after fight. {ex}");
                try
                {
                    if (character.Map != null)
                        character.EnterMap(character.Map);
                }
                catch
                {
                }
            }

            try
            {
                if (character.Map != null)
                    client.Send(new MapFightCountMessage((short)character.Map.Fights.Count));

                ContextHandler.SendGameMapChangeOrientationMessage(client);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to finalize roleplay synchronization for character {character.Id}. {ex}");
            }

            try
            {
                character.StartRegen();
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: failed to restart regen for character {character.Id} after fight. {ex}");
            }
        }

        public void EndFight()
        {
            lock (_endFightSyncRoot)
            {
                _endFightScheduled = false;

                if (_isEndingFight || State == FightStateEnum.Ended)
                    return;

                _isEndingFight = true;
                State = FightStateEnum.Ended;
            }

            CombatTelemetry.LogTurnEvent("FightEnded", this, FighterPlaying);
            Checker.Cancel();

            try
            {
                if (Timer != null)
                {
                    Timer.Stop();
                    Timer.Dispose();
                    Timer = null;
                }

                DeterminsWinners();

                try
                {
                    Results.Apply();
                }
                catch
                {
                    if (this is FightPvPrism prismFight && prismFight.PrismRecord != null)
                        PrismManager.Instance.MarkDefended(prismFight.PrismRecord);
                }

                ContextHandler.SendGameFightEndMessage(Clients, this, Results.FightResultsListEntry);
                NotifyFightEndSummary();
                HideBlades();

                var fighters = this.GetAllFighters().Where(x => !(x is ISummoned)).ToList();

                FightManager.Instance.RemoveFight(this);

                for (int i = 0; i < fighters.Count; i++)
                    FinalizeFighterAfterFight(fighters[i]);

                RespawnMonsterGroupIfNeeded();
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fight {Id}: fatal error while ending fight. {ex}");
            }
            finally
            {
                FrigostBossMechanics.Cleanup(this);

                try
                {
                    this.Team.RemoveAllTeams();
                }
                catch
                {
                }
            }
        }

        private void RespawnMonsterGroupIfNeeded()
        {
            try
            {
                if (!(this is Types.FightPvM pvmFight))
                    return;

                if (Map != null && Map.IsDungeon())
                {
                    Map.EnsureDungeonMonsterGroup();
                    return;
                }

                var sourceGroup = pvmFight.SourceMonsterGroup;
                if (sourceGroup != null)
                {
                    var sourceMap = sourceGroup.Map ?? Map;
                    sourceMap?.ScheduleMonsterGroupRespawn(sourceGroup);
                }
            }
            catch
            {
            }
        }

        public void EnterFighter(WorldClient client, FightActor fighter)
        {
            client.Character.CaptureFightReturnPosition();
            client.Character.StopRegen();
            client.Character.Look.RemoveAuras();
            ContextHandler.SendGameContextDestroyMessage(client);
            ContextHandler.SendGameContextCreateMessage(client, GameContextEnum.FIGHT);
            ContextHandler.SendGameFightStartingMessage(client, Type);
            ContextHandler.SendGameFightJoinMessage(client, this);
            ContextHandler.SendGameFightPlacementPossiblePositionsMessage(client, this);
            ContextHandler.SendGameFightShowFighterPreparationMessage(Clients, GetAllFighters());
            ContextHandler.SendGameEntitiesDispositionMessage(Clients, GetAllFighters());
            ContextHandler.SendGameFightUpdateTeamMessage(Clients, Team);
            ContextHandler.SendGameFightTurnListMessage(Clients, this);
        }

        public void LeaveFighter(WorldClient client, FightActor fighter)
        {
            if (fighter is CharacterFighter characterFighter && this is FightPvPrism)
            {
                bool wasDead = fighter.IsDead();
                if (!wasDead)
                {
                    fighter.OnDead(fighter, fighter);
                    wasDead = true;
                }

                int remainingLife = fighter.GetCurrentLife();

                if (State == FightStateEnum.Ended || CheckFightEnd())
                {
                    HideBlades();
                    return;
                }

                fighter.ResetFightProperties();
                var prismCharacter = characterFighter.Character;
                Clients.Remove(client);
                ContextHandler.SendGameContextDestroyMessage(client);
                ContextHandler.SendGameContextCreateMessage(client, GameContextEnum.ROLE_PLAY);
                prismCharacter.ApplyLifeAfterFight(remainingLife, wasDead, true);
                prismCharacter.TeleportToFightReturnPosition();
                prismCharacter.StartRegen();
                prismCharacter.Fighter = null;
                prismCharacter.Fight = null;
                HideBlades();
                return;
            }

            if (Clients.Count > 1 && Type != FightTypeEnum.FIGHT_TYPE_AGRESSION
                && Type != FightTypeEnum.FIGHT_TYPE_CHALLENGE
                && Type != FightTypeEnum.FIGHT_TYPE_MXvM)
            {
                fighter.OnDead(fighter, fighter);
                int remainingLife = fighter.GetCurrentLife();
                bool wasDead = true;
                fighter.ResetFightProperties();
                var character = (fighter as CharacterFighter).Character;
                Clients.Remove(client);
                ContextHandler.SendGameContextDestroyMessage(client);
                ContextHandler.SendGameContextCreateMessage(client, GameContextEnum.ROLE_PLAY);
                character.ApplyLifeAfterFight(remainingLife, wasDead, true);
                character.TeleportToFightReturnPosition();
                character.StartRegen();
                character.Fighter = null;
                character.Fight = null;
                CheckFightEnd();
            }
            else
            {
                EndFight();
                HideBlades();
            }
        }

        public void StartSequence(SequenceTypeEnum sequenceType)
        {
            _visualSequenceDepth++;
            ActionsHandler.SendSequenceStartMessage(Clients, FighterPlaying, sequenceType);
        }

        public void EndSequence(SequenceTypeEnum sequenceType, ActionsEnum actionType)
        {
            ActionsHandler.SendSequenceEndMessage(Clients, FighterPlaying, sequenceType, actionType);

            if (_visualSequenceDepth > 0)
                _visualSequenceDepth--;
        }

        public void OnFightPointsVariation(ActionsEnum action, FightActor source, FightActor target, int delta)
        {
            ActionsHandler.SendGameActionFightPointsVariationMessage(Clients, action, source, target, (short)delta);
        }

        public void OnLifePointsChanged(int delta, FightActor source, FightActor target)
        {
            if (target == null || Clients == null || delta == 0)
                return;

            target.NormalizeFightHealth(false);
            ActionsHandler.SendGameActionFightLifePointsVariationMessage(Clients, source ?? target, target, delta);
        }

        public void OnShieldPointsChanged(int delta, int shliedLoss, FightActor source, FightActor target)
        {
            ActionsHandler.SendGameActionFightShieldPointsVariationMessage(Clients, source, target, (short)delta);
        }

        public void OnDamageReducted(int amount, FightActor source, FightActor target)
        {
            ActionsHandler.SendGameActionFightReduceDamagesMessage(Clients, source, target, amount);
        }

        public void OnDamageReflected(FightActor source, FightActor target, int reflected)
        {
            ActionsHandler.SendGameActionFightReflectDamagesMessage(Clients, source, target, reflected);
        }

        public void OnSpellCasted(FightActor caster, Spell spell, short targetCell, FightSpellCastCriticalEnum critical, bool silentCast)
        {
            Diagnostics.FightCombatLogger.LogSpellCast(this, caster, spell, targetCell);

            foreach (CharacterFighter entry in GetAllFighters(x => x is CharacterFighter))
            {
                if (spell != null && spell.Id == 0)
                {
                    var characterCaster = caster as CharacterFighter;
                    int weaponGenericId = characterCaster != null ? characterCaster.GetCloseCombatWeaponGenericId() : 0;
                    ActionsHandler.SendGameActionFightCloseCombatMessage(entry.Client, caster, targetCell, critical, !caster.IsVisibleFor(entry) || silentCast, weaponGenericId);
                    continue;
                }

                if (spell.Id != (int)SpellIdEnum.PoisonedTrap && spell.Id != (int)SpellIdEnum.Trap && spell.Id != (int)SpellIdEnum.TrapofSilence && spell.Id != (int)SpellIdEnum.RepellingTrap && spell.Id != (int)SpellIdEnum.ParalyzingTrap && spell.Id != (int)SpellIdEnum.TrickyTrap && spell.Id != (int)SpellIdEnum.LethalTrap && spell.Id != (int)SpellIdEnum.MassTrap)
                    ContextHandler.SendGameActionFightSpellCastMessage(entry.Client, ActionsEnum.ACTION_FIGHT_CAST_SPELL, caster, spell, targetCell, critical, !caster.IsVisibleFor(entry) || silentCast);
                else
                {
                    if (caster.IsFriendlyWith(entry))
                        ContextHandler.SendGameActionFightSpellCastMessage(entry.Client, ActionsEnum.ACTION_FIGHT_CAST_SPELL, caster, spell, targetCell, critical, !caster.IsVisibleFor(entry) || !silentCast);
                    else
                        ContextHandler.SendGameActionFightSpellCastMessage(entry.Client, ActionsEnum.ACTION_FIGHT_CAST_SPELL, caster, spell, caster.Position.Point.GetNearestCellInDirection(caster.Position.Point.OrientationTo(MapPoint.GetPoint(targetCell), true)).CellId, critical, !caster.IsVisibleFor(entry) || silentCast);
                }
            }
        }

        public int PopNextTriggerId()
        {
            return this.m_triggerIdProvider.Pop();
        }

        public void FreeTriggerId(int id)
        {
            this.m_triggerIdProvider.Push(id);
        }

        public bool ShouldTriggerOnMove(short cell)
        {
            return ShouldTriggerOnMove(cell, null);
        }

        public bool ShouldTriggerOnMove(short cell, FightActor actor)
        {
            return this.m_triggers.Any(entry => entry.TriggersOn(TriggerTypeEnum.MOVE) && entry.ContainsCell(cell) && (actor == null || entry.IsAffected(actor)));
        }

        public MarkTrigger[] GetTriggers(short cell)
        {
            return (
                from entry in this.m_triggers
                where entry.CenterCell == cell
                select entry).ToArray<MarkTrigger>();
        }

        public MarkTrigger[] GetTriggers()
        {
            return this.m_triggers.ToArray();
        }

        public void AddTrigger(MarkTrigger trigger)
        {
            this.m_triggers.Add(trigger);
            foreach (CharacterFighter current in GetAllFighters(x => x is CharacterFighter))
                ContextHandler.SendGameActionFightMarkCellsMessage(current.Client, trigger, current != null && trigger.DoesSeeTrigger(current));

            if (!trigger.TriggersOn(TriggerTypeEnum.CREATION))
                return;

            var fighters = GetAllFighters().Where(x => x != null && x.IsAlive && x.Position != null && trigger.ContainsCell(x.Position.Cell)).ToArray();
            foreach (var fighter in fighters)
                trigger.Trigger(fighter);
        }

        public void RemoveTrigger(MarkTrigger trigger)
        {
            if (!this.m_triggers.Remove(trigger))
                return;

            trigger.NotifyRemoved();
            ContextHandler.SendGameActionFightUnmarkCellsMessage(Clients, trigger);
        }

        public void RemoveTriggers(FightActor caster)
        {
            MarkTrigger[] array = this.m_triggers.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                MarkTrigger markTrigger = array[i];
                if (markTrigger.Caster == caster)
                    this.RemoveTrigger(markTrigger);
            }
        }

        public void TriggerMarks(short cell, FightActor target, TriggerTypeEnum triggerType, bool isRecalled = false)
        {
            if (!isRecalled)
                CurrentMarkTriggerEventId = ++_markTriggerEventCounter;

            MarkTrigger[] source = this.m_triggers.ToArray();

            var markTriggers = (from markTrigger in source
                                where markTrigger.TriggersOn(triggerType) && markTrigger.ContainsCell(cell)
                                select markTrigger).ToArray();

            if (markTriggers.Length == 0)
                return;

            short startingCell = cell;
            ObjectPosition firstPosition = new ObjectPosition(Map, startingCell, target.Position.Direction);
            int countPushed = 0;

            foreach (MarkTrigger current in markTriggers)
            {
                ContextHandler.SendGameActionFightTriggerGlyphTrapMessage(Clients, current, target, current.CastedSpell);
                if (current is Trap)
                    this.RemoveTrigger(current);

                current.Trigger(target, firstPosition, ++countPushed);
            }

            if (target is Game.Actors.Fighters.BombFighter bombTarget)
                Game.Fights.Bombs.BombManager.Instance.CheckWalls(this, bombTarget.Summoner);

            // A full disposition refresh right after a glyph/trap/wall trigger cuts the client-side
            // slide/throw animation and makes several forced moves look like teleports.
            // The effect handlers already send the correct movement packets, so keep marks lightweight.
            if (!isRecalled && target != null && target.IsAlive && target.Position != null && target.Position.Cell != startingCell)
                TriggerMarks(target.Position.Cell, target, TriggerTypeEnum.MOVE, true);
        }

        public void DecrementGlyphDuration(FightActor caster)
        {
            System.Collections.Generic.List<MarkTrigger> list = (
                from trigger in this.m_triggers
                where trigger.Caster == caster
                where trigger.DecrementDuration()
                select trigger).ToList<MarkTrigger>();
            if (list.Count != 0)
            {
                this.StartSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP);
                foreach (MarkTrigger current in list)
                    this.RemoveTrigger(current);
                this.EndSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP, ActionsEnum.ACTION_FIGHT_TRIGGER_GLYPH);
            }
        }

        private void SortFighters()
        {
            #region GameFightMinimalStatsPreparation
            int fighterCount = TimeLine.Fighters.Count;
            List<FightActor> redFighters = new List<FightActor>();
            List<FightActor> blueFighters = new List<FightActor>();
            TimeLine.Fighters = new List<FightActor>();

            if (Team.Attackers.TrueForAll(x => x.GetGameFightMinimalStatsPreparation().initiative == 0)
              && Team.Defenders.TrueForAll(x => x.GetGameFightMinimalStatsPreparation().initiative == 0))
            {
                redFighters = Team.Attackers.OrderByDescending(x => x.Level).ToList();
                blueFighters = Team.Defenders.OrderByDescending(x => x.Level).ToList();

                if (redFighters[0].Level > blueFighters[0].Level)
                {
                    for (int i = 0; i < fighterCount; i++)
                    {
                        if (i < redFighters.Count)
                            TimeLine.AddFighter(redFighters[i]);
                        if (i < blueFighters.Count)
                            TimeLine.AddFighter(blueFighters[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < fighterCount; i++)
                    {
                        if (i < blueFighters.Count)
                            TimeLine.AddFighter(blueFighters[i]);
                        if (i < redFighters.Count)
                            TimeLine.AddFighter(redFighters[i]);
                    }
                }
            }
            else
            {
                redFighters = Team.Attackers.OrderByDescending(x => x.GetGameFightMinimalStatsPreparation().initiative).ToList();
                blueFighters = Team.Defenders.OrderByDescending(x => x.GetGameFightMinimalStatsPreparation().initiative).ToList();

                if (redFighters[0].GetGameFightMinimalStatsPreparation().initiative
                  > blueFighters[0].GetGameFightMinimalStatsPreparation().initiative)
                {
                    for (int i = 0; i < fighterCount; i++)
                    {
                        if (i < redFighters.Count)
                            TimeLine.AddFighter(redFighters[i]);
                        if (i < blueFighters.Count)
                            TimeLine.AddFighter(blueFighters[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < fighterCount; i++)
                    {
                        if (i < blueFighters.Count)
                            TimeLine.AddFighter(blueFighters[i]);
                        if (i < redFighters.Count)
                            TimeLine.AddFighter(redFighters[i]);
                    }
                }
            }
            #endregion
        }

        public void UpdateDirection()
        {
            if (Team == null || Team.Attackers == null || Team.Defenders == null)
                return;

            var attackers = Team.Attackers.Where(x => x != null && x.Position != null).ToList();
            var defenders = Team.Defenders.Where(x => x != null && x.Position != null).ToList();

            if (attackers.Count <= 0 || defenders.Count <= 0)
                return;

            ObjectPosition firstPosition = attackers[0].Position;

            foreach (var monster in defenders)
            {
                foreach (var fighter in attackers)
                {
                    if (monster.Position.Point.DistanceToCell(fighter.Position.Point) <= monster.Position.Point.DistanceToCell(firstPosition.Point))
                    {
                        firstPosition = fighter.Position;
                        monster.Position.Direction = monster.Position.Point.OrientationTo(fighter.Position.Point);
                        fighter.Position.Direction = fighter.Position.Point.OrientationTo(monster.Position.Point);
                    }
                }
            }

            ContextHandler.SendGameEntitiesDispositionMessage(Clients, GetAllFighters());
        }

        private void DeterminsWinners()
        {
            if (Team.Defenders.TrueForAll(x => x.IsDead()))
            {
                Winners = Team.Attackers;
                Losers = Team.Defenders;
            }
            else
            {
                Winners = Team.Defenders;
                Losers = Team.Attackers;
            }
        }

        public bool TryBeginTurnEnd(FightActor fighter, string source, out bool hadActiveTimer)
        {
            hadActiveTimer = Timer != null;

            lock (_turnTransitionLock)
            {
                if (State != FightStateEnum.Fighting || FighterPlaying?.Id != fighter?.Id)
                    return false;

                var round = TimeLine?.RoundNumber ?? 0;
                if (_turnTransitionRound != round || _turnTransitionFighterId != fighter.Id)
                {
                    _turnTransitionRound = round;
                    _turnTransitionFighterId = fighter.Id;
                    _turnEndStarted = false;
                    _turnAdvanceStarted = false;
                }

                if (_turnEndStarted)
                    return false;

                _turnEndStarted = true;
            }

            if (Timer != null)
            {
                Timer.Stop();
                Timer.Dispose();
                Timer = null;
            }

            return true;
        }

        public bool TryAdvanceTurn(string source, CharacterFighter[] laggers = null)
        {
            var previousFighter = FighterPlaying;
            var round = TimeLine?.RoundNumber ?? 0;

            lock (_turnTransitionLock)
            {
                if (State == FightStateEnum.Ended)
                {
                    LogAdvanceIgnored(previousFighter, source, laggers, "fight-ended");
                    return false;
                }

                if (State != FightStateEnum.Fighting)
                {
                    LogAdvanceIgnored(previousFighter, source, laggers, "fight-not-started");
                    return false;
                }

                if (!_turnEndStarted)
                {
                    LogAdvanceIgnored(previousFighter, source, laggers, "turn-end-not-started");
                    return false;
                }

                if (_turnAdvanceStarted)
                {
                    LogAdvanceIgnored(previousFighter, source, laggers, "turn-advance-already-started");
                    return false;
                }

                _turnAdvanceStarted = true;
            }

            var nextFighter = GetFighterPlaying();
            CombatTelemetry.LogReadyCheckerEvent(
                "ReadyCheckerAdvanceTurn",
                this,
                previousFighter,
                nextActor: nextFighter,
                reason: source);

            if (laggers != null && laggers.Length > 0)
                NotifyReadyLaggers(laggers);

            AdvanceToNextTurn(previousFighter, nextFighter, source, round);
            return true;
        }

        private void LogAdvanceIgnored(FightActor previousFighter, string source, CharacterFighter[] laggers, string reason)
        {
            CombatTelemetry.LogReadyCheckerEvent(
                "ReadyCheckerIgnored",
                this,
                previousFighter,
                reason: $"{source}:{reason}",
                waiters: laggers);
        }

        private void NotifyReadyLaggers(CharacterFighter[] laggers)
        {
            if (laggers == null || laggers.Length == 0)
                return;

            foreach (var lagger in laggers)
            {
                try
                {
                    lagger?.Character?.SendServerMessage(
                        laggers.Length == 1
                            ? $"En attente du joueur {lagger.Name}..."
                            : $"En attente des joueurs {string.Join(", ", laggers.Select(x => x.Name))}...");
                }
                catch
                {
                }
            }
        }

        internal void AdvanceToNextTurn(FightActor endingFighter, FightActor nextFighter, string source, int round)
        {
            CombatTelemetry.LogTurnEvent("NextTurnRequested", this, endingFighter, detail: $"source={source}");

            FighterPlaying = nextFighter;
            if (nextFighter != null)
                CombatTelemetry.LogTurnEvent("TurnOwner", this, nextFighter, detail: $"source={source}");

            lock (_turnTransitionLock)
            {
                _turnTransitionRound = TimeLine?.RoundNumber ?? round;
                _turnTransitionFighterId = nextFighter?.Id ?? 0;
                _turnEndStarted = false;
                _turnAdvanceStarted = false;
            }

            if (endingFighter is SlaveFighter controlledSlave && controlledSlave.IsControlledBySummoner() && !controlledSlave.MustAutoUnspawnAfterTurn)
                controlledSlave.RestoreSummonerContext();

            if (endingFighter is SlaveFighter autoUnspawnSlave && autoUnspawnSlave.MustAutoUnspawnAfterTurn && autoUnspawnSlave.IsAlive)
            {
                autoUnspawnSlave.IsAutoUnspawning = true;
                autoUnspawnSlave.Die(endingFighter);

                if (State != FightStateEnum.Fighting || CheckFightEnd())
                    return;
            }
            else if (endingFighter is SummonedMonster summonedMonster && summonedMonster.IsAlive)
            {
                if (summonedMonster.Monster?.Record?.Id == SlaveFighter.RoublabotMonsterId)
                    summonedMonster.Die(endingFighter);
                else if (summonedMonster.DiesAtTurnEnd)
                    summonedMonster.Die(endingFighter);

                if (State != FightStateEnum.Fighting || CheckFightEnd())
                    return;
            }

            if (FighterPlaying != null)
                FighterPlaying.StartTurn();
        }

        public void StartAction(double interval, object parameter)
        {
            if (parameter is string timerName && timerName == "EndTurn")
                CombatTelemetry.LogTurnEvent("TurnTimerStarted", this, FighterPlaying, detail: $"intervalMs={interval}");

            if (Timer != null)
            {
                Timer.Stop();
                Timer.Dispose();
                Timer = null;
            }

            Timer = new System.Timers.Timer(interval);
            Timer.AutoReset = false;
            Timer.Elapsed += (sender, e) => ActionElapsed(parameter, e);
            Timer.Start();
        }

        private void ActionElapsed(object sender, ElapsedEventArgs e)
        {
            if (State == FightStateEnum.Ended)
                return;

            switch ((string)sender)
            {
                case "EndTurn":
                    if (State == FightStateEnum.Fighting && FighterPlaying != null)
                    {
                        CombatTelemetry.LogTurnEvent("TimerElapsed", this, FighterPlaying, detail: "timer=EndTurn");
                        FighterPlaying.EndTurn("Timer");
                    }
                    break;

                case "StartFight":
                    if (State == FightStateEnum.Ended)
                        return;

                    if (TimeLine.Fighters.Count <= 0)
                        EndFight();
                    else
                        StartFight();
                    break;

                case "TeleportPlayers":
                    if (State != FightStateEnum.Placement)
                        return;

                    if (this is FightPvT)
                        (this as FightPvT).TeleportDefendersQueue();
                    else if (this is FightPvPrism)
                        (this as FightPvPrism).TeleportDefendersQueue();
                    break;
            }
        }
    }
}
