using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Characters.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Effects.Spells.Damages;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Fights.Buffs;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.MySql.Database.World.Spells;
using Sunshine.WorldServer.Game.Maps.Shapes;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights.Types;
using Sunshine.WorldServer.Game.Effects.Spells;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Fights.Diagnostics;
using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.WorldServer.Game.Fights.Mechanics;
using Sunshine.WorldServer.Game.Fights.Telemetry;
using System.Diagnostics;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public abstract class FightActor
    {
        private UniqueIdProvider m_buffIdProvider = new UniqueIdProvider();

        private bool m_deathHandled;

        public bool DeathHandled => m_deathHandled;

        private List<Buff> m_buffList = new List<Buff>();

        private List<SpellStatesEnum> m_states = new List<SpellStatesEnum>();

        private List<ISummoned> m_summons = new List<ISummoned>();

        private Dictionary<Spell, short> m_boostedSpells = new Dictionary<Spell, short>();

        // SaveKrosmoz confirme les IDs officiels Pandawa :
        // Picole = 686, Lait de Bambou = 699, Colère de Zatoïshwan = 701.
        // L enum Sunshine existante peut être décalée selon la génération;
        // ces IDs explicites passent donc avant les valeurs enum.
        private static readonly int[] BambooMilkSpellIds =
        {
            699,
            (int)SpellIdEnum.BambooMilk,
            705
        };

        private static readonly int[] PandawaAlcoholSpellIds =
        {
            686,
            701,
            (int)SpellIdEnum.Boozer,
            (int)SpellIdEnum.DopplesBoozer,
            (int)SpellIdEnum.ZatoishwansWrath,
            (int)SpellIdEnum.ZatoishwansWrath_77,
            692,
            707,
            1111,
            1117,
            1676,
            1682
        };

        private static readonly int[] PandawaAlcoholAppearanceIds =
        {
            667, // Picole
            874  // Colère de Zatoïshwan
        };

        // Selon les bases 2.x, Picole/Saoul peut être stocké comme Drunk (1)
        // ou comme l'état générique 44. Lait de Bambou doit purger les deux,
        // sinon le client garde les sorts/contraintes de l'état saoul.
        private static readonly SpellStatesEnum[] PandawaAlcoholStates =
        {
            SpellStatesEnum.Drunk,
            SpellStatesEnum.State_44,
            SpellStatesEnum.Barmy
        };

        private static readonly EffectsEnum[] PandawaAlcoholStatsEffectIds =
        {
            EffectsEnum.Effect_AddEarthResistPercent,
            EffectsEnum.Effect_AddWaterResistPercent,
            EffectsEnum.Effect_AddAirResistPercent,
            EffectsEnum.Effect_AddFireResistPercent,
            EffectsEnum.Effect_AddNeutralResistPercent,
            EffectsEnum.Effect_AddResistances,
            EffectsEnum.Effect_AddDamageBonusPercent,
            EffectsEnum.Effect_IncreaseDamage_138,
            EffectsEnum.Effect_AddDamageMultiplicator,
            EffectsEnum.Effect_AddCriticalHit,
            EffectsEnum.Effect_AddMP,
            EffectsEnum.Effect_AddMP_128
        };

        public FightActor Carrying { get; set; }
        public FightActor CarryingBy { get; set; }
        public bool IsCarrying => Carrying != null;
        public bool IsCarried => CarryingBy != null;

        public abstract int Id { get; set; }

        public abstract FightTeam Team { get; }

        public abstract ActorLook Look { get; set; }

        public abstract bool IsAttacker();

        public abstract GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null);

        public abstract GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null);

        public abstract FightTeamMemberInformations GetFightTeamMemberInformations();

        public abstract short Level { get; }

        public Dictionary<ushort, byte> CustomCoolDown = new Dictionary<ushort, byte>();

        public abstract StatsFields Stats { get; set; }

        public SpellInventory Spells { get; set; }

        public abstract SpellHistory SpellHistory { get; set; }

        public abstract GameActionFightInvisibilityStateEnum Visibility { get; set; }

        public event Action<FightActor, FightActor> Dead;

        public abstract Fight Fight { get; }

        public Map Map { get { return Fight?.Map; } }

        public ObjectPosition Position { get; set; }

        public virtual bool IsAlive
        {
            get
            {
                return !m_deathHandled
                    && Stats != null
                    && Stats.Health != null
                    && Stats.Health.Total > 0;
            }
        }

        public bool IsDead() { return !IsAlive; }

        protected bool IsGodModeProtected()
        {
            var characterFighter = this as CharacterFighter;
            return characterFighter != null && characterFighter.Character != null && characterFighter.Character.GodMode;
        }


        public bool IsReady { get; set; }

        public bool IsFighterTurn() { return Fight.FighterPlaying == this; }

        public bool HasReflectBuff { get; set; }

        public int LastBombWallTriggerEventId { get; set; }
        public int LastBombWallTriggerCasterId { get; set; }
        public int LastBombExplosionEventId { get; set; }

        public Fights.Results.FightLoot FightLoot { get; set; }

        public EntityDispositionInformations GetEntityDispositionInformations(WorldClient client = null)
        {
            short cellId = Position != null ? Position.Cell : (short)0;
            var direction = Position != null ? Position.Direction : DirectionsEnum.DIRECTION_EAST;

            if (client != null && client.Character != null && !IsVisibleFor(client.Character))
                cellId = -1;

            if (CarryingBy != null)
                return new FightEntityDispositionInformations(cellId, (sbyte)direction, CarryingBy.Id);

            return new EntityDispositionInformations(cellId, (sbyte)direction);
        }
        private short GetRawFightStat(StatsEnum stat)
        {
            var statData = Stats[stat];
            if (statData == null)
                return 0;

            return (short)(statData.Base + statData.Equiped + statData.Context);
        }

        private short GetDisplayedResistPercent(StatsEnum stat)
        {
            return GetRawFightStat(stat);
        }

        private short GetAdditionalPvpResistPercent(StatsEnum baseResistStat, StatsEnum pvpResistStat)
        {
            short baseResist = GetRawFightStat(baseResistStat);
            short pvpResist = GetRawFightStat(pvpResistStat);

            if (baseResist + pvpResist >= 50)
                return (short)(50 - baseResist);

            return pvpResist;
        }
        public GameFightMinimalStatsPreparation GetGameFightMinimalStatsPreparation(WorldClient client = null)
        {
            return new GameFightMinimalStatsPreparation(
                Stats.Health.Total,
                Stats.Health.TotalMax,
                Stats.Health.Base,
                Stats[StatsEnum.PermanentDamagePercent].TotalMax,
                Stats[StatsEnum.Shield].TotalMax,
                (short)Stats.AP.Total,
                (short)Stats.MP.Total,
                Stats[StatsEnum.SummonLimit].TotalMax,
                GetDisplayedResistPercent(StatsEnum.NeutralResistPercent),
                GetDisplayedResistPercent(StatsEnum.EarthResistPercent),
                GetDisplayedResistPercent(StatsEnum.WaterResistPercent),
                GetDisplayedResistPercent(StatsEnum.AirResistPercent),
                GetDisplayedResistPercent(StatsEnum.FireResistPercent),
                (short)Stats[StatsEnum.DodgeAPProbability].TotalMax,
                (short)Stats[StatsEnum.DodgeMPProbability].TotalMax,
                (short)Stats[StatsEnum.TackleBlock].TotalMax,
                (short)Stats[StatsEnum.TackleEvade].TotalMax,
                client == null ? (sbyte)Visibility : (sbyte)GetVisibleFor(client.Character),
                Stats[StatsEnum.Initiative].TotalMax);
        }

        public GameFightMinimalStats GetGameFightMinimalStats(WorldClient client = null)
        {
            return new GameFightMinimalStats(
                Stats.Health.Total,
                Stats.Health.TotalMax,
                Stats.Health.Base,
                Stats[StatsEnum.PermanentDamagePercent].TotalMax,
                Stats[StatsEnum.Shield].TotalMax,
                (short)Stats.AP.Total,
                (short)Stats.MP.Total,
                Stats[StatsEnum.SummonLimit].TotalMax,
                GetDisplayedResistPercent(StatsEnum.NeutralResistPercent),
                GetDisplayedResistPercent(StatsEnum.EarthResistPercent),
                GetDisplayedResistPercent(StatsEnum.WaterResistPercent),
                GetDisplayedResistPercent(StatsEnum.AirResistPercent),
                GetDisplayedResistPercent(StatsEnum.FireResistPercent),
                (short)Stats[StatsEnum.DodgeAPProbability].TotalMax,
                (short)Stats[StatsEnum.DodgeMPProbability].TotalMax,
                (short)Stats[StatsEnum.TackleBlock].TotalMax,
                (short)Stats[StatsEnum.TackleEvade].TotalMax,
                client == null ? (sbyte)Visibility : (sbyte)GetVisibleFor(client.Character));
        }

        public IdentifiedEntityDispositionInformations GetIdentifiedEntityDispositionInformations(WorldClient client = null)
        {
            short cellId = Position != null ? Position.Cell : (short)0;
            var direction = Position != null ? Position.Direction : DirectionsEnum.DIRECTION_EAST;

            if (client != null && client.Character != null && !IsVisibleFor(client.Character))
                cellId = -1;

            return new IdentifiedEntityDispositionInformations(cellId, (sbyte)direction, Id);
        }

        public bool IsFriendlyWith(FightActor fighter)
        {
            if (IsAttacker())
                return fighter.IsAttacker();
            return !fighter.IsAttacker();
        }

        public bool IsEnnemyWith(FightActor fighter)
        {
            return !IsFriendlyWith(fighter);
        }

        public bool CanBeSee(object obj)
        {
            FightActor fighter = obj as FightActor;

            if (fighter != null && fighter.Fight.Id == this.Fight.Id)
                return IsAlive && GetVisibleFor(fighter) != GameActionFightInvisibilityStateEnum.INVISIBLE;
            return true;
        }

        public void SetReadyStatus(bool isReady)
        {
            IsReady = isReady;
            Fight.Clients.ForEach(x => ContextHandler.SendGameFightHumanReadyStateMessage(x, this));
            Fight.CheckAllStatus();
        }

        public void StartTurn()
        {
            if (Fight == null)
                return;

            CombatTelemetry.LogTurnEvent("NextTurnStarted", Fight, this);
            CombatTelemetry.LogTurnEvent("TurnStarted", Fight, this);

            Fight.Timer?.Dispose();
            Fight.Timer = null;

            if (Fight is FightPvPrism prismFight)
            {
                bool noAttackers = prismFight.Team.Attackers == null || prismFight.Team.Attackers.Count == 0 || prismFight.Team.Attackers.TrueForAll(x => x == null || x.IsDead());
                bool noDefenders = prismFight.Team.Defenders == null || prismFight.Team.Defenders.Count == 0 || prismFight.Team.Defenders.TrueForAll(x => x == null || x.IsDead());
                if (noAttackers || noDefenders)
                {
                    Fight.EndFight();
                    return;
                }
            }

            if (Fight.State == FightStateEnum.Fighting && !Fight.CheckFightEnd())
            {
                FrigostBossMechanics.OnTurnStarted(this);

                if (Fight == null || Fight.State != FightStateEnum.Fighting || !IsAlive)
                    return;

                var slaveFighter = this as SlaveFighter;
                bool controlledSlaveTurn = slaveFighter != null && slaveFighter.IsControlledBySummoner();

                if (this is CharacterFighter || controlledSlaveTurn)
                    Fight?.StartAction(35000, "EndTurn");

                if (controlledSlaveTurn)
                {
                    ContextHandler.SendGameFightTurnStartSlaveMessage(Fight.Clients, slaveFighter);
                    slaveFighter.SendControlContext();
                }
                else
                {
                    ContextHandler.SendGameFightTurnStartMessage(Fight.Clients, this);
                }

                ExpireRogueImages();
                Game.Fights.Bombs.BombManager.Instance.OnTurnStarted(this);
                if (Fight == null || Fight.State != FightStateEnum.Fighting || !IsAlive)
                    return;

                foreach (var buff in GetBuffs().ToArray())
                {
                    if (buff is Fights.Buffs.Spells.HealOverTimeBuff hotBuff)
                        hotBuff.Tick();
                    else if (buff is Fights.Buffs.Spells.DamageOverTimeBuff dotBuff)
                        dotBuff.Tick();
                }

                Fight.DecrementGlyphDuration(this);
                Fight.StartSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP);
                Fight.TriggerMarks(this.Position.Cell, this, TriggerTypeEnum.TURN_BEGIN);
                Fight.EndSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP, ActionsEnum.ACTION_FIGHT_TRIGGER_GLYPH);
                TriggerFightBuffs(BuffTriggerType.TURN_BEGIN);
                ContextHandler.SendGameFightSynchronizeMessage(Fight.Clients, Fight.GetAllFighters());
                foreach (CharacterFighter fighter in Fight.GetAllFighters(x => x is CharacterFighter))
                    fighter.Character.RefreshStats();

                if (this is PrismFighter || this is BombFighter)
                {
                    Fights.Diagnostics.FightCombatLogger.LogTurnSkip(Fight, this, this is BombFighter ? "bomb" : "prism");
                    this.EndTurn();
                    return;
                }

                if (!controlledSlaveTurn && this is SummonedMonster staticSummon && !staticSummon.CanPlayTurn)
                {
                    Fights.Diagnostics.FightCombatLogger.LogTurnSkip(Fight, this, "static-summon");
                    this.EndTurn();
                    return;
                }

                if (!controlledSlaveTurn && this is AIFighter aiFighter)
                    aiFighter.PlayAI();
            }
        }

        private void ExpireRogueImages()
        {
            if (Fight == null)
                return;

            var images = Fight.GetAllFighters()
                .OfType<RogueImageFighter>()
                .Where(x => x != null && x.IsAlive && x.Summoner == this)
                .ToArray();

            if (images.Length == 0)
                return;

            foreach (var image in images)
                image.Die(this);

            ContextHandler.SendGameFightTurnListMessage(Fight.Clients, Fight);
        }

        public void EndTurn(string source = "Unknown")
        {
            var currentFight = Fight;
            if (currentFight == null || currentFight.State != FightStateEnum.Fighting || currentFight.FighterPlaying != this)
                return;

            CombatTelemetry.LogTurnEvent("EndTurnRequested", currentFight, this);

            FrigostBossMechanics.OnTurnEnded(this);

            CombatTelemetry.LogTurnEvent("EndTurnRequested", currentFight, this, detail: $"source={source}");

            FrigostBossMechanics.OnTurnEnded(this);

            currentFight.StartSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP);
            currentFight.TriggerMarks(this.Position.Cell, this, TriggerTypeEnum.TURN_END);
            currentFight.EndSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP, ActionsEnum.ACTION_FIGHT_TRIGGER_GLYPH);

            if (Fight != currentFight || currentFight.State != FightStateEnum.Fighting)
                return;

            this.DecrementsAllBuffsDuration();
            ContextHandler.SendGameFightSynchronizeMessage(currentFight.Clients, currentFight.GetAllFighters());
            foreach (CharacterFighter fighter in currentFight.GetAllFighters(x => x is CharacterFighter))
                fighter.Character.RefreshStats();

            this.ResetUsedPoints();
            ContextHandler.SendGameFightTurnEndMessage(currentFight.Clients, this);
            ContextHandler.SendGameFightTurnReadyRequestMessage(currentFight.Clients, this);

            if (currentFight.CheckFightEnd())
                return;

            CombatTelemetry.LogTurnEvent("EndTurnCompleted", currentFight, this);
            CombatTelemetry.LogTurnEvent("NextTurnRequested", currentFight, this);
            currentFight.FighterPlaying = currentFight.GetFighterPlaying();
            if (currentFight.FighterPlaying != null)
                CombatTelemetry.LogTurnEvent("TurnOwner", currentFight, currentFight.FighterPlaying, detail: "source=EndTurn");

            if (CombatReadyCheckerSettings.Enabled)
            {
                var waiters = currentFight.GetAllFighters(x => x is CharacterFighter)
                    .Cast<CharacterFighter>()
                    .ToArray();
                currentFight.Checker.Start(this, waiters);
            }
            else
                currentFight.TryAdvanceTurn("ReadyCheckerDisabled");
        }

        public void ShowCell(short cellId, bool team = true)
        {
            if (team)
            {
                var teamClients = Fight.Clients.Where(x => x.Character.Fighter.IsAttacker() == this.IsAttacker());
                ContextHandler.SendShowCellMessage(teamClients, this, cellId);
            }
            else
                ContextHandler.SendShowCellMessage(this.Fight.Clients, this, cellId);
        }

        /// <summary>
        /// Retorna los luchadores enemigos vivos cuya celda está a distancia 1
        /// (adyacente en diamante isométrico) de <paramref name="cell"/>.
        /// </summary>
        private IEnumerable<FightActor> GetAdjacentEnemies(short cell)
        {
            var origin = MapPoint.GetPoint(cell);
            return Fight.GetAllFighters()
                .Where(f => f != null
                         && f.IsAlive
                         && !f.IsFriendlyWith(this)
                         && !f.IsCarried
                         && f.Position != null
                         && origin.DistanceToCell(f.Position.Point) == 1);
        }

        /// <summary>
        /// Calcula cuántos PM y PA pierde este actor por el placaje de <paramref name="enemy"/>,
        /// SIN aplicarlos todavía. Fórmula oficial Dofus 2.x:
        ///   chance_PM = 50 × (TackleBlock_enemigo / (TackleEvade_yo + 1)) × (Level_enemigo / Level_yo)
        ///   chance_PA = 50 × (TackleBlock_enemigo / (DodgeAP_yo + 1))    × (Level_enemigo / Level_yo)
        /// Rango clampeado a [10 %, 90 %]. Cada PM/PA se tira individualmente.
        /// </summary>
        private (short lostMP, short lostAP) RollTackle(FightActor enemy)
        {
            if (enemy == null || IsDead())
                return (0, 0);

            int myLevel = Math.Max(1, (int)this.Level);
            int enemyLevel = Math.Max(1, (int)enemy.Level);
            double levelRatio = (double)enemyLevel / myLevel;

            int tackleBlock = enemy.Stats[StatsEnum.TackleBlock].TotalMax;
            int tackleEvadeMP = this.Stats[StatsEnum.TackleEvade].TotalMax;
            int tackleEvadeAP = this.Stats[StatsEnum.DodgeAPProbability].TotalMax;

            short lostMP = 0;
            int currentMP = this.Stats.MP.Total;
            if (currentMP > 0)
            {
                double chanceMP = 50.0 * ((double)tackleBlock / (tackleEvadeMP + 1)) * levelRatio;
                chanceMP = Math.Max(10.0, Math.Min(90.0, chanceMP));
                var rng = new AsyncRandom();
                for (int i = 0; i < currentMP; i++)
                    if (rng.Next(1, 100) <= (int)chanceMP)
                        lostMP++;
            }

            short lostAP = 0;
            int currentAP = this.Stats.AP.Total;
            if (currentAP > 0)
            {
                double chanceAP = 50.0 * ((double)tackleBlock / (tackleEvadeAP + 1)) * levelRatio;
                chanceAP = Math.Max(10.0, Math.Min(90.0, chanceAP));
                var rng2 = new AsyncRandom();
                for (int i = 0; i < currentAP; i++)
                    if (rng2.Next(1, 100) <= (int)chanceAP)
                        lostAP++;
            }

            return (lostMP, lostAP);
        }

        /// <summary>
        /// Aplica la pérdida de PM/PA ya calculada por RollTackle, usando al enemigo como source
        /// para que el cliente muestre el visual correcto. DEBE llamarse FUERA de SEQUENCE_MOVE.
        /// </summary>
        private void ApplyTackleResult(FightActor enemy, short lostMP, short lostAP)
        {
            if (IsDead())
                return;

            if (lostMP > 0)
            {
                Stats.MP.Used += lostMP;
                Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_LOST, enemy, this, -lostMP);
            }

            if (lostAP > 0)
            {
                Stats.AP.Used += lostAP;
                Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_LOST, enemy, this, -lostAP);
            }
        }

        public void StartMove(Path path, bool traped = false)
        {
            if (IsCarried)
            {
                if (this is CharacterFighter carriedCharacter && carriedCharacter.Character != null)
                    carriedCharacter.Character.SendServerMessage("Il est impossible de ce déplacer pour le moment, vous êtes porté par un pandawa");

                return;
            }

            Fight.StartSequence(SequenceTypeEnum.SEQUENCE_MOVE);

            short[] cells = path.GetPath(); // índice 0 = celda inicial (ya ocupada)
            List<short> newCells = new List<short>();
            short newMPCount = 0;

            // Acumula los resultados del placaje para aplicarlos DESPUÉS de EndSequence,
            // así el cliente muestra la variación de PA/PM fuera de la animación de movimiento.
            var pendingTackles = new List<(FightActor enemy, short lostMP, short lostAP)>();

            if (IsFighterTurn() && !path.IsEmpty())
            {
                // Enemigos adyacentes a la celda de origen antes de cualquier movimiento.
                var tacklingEnemies = GetAdjacentEnemies(cells[0]).ToList();

                for (int i = 0; i < cells.Length; i++)
                {
                    if (this.Stats.MP.Total < i)
                        break;

                    newMPCount++;
                    newCells.Add(cells[i]);

                    // Detección de salida de zona de control (placaje).
                    if (i > 0 && tacklingEnemies.Count > 0)
                    {
                        short prevCell = cells[i - 1];

                        foreach (var enemy in tacklingEnemies.ToArray())
                        {
                            if (enemy == null || !enemy.IsAlive || enemy.Position == null)
                            {
                                tacklingEnemies.Remove(enemy);
                                continue;
                            }

                            bool wasAdjacentBefore = MapPoint.GetPoint(prevCell).DistanceToCell(enemy.Position.Point) == 1;
                            bool isAdjacentNow = MapPoint.GetPoint(cells[i]).DistanceToCell(enemy.Position.Point) == 1;

                            if (wasAdjacentBefore && !isAdjacentNow)
                            {
                                var (lostMP, lostAP) = RollTackle(enemy);
                                if (lostMP > 0 || lostAP > 0)
                                    pendingTackles.Add((enemy, lostMP, lostAP));

                                tacklingEnemies.Remove(enemy);

                                if (this.Stats.MP.Total - lostMP <= 0)
                                {
                                    path.CutPath(i + 1);
                                    goto MovementDone;
                                }
                            }
                        }

                        foreach (var newEnemy in GetAdjacentEnemies(cells[i]))
                        {
                            if (!tacklingEnemies.Contains(newEnemy))
                                tacklingEnemies.Add(newEnemy);
                        }
                    }

                    if (i > 0 && Fight.ShouldTriggerOnMove(cells[i], this))
                    {
                        path.CutPath(i + 1);
                        traped = true;
                        break;
                    }

                    if (i < cells.Length - 1 && !Fight.IsCellFree(cells[i + 1]))
                    {
                        path.CutPath(i + 1);
                        break;
                    }
                }

            MovementDone:

                short usedMp = (short)(newMPCount - 1);
                path = new Path(this.Map, newCells);
                foreach (var client in Fight.Clients)
                {
                    if (client.Character.Fighter != null && this.CanBeSee(client.Character.Fighter))
                        ContextHandler.SendGameMapMovementMessage(client, Id, path.GetServerPathKeys());
                }
                Position = path.EndPathPosition;

                if (usedMp > 0)
                    this.UseMP(usedMp);

                if (IsCarrying && Carrying != null)
                    Carrying.Position.Cell = Position.Cell;

                if (this is BombFighter movedBomb)
                    Game.Fights.Bombs.BombManager.Instance.CheckWalls(Fight, movedBomb.Summoner);
                if (traped)
                {
                    Fight?.StartSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP);
                    Fight?.TriggerMarks(Position.Cell, this, TriggerTypeEnum.MOVE);
                    Fight?.EndSequence(SequenceTypeEnum.SEQUENCE_GLYPH_TRAP, ActionsEnum.ACTION_FIGHT_TRIGGER_TRAP);
                }
            }

            // Cerrar la secuencia de movimiento ANTES de aplicar el placaje, para que el
            // cliente reciba los packets de PA/PM perdidos fuera de la animación de movimiento.
            Fight?.EndSequence(SequenceTypeEnum.SEQUENCE_MOVE, ActionsEnum.ACTION_CHARACTER_MOVEMENT);

            foreach (var (enemy, lostMP, lostAP) in pendingTackles)
                ApplyTackleResult(enemy, lostMP, lostAP);

            RefreshControllingClientFightState();
        }

        public bool NeedMoveToCastSpell(Spell spell, short cell)
        {
            short[] castZone = this.GetCastZone(spell.Template);
            if (!castZone.Contains(cell))
                return true;
            return false;
        }

        protected bool IsBombSummonSpell(Spell spell)
        {
            if (spell == null)
                return false;

            return (spell.Effects != null && spell.Effects.Any(x => x != null && x.Id == EffectsEnum.Effect_SummonBomb))
                || (spell.CriticalEffects != null && spell.CriticalEffects.Any(x => x != null && x.Id == EffectsEnum.Effect_SummonBomb));
        }

        protected bool HasReachedBombPlacementLimit(Spell spell)
        {
            if (Fight == null || !IsBombSummonSpell(spell))
                return false;

            return Fight.GetAllFighters()
                .OfType<BombFighter>()
                .Count(x => x != null && x.IsAlive && !x.IsExploded && x.Summoner == this) >= BombFighter.BombLimit;
        }

        protected void NotifyBombPlacementLimit()
        {
            var characterFighter = this as CharacterFighter;
            if (characterFighter?.Character != null)
                characterFighter.Character.SendServerMessage("Vous ne pouvez pas poser plus de 3 bombes.");
        }

        public void CastSpell(Spell spell, short cell)
        {
            if (spell != null && spell.Id == 0 && this is CharacterFighter characterCaster)
            {
                characterCaster.CastCloseCombat(cell);
                return;
            }

            if (spell == null || spell.Template == null || Fight == null)
                return;

            var castStopwatch = Stopwatch.StartNew();
            var castResult = this.CanCastSpell(spell, cell);
            if (castResult != SpellCastResult.OK)
            {
                if (HasReachedBombPlacementLimit(spell))
                    NotifyBombPlacementLimit();

                if (spell != null && spell.Id == SlaveFighter.RoublabotSpellId && this is CharacterFighter roublard && roublard.Character != null)
                    roublard.Character.SendServerMessage($"Roublabot : lancement refusé ({castResult}).", System.Drawing.Color.Red);

                CombatTelemetry.LogSpellEvent(
                    "SpellCastFailed",
                    Fight,
                    this,
                    spell?.Id,
                    spell?.Level,
                    result: castResult.ToString(),
                    durationMs: castStopwatch.ElapsedMilliseconds);
                return;
            }

            var currentFight = Fight;
            if (currentFight == null || currentFight.State != FightStateEnum.Fighting)
                return;

            CombatTelemetry.LogSpellEvent(
                "SpellCastStarted",
                currentFight,
                this,
                spell.Id,
                spell.Level,
                targetIds: new[] { cell });

            currentFight.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);

            bool isBambooMilk = IsBambooMilkSpell(spell);
            bool isPandawaAlcohol = !isBambooMilk && IsPandawaAlcoholActivationSpell(spell);
            if (isBambooMilk)
                ApplyBambooMilkPandawaReset();

            FightSpellCastCriticalEnum spellCastCritical = RollCriticalDice(spell);
            List<Effect> effects = spellCastCritical == FightSpellCastCriticalEnum.NORMAL
                ? (spell.Effects ?? new List<Effect>())
                : (spell.CriticalEffects ?? new List<Effect>());

            IEnumerable<SpellEffectHandler> spellEffectsHandler = effects.Select(x =>
                EffectManager.Instance.SpellEffects.ContainsKey(x.Id)
                    ? EffectManager.Instance.SpellEffects[x.Id]()
                    : null);

            var customCastHandler = Game.Spells.Casts.SpellCastManager.Instance.CreateHandler(
                this,
                spell,
                cell,
                spellCastCritical == FightSpellCastCriticalEnum.CRITICAL_HIT,
                effects);

            bool silentCast = customCastHandler != null
                ? customCastHandler.SilentCast
                : spellEffectsHandler.Any(x => x != null && x.RequireSilentCast());

            short displayedCell = customCastHandler != null ? customCastHandler.TargetedCell : cell;

            if (Visibility == GameActionFightInvisibilityStateEnum.INVISIBLE && !IsInvisibleSpellCast(effects))
                SetInvisibilityState(GameActionFightInvisibilityStateEnum.DETECTED, this);

            currentFight.OnSpellCasted(this, spell, displayedCell, spellCastCritical, silentCast);
            this.UseAP((short)spell.Template.ApCost);

            if (spellCastCritical != FightSpellCastCriticalEnum.CRITICAL_FAIL)
            {

                if (customCastHandler != null)
                {
                    try
                    {
                        customCastHandler.Execute();
                    }
                    catch (Exception ex)
                    {
                        Logs.Logger.WriteError($"Custom spell cast failed for spell {spell.Id}: {ex.Message}");

                        foreach (var effect in effects)
                        {
                            EffectDispatcher.Dispatch(this, spell, effect, cell);

                            if (Fight != currentFight || currentFight.State == FightStateEnum.Ended)
                                break;
                        }
                    }
                }
                else
                {
                    foreach (var effect in effects)
                    {
                        EffectDispatcher.Dispatch(this, spell, effect, cell);

                        if (Fight != currentFight || currentFight.State == FightStateEnum.Ended)
                            break;
                    }
                }

                if (Fight == currentFight && currentFight.State == FightStateEnum.Fighting && isBambooMilk)
                    ApplyBambooMilkPandawaReset();

                if (Fight == currentFight && currentFight.State == FightStateEnum.Fighting && isPandawaAlcohol)
                    EnsurePandawaAlcoholClientState(spell);

                if (Fight == currentFight && currentFight.State == FightStateEnum.Fighting && SpellHistory != null)
                    SpellHistory.RegisterCastedSpell(spell, currentFight.GetOneFighter(displayedCell));
            }

            if (Fight != currentFight || currentFight.State != FightStateEnum.Fighting)
                return;

            currentFight.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL, ActionsEnum.ACTION_FIGHT_CAST_SPELL);
            RefreshControllingClientFightState();
            currentFight.CheckFightEnd();

            castStopwatch.Stop();
            CombatTelemetry.LogSpellEvent(
                "SpellCastResolved",
                currentFight,
                this,
                spell.Id,
                spell.Level,
                targetIds: new[] { cell },
                result: "OK",
                durationMs: castStopwatch.ElapsedMilliseconds);
        }

        public virtual SpellCastResult CanCastSpell(Spell spell, short cell)
        {
            if (spell == null)
                return SpellCastResult.HAS_NOT_SPELL;

            if (spell.Id == 0 && this is CharacterFighter characterCaster)
                return characterCaster.CanCastCloseCombat(cell);

            if (!IsFighterTurn() || IsDead())
                return SpellCastResult.CANNOT_PLAY;

            if (!HasSpell((short)spell.Id))
                return SpellCastResult.HAS_NOT_SPELL;

            if ((long)Stats.AP.Total < (long)((ulong)spell.Template.ApCost))
                return SpellCastResult.NOT_ENOUGH_AP;

            if (HasReachedBombPlacementLimit(spell))
                return SpellCastResult.UNKNOWN;

            bool cellIsFree = Fight.IsCellFree(cell);
            bool ignoreNeedFreeCell = !cellIsFree && IsBombSummonSpell(spell);
            if (((spell.Template.NeedFreeCell && !cellIsFree) && !ignoreNeedFreeCell) || (spell.Template.NeedTakenCell && cellIsFree))
                return SpellCastResult.CELL_NOT_FREE;

            if (spell.StatesForbidden.Any(x => HasState((SpellStatesEnum)x)))
                return SpellCastResult.STATE_FORBIDDEN;

            if (spell.StatesRequired.Any(x => !HasState((SpellStatesEnum)x)))
                return SpellCastResult.STATE_REQUIRED;

            short[] castZone = GetCastZone(spell.Template);
            if (!castZone.Contains(cell))
                return SpellCastResult.NOT_IN_ZONE;

            if (!SpellHistory.CanCastSpell(spell, cell))
                return SpellCastResult.HISTORY_ERROR;

            return SpellCastResult.OK;
        }

        public bool IsInvisibleSpellCast(List<Effect> effects)
        {
            if (!(this is CharacterFighter))
                return true;
            else
            {
                if (!effects.Any(x => x.Duration == 0 && (x.Id == EffectsEnum.Effect_DamageNeutral || x.Id == EffectsEnum.Effect_DamageAir
                 || x.Id == EffectsEnum.Effect_DamageWater || x.Id == EffectsEnum.Effect_DamageFire
                 || x.Id == EffectsEnum.Effect_DamageEarth || x.Id == EffectsEnum.Effect_Punishment_Damage
                 || x.Id == EffectsEnum.Effect_1015)))
                    return true;
                return false;
            }
        }

        public short[] GetCastZone(SpellTemplate spellLevel)
        {
            long num = spellLevel.Range;
            if (spellLevel.RangeCanBeBoosted)
            {
                num += this.Stats[StatsEnum.Range].TotalMax;
                if (num < spellLevel.MinRange)
                {
                    num = spellLevel.MinRange;
                }
                num = Math.Min(num, 280u);
            }
            IShape shape;
            if (spellLevel.CastInDiagonal && spellLevel.CastInLine)
            {
                shape = new Cross((byte)spellLevel.MinRange, (byte)num)
                {
                    AllDirections = true
                };
            }
            else
            {
                if (spellLevel.CastInLine)
                {
                    shape = new Cross((byte)spellLevel.MinRange, (byte)num);
                }
                else
                {
                    if (spellLevel.CastInDiagonal)
                    {
                        shape = new Cross((byte)spellLevel.MinRange, (byte)num)
                        {
                            Diagonal = true
                        };
                    }
                    else
                    {
                        shape = new Lozenge((byte)spellLevel.MinRange, (byte)num);
                    }
                }
            }
            return shape.GetCells(Position.Cell, Map);
        }

        public bool HasSpell(short spellId)
        {
            return Spells != null && Spells.HasSpell(spellId);
        }

        public void InflictDamage(int amount, FightActor source, bool isPoisoned = false)
        {
            Damage damage = new Damage(amount);
            damage.Source = source;
            InflictDamage(damage, isPoisoned);
        }

        public void InflictDamage(Damage damage, bool isPoisoned = false)
        {
            if (damage == null)
                return;

            if (this is RogueImageFighter rogueImage)
            {
                rogueImage.Die(damage.Source);
                return;
            }

            if (HasActiveRogueImages())
                RemoveRogueImages(damage.Source);

            if (IsGodModeProtected())
            {
                Fight?.OnDamageReducted(damage.Amount, damage.Source, this);
                return;
            }

            if (FrigostBossMechanics.BeforeDamage(this, damage, isPoisoned))
                return;

            if (this.HasState(SpellStatesEnum.Invulnerable))
            {
                Fight.OnDamageReducted(8400, damage.Source, this);
                return;
            }

            // Sacrifice logic
            var sacrificeBuff = (global::Sunshine.WorldServer.Game.Fights.Buffs.Spells.SacrificeBuff)GetBuffs(x => x is global::Sunshine.WorldServer.Game.Fights.Buffs.Spells.SacrificeBuff).FirstOrDefault();
            if (sacrificeBuff != null && sacrificeBuff.Caster != this && !isPoisoned)
            {
                // Exchange positions if needed (2.x rules usually swap on hit if not already swapped)
                if (Position.Cell != sacrificeBuff.Caster.Position.Cell)
                {
                    this.ExchangePositions(sacrificeBuff.Caster);
                }

                // Redirect damage to the caster of the sacrifice
                sacrificeBuff.Caster.InflictDamage(damage, isPoisoned);
                return;
            }

            NormalizeFightHealth(false);

            damage.GenerateDamages();

            if (damage.Source != null && damage.Spell != null)
                damage.Amount += damage.Source.GetSpellBoost(damage.Spell);

            damage.Amount = this.CalculateDamage(damage.Source, damage.Amount, damage.EffectSchool);

            double armorReduction = this.CalculateArmorReduction(damage.EffectSchool, this, isPoisoned);

            damage.Amount = this.CalculateDamageResistance(damage.Source, damage.Amount, damage.EffectSchool, Fight.Type == FightTypeEnum.FIGHT_TYPE_AGRESSION, armorReduction, isPoisoned);

            if (this.HasReflectBuff && !isPoisoned)
            {
                Fight.OnDamageReflected(this, damage.Source, damage.Amount);
                damage.Source?.InflictDamage(damage.Amount, damage.Source);
                return;
            }

            if (damage.Amount <= 0)
                damage.Amount = 0;

            if (armorReduction > 0)
                Fight.OnDamageReducted((int)armorReduction, damage.Source, this);

            int lostShield = 0;
            StatsBuff shieldBuff = (StatsBuff)this.GetBuffs(x => x.ActionId == 1040).FirstOrDefault();
            int currentShield = Math.Max(0, this.Stats[StatsEnum.Shield].TotalMax);

            if (currentShield > damage.Amount) // shield zozo
            {
                int shieldLoss = damage.Amount;
                if (shieldBuff != null)
                    shieldBuff.Value = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, shieldBuff.Value - shieldLoss));

                lostShield = shieldLoss;
                this.Stats[StatsEnum.Shield].Context -= shieldLoss;
                Fight.OnShieldPointsChanged(-shieldLoss, lostShield, damage.Source, this);

                if (shieldBuff != null)
                    this.UpdateBuff(shieldBuff);

                return;
            }

            if (currentShield > 0)
            {
                int shieldLoss = Math.Max(0, this.Stats[StatsEnum.Shield].Context);
                if (shieldBuff != null)
                    shieldBuff.Value = 0;

                lostShield = shieldLoss;
                damage.Amount -= shieldLoss;
                this.Stats[StatsEnum.Shield].Context = 0;
                Fight.OnShieldPointsChanged(-shieldLoss, lostShield, damage.Source, this);

                if (shieldBuff != null)
                    this.UpdateBuff(shieldBuff);
            }

            NormalizeFightHealth(false);

            int currentLife = Math.Max(0, this.Stats.Health.Total);
            if (damage.Amount > currentLife)
                damage.Amount = currentLife;

            if (damage.Amount < 0)
                damage.Amount = 0;

            int lifeBeforeDamage = Math.Max(0, this.Stats.Health.Total);
            int damageErosion = CalculateErosionDamage(damage.Amount);
            this.Stats.Health.Taken += damage.Amount;
            this.Stats.Health.PermanentTaken += damageErosion;

            NormalizeFightHealth(false);

            int lifeAfterDamage = Math.Max(0, this.Stats.Health.Total);
            int realLifeLost = Math.Max(0, lifeBeforeDamage - lifeAfterDamage);

            if (realLifeLost > 0)
                Fight.OnLifePointsChanged(-realLifeLost, damage.Source, this);

            FightCombatLogger.LogDamage(Fight, damage.Source, this, damage);

            // Si le coup met la cible à 0 PV, on traite la mort tout de suite.
            // Les buffs déclenchés "après avoir subi des dommages" ne doivent pas ajouter de vitalité/PV
            // sur une cible déjà morte, sinon le client affiche 0 PV puis un +PV fantôme.
            if (TryKillIfNoHealth(damage.Source))
                return;

            var punishmentBuffs = GetBuffs(x => x is global::Sunshine.WorldServer.Game.Fights.Buffs.Spells.PunishmentBuff)
                .Cast<global::Sunshine.WorldServer.Game.Fights.Buffs.Spells.PunishmentBuff>()
                .ToArray();

            foreach (var punishmentBuff in punishmentBuffs)
                punishmentBuff.OnDamaged(damage.Source, damage.Amount);

            TriggerFightBuffs(BuffTriggerType.AFTER_ATTACKED, damage);
        }

        public void Heal(int healPoints, FightActor source, bool withBoost = false)
        {
            if (Stats == null || Stats.Health == null)
                return;

            if (IsDead() || this.HasState(SpellStatesEnum.Unhealable))
                return;

            if (healPoints <= 0)
                return;

            NormalizeFightHealth(false);

            int missingLife = Math.Max(0, Stats.Health.TotalMax - Stats.Health.Total);
            if (missingLife <= 0)
                return;

            if (withBoost)
                healPoints = CalculateHeal(healPoints);

            healPoints = Math.Max(0, Math.Min(healPoints, missingLife));
            if (healPoints <= 0)
                return;

            int lifeBefore = Stats.Health.Total;
            this.Stats.Health.Taken -= healPoints;
            NormalizeFightHealth(false);

            int realHeal = Math.Max(0, Stats.Health.Total - lifeBefore);
            if (realHeal > 0)
                Fight.OnLifePointsChanged(realHeal, source, this);
        }

        public void ExchangePositions(FightActor with)
        {
            if (this.HasState(SpellStatesEnum.Gravity) || this.HasState(SpellStatesEnum.Rooted)
                || with.HasState(SpellStatesEnum.Gravity) || with.HasState(SpellStatesEnum.Rooted))
                return;

            short cell = this.Position.Cell;
            this.Position.Cell = with.Position.Cell;
            with.Position.Cell = cell;

            if (this is BombFighter thisBomb)
                BombManager.Instance.CheckWalls(Fight, thisBomb.Summoner);

            if (with is BombFighter otherBomb)
                BombManager.Instance.CheckWalls(Fight, otherBomb.Summoner);

            ActionsHandler.SendGameActionFightExchangePositionsMessage(Fight.Clients, this, with);
        }

        public void Kill(FightActor killer)
        {
            if (m_deathHandled || Fight == null || Stats == null || Stats.Health == null || !IsAlive)
                return;

            Stats.Health.Taken = Math.Max(Stats.Health.Taken, Stats.Health.TotalMax);
            NormalizeFightHealth(false);
            FightCombatLogger.LogKill(Fight, killer, this);
            TryKillIfNoHealth(killer ?? this);
        }

        public bool TryKillIfNoHealth(FightActor killer = null)
        {
            if (m_deathHandled || Fight == null || Stats == null || Stats.Health == null)
                return false;

            NormalizeFightHealth(false);
            if (Stats.Health.Total > 0)
                return false;

            OnDead(this, killer ?? this);
            return true;
        }

        public virtual void OnDead(FightActor target, FightActor fighter)
        {
            if (Fight == null || target == null)
                return;

            if (target.m_deathHandled)
                return;

            target.m_deathHandled = true;

            if (target.Stats != null && target.Stats.Health != null)
            {
                target.Stats.Health.Taken = Math.Max(target.Stats.Health.Taken, target.Stats.Health.TotalMax);
                target.NormalizeFightHealth(false);
            }

            target.BreakCarryLink(false);

            Fight.StartSequence(SequenceTypeEnum.SEQUENCE_CHARACTER_DEATH);
            ActionsHandler.SendGameActionFightDeathMessage(Fight.Clients, fighter, target);
            Fight.TimeLine.Fighters.Remove(target);
            Fight.TimeLine.AddLeavers(target);
            Fight.EndSequence(SequenceTypeEnum.SEQUENCE_CHARACTER_DEATH, ActionsEnum.ACTION_CHARACTER_DEATH);
            Fight.RemoveTriggers(target);
            FrigostBossMechanics.OnActorDied(target, fighter);
            target.RemoveAllBuffs();
            target.KillAllSummons();
            target.Dead?.Invoke(target, fighter);

            if (Fight is FightPvPrism prismFight)
            {
                bool noAttackers = prismFight.Team.Attackers == null || prismFight.Team.Attackers.Count == 0 || prismFight.Team.Attackers.TrueForAll(x => x == null || x.IsDead());
                bool noDefenders = prismFight.Team.Defenders == null || prismFight.Team.Defenders.Count == 0 || prismFight.Team.Defenders.TrueForAll(x => x == null || x.IsDead());
                if (target is PrismFighter || noAttackers || noDefenders)
                {
                    Fight.EndFight();
                    return;
                }
            }

            if (Fight.CheckFightEnd())
                return;

            if (target.IsFighterTurn())
                target.EndTurn();
        }


        public void BreakCarryLink(bool refreshDisposition = true)
        {
            var carrier = CarryingBy;
            var carried = Carrying;

            if (carrier != null)
                carrier.Carrying = null;

            if (carried != null)
                carried.CarryingBy = null;

            if (carrier != null)
            {
                foreach (var buff in carrier.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carrying).ToArray())
                    carrier.RemoveBuff(buff);

                if (carrier.HasState(SpellStatesEnum.Carrying))
                    carrier.RemoveState(SpellStatesEnum.Carrying);
            }

            if (carried != null)
            {
                foreach (var buff in carried.GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carried).ToArray())
                    carried.RemoveBuff(buff);

                if (carried.HasState(SpellStatesEnum.Carried))
                    carried.RemoveState(SpellStatesEnum.Carried);
            }

            if (CarryingBy != null)
            {
                foreach (var buff in GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carried).ToArray())
                    RemoveBuff(buff);

                if (HasState(SpellStatesEnum.Carried))
                    RemoveState(SpellStatesEnum.Carried);

                CarryingBy.Carrying = null;
                CarryingBy = null;
            }

            if (Carrying != null)
            {
                foreach (var buff in GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == SpellStatesEnum.Carrying).ToArray())
                    RemoveBuff(buff);

                if (HasState(SpellStatesEnum.Carrying))
                    RemoveState(SpellStatesEnum.Carrying);

                Carrying.CarryingBy = null;
                Carrying = null;
            }

            if (refreshDisposition && Fight != null)
            {
                var fightersToRefresh = new[] { this, carrier, carried }
                    .Where(x => x != null && x.Fight == Fight)
                    .Distinct()
                    .ToArray();

                if (fightersToRefresh.Length > 0)
                    ContextHandler.SendGameEntitiesDispositionMessage(Fight.Clients, fightersToRefresh);
            }
        }

        public void NormalizeFightHealth(bool resetNonCharacterLostLife = false)
        {
            if (Stats == null || Stats.Health == null)
                return;

            if (resetNonCharacterLostLife && !(this is CharacterFighter))
            {
                Stats.Health.PermanentTaken = 0;
                Stats.Health.Taken = 0;
                return;
            }

            if (Stats.Health.PermanentTaken < 0)
                Stats.Health.PermanentTaken = 0;

            int maxLife = Math.Max(1, Stats.Health.TotalMax);

            if (Stats.Health.Taken < 0)
                Stats.Health.Taken = 0;

            if (Stats.Health.Taken > maxLife)
                Stats.Health.Taken = maxLife;
        }

        public static bool StatAffectsLifeMaximum(StatsEnum stat)
        {
            return stat == StatsEnum.Health || stat == StatsEnum.Vitality;
        }

        public int GetCurrentLife()
        {
            if (Stats == null || Stats.Health == null)
                return 0;

            return Math.Max(0, Stats.Health.Total);
        }

        public void PreserveCurrentLifeAfterMaxLifeChange(int lifeBefore, FightActor source = null, bool notify = true)
        {
            if (Stats == null || Stats.Health == null)
                return;

            int maxLife = Math.Max(1, Stats.Health.TotalMax);
            int wantedLife = Math.Min(Math.Max(0, lifeBefore), maxLife);

            // Un changement de PV max (vitalité/châtiment/buff) ne doit jamais soigner
            // silencieusement le combattant. On garde les PV courants et on recalcule
            // uniquement la valeur Taken qui représente les PV manquants.
            if (lifeBefore > 0 && wantedLife <= 0)
                wantedLife = 1;

            Stats.Health.Taken = Math.Max(0, maxLife - wantedLife);
            NormalizeFightHealth(false);

            int lifeAfter = GetCurrentLife();
            int delta = lifeAfter - Math.Max(0, lifeBefore);

            // Un changement de PV max ne doit jamais afficher un +PV comme un soin.
            // On notifie seulement une perte réelle quand le nouveau maximum est inférieur aux PV actuels.
            if (notify && Fight != null && delta < 0)
                Fight.OnLifePointsChanged(delta, source ?? this, this);
        }

        private static bool IsBambooMilkSpell(Spell spell)
        {
            if (spell == null)
                return false;

            if (BambooMilkSpellIds.Contains(spell.Id))
                return true;

            var allEffects = (spell.Effects ?? new List<Effect>())
                .Concat(spell.CriticalEffects ?? new List<Effect>());

            return allEffects.Any(effect =>
                effect != null
                && (effect.Id == EffectsEnum.Effect_RemoveState || effect.Id == EffectsEnum.Effect_952)
                && (IsPandawaAlcoholStateValue(effect.Value)
                    || IsPandawaAlcoholStateValue((int)effect.DiceNum)
                    || IsPandawaAlcoholStateValue((int)effect.DiceFace)));
        }

        private static bool IsPandawaAlcoholSpell(Spell spell)
        {
            return spell != null && PandawaAlcoholSpellIds.Contains(spell.Id);
        }

        private static IEnumerable<Effect> GetSpellEffects(Spell spell)
        {
            if (spell == null)
                return Enumerable.Empty<Effect>();

            return (spell.Effects ?? new List<Effect>())
                .Concat(spell.CriticalEffects ?? new List<Effect>());
        }

        private static bool IsPandawaAlcoholActivationSpell(Spell spell)
        {
            if (IsPandawaAlcoholSpell(spell))
                return true;

            foreach (var effect in GetSpellEffects(spell))
            {
                if (effect == null)
                    continue;

                bool addsAlcoholState = effect.Id == EffectsEnum.Effect_AddState
                    && (IsPandawaAlcoholStateValue(effect.Value)
                        || IsPandawaAlcoholStateValue((int)effect.DiceNum)
                        || IsPandawaAlcoholStateValue((int)effect.DiceFace));

                bool changesToAlcoholLook = (effect.Id == EffectsEnum.Effect_ChangeAppearance || effect.Id == EffectsEnum.Effect_ChangeAppearance_335)
                    && (PandawaAlcoholAppearanceIds.Contains(effect.Value)
                        || PandawaAlcoholAppearanceIds.Contains((int)effect.DiceNum)
                        || PandawaAlcoholAppearanceIds.Contains((int)effect.DiceFace));

                if (addsAlcoholState || changesToAlcoholLook)
                    return true;
            }

            return false;
        }

        private static SpellStatesEnum[] GetPandawaAlcoholStatesFromSpell(Spell spell)
        {
            var states = new HashSet<SpellStatesEnum>();

            foreach (var effect in GetSpellEffects(spell))
            {
                if (effect == null || effect.Id != EffectsEnum.Effect_AddState)
                    continue;

                var candidates = new[] { effect.Value, (int)effect.DiceNum, (int)effect.DiceFace };
                foreach (var candidate in candidates)
                {
                    if (IsPandawaAlcoholStateValue(candidate))
                        states.Add((SpellStatesEnum)candidate);
                }
            }

            if (states.Count == 0)
                states.Add(SpellStatesEnum.Drunk);

            return states.ToArray();
        }

        private static bool IsPandawaAlcoholState(SpellStatesEnum state)
        {
            return PandawaAlcoholStates.Contains(state);
        }

        private static bool IsPandawaAlcoholStateValue(int value)
        {
            return PandawaAlcoholStates.Any(x => (int)x == value);
        }

        private static bool IsPandawaAlcoholEffect(Effect effect)
        {
            if (effect == null)
                return false;

            bool targetsAlcoholState = IsPandawaAlcoholStateValue(effect.Value)
                || IsPandawaAlcoholStateValue((int)effect.DiceNum)
                || IsPandawaAlcoholStateValue((int)effect.DiceFace);

            if ((effect.Id == EffectsEnum.Effect_AddState || effect.Id == EffectsEnum.Effect_RemoveState || effect.Id == EffectsEnum.Effect_952) && targetsAlcoholState)
                return true;

            bool targetsPandawaAppearance = PandawaAlcoholAppearanceIds.Contains(effect.Value)
                || PandawaAlcoholAppearanceIds.Contains((int)effect.DiceNum)
                || PandawaAlcoholAppearanceIds.Contains((int)effect.DiceFace);

            if ((effect.Id == EffectsEnum.Effect_ChangeAppearance || effect.Id == EffectsEnum.Effect_ChangeAppearance_335) && targetsPandawaAppearance)
                return true;

            return false;
        }

        private static bool IsPandawaBreedSpell(Spell spell)
        {
            return spell != null && spell.Template != null && spell.Template.SpellBreed == (int)BreedEnum.Pandawa;
        }

        private static bool IsPandawaAlcoholStatsBuff(Buff buff)
        {
            var statsBuff = buff as StatsBuff;
            if (statsBuff == null || statsBuff.Effect == null)
                return false;

            if (!PandawaAlcoholStatsEffectIds.Contains(statsBuff.Effect.Id))
                return false;

            return buff.Target == buff.Caster && IsPandawaBreedSpell(buff.Spell);
        }

        private static bool IsPandawaAlcoholBuff(Buff buff)
        {
            if (buff == null)
                return false;

            if (buff is StateBuff stateBuff && IsPandawaAlcoholState(stateBuff.State))
                return true;

            if (buff is SkinBuff && (IsPandawaAlcoholSpell(buff.Spell) || IsPandawaAlcoholEffect(buff.Effect)))
                return true;

            if (buff is SpellBuff && (IsPandawaAlcoholSpell(buff.Spell) || IsPandawaBreedSpell(buff.Spell)))
                return true;

            if (IsPandawaAlcoholStatsBuff(buff))
                return true;

            if (IsPandawaAlcoholSpell(buff.Spell))
                return true;

            if (IsPandawaAlcoholEffect(buff.Effect))
                return true;

            return false;
        }

        private static bool IsCarryStateBuff(Buff buff)
        {
            var stateBuff = buff as StateBuff;
            return stateBuff != null && (stateBuff.State == SpellStatesEnum.Carrying || stateBuff.State == SpellStatesEnum.Carried);
        }

        private static void TrackPandawaBuffRemoval(Buff buff, HashSet<SpellStatesEnum> statesToForceOff, HashSet<int> spellIdsToDispell)
        {
            if (buff == null)
                return;

            var stateBuff = buff as StateBuff;
            if (stateBuff != null && statesToForceOff != null)
                statesToForceOff.Add(stateBuff.State);

            var spellBuff = buff as SpellBuff;
            if (spellBuff != null && spellBuff.BoostedSpell != null && spellIdsToDispell != null)
                spellIdsToDispell.Add(spellBuff.BoostedSpell.Id);

            if (buff.Spell != null && IsPandawaAlcoholSpell(buff.Spell) && spellIdsToDispell != null)
                spellIdsToDispell.Add(buff.Spell.Id);
        }

        public void ApplyBambooMilkPandawaReset()
        {
            // V10 - Retour sobre immédiat : retrait serveur + client des états, buffs,
            // boosts de sorts et effets affichés dans la timeline. On collecte les IDs
            // avant de retirer les buffs, sinon SpellBuff.Dispell() vide le dictionnaire
            // et le client ne reçoit jamais le GameActionFightDispellSpellMessage attendu.
            if (Fight == null)
                return;

            var statesToForceOff = new HashSet<SpellStatesEnum>(PandawaAlcoholStates);
            var spellIdsToDispell = new HashSet<int>();

            foreach (var buff in GetBuffs(x => x != null && x.Target == this && !IsCarryStateBuff(x)).ToArray())
            {
                TrackPandawaBuffRemoval(buff, statesToForceOff, spellIdsToDispell);
                RemoveBuff(buff);
            }

            ClearSpellBoostsAndNotify(spellIdsToDispell);

            foreach (var state in statesToForceOff.ToArray())
            {
                while (HasState(state))
                    RemoveState(state);
            }

            foreach (var state in statesToForceOff.Distinct())
                m_states.RemoveAll(x => x == state);

            ForceSendStateInactive(statesToForceOff);
            BreakCarryLink();

            ContextHandler.SendGameFightSynchronizeMessage(Fight.Clients, this);

            if (this is CharacterFighter characterFighter)
            {
                characterFighter.Character?.RefreshStats();

                if (characterFighter.Character?.Client != null)
                {
                    InventoryHandler.SendSpellListMessage(characterFighter.Character.Client, true);
                    RefreshControllingClientFightState();
                }
            }
        }

        public void RemovePandawaAlcoholState()
        {
            var statesToForceOff = new HashSet<SpellStatesEnum>(PandawaAlcoholStates);
            var spellIdsToDispell = new HashSet<int>();

            bool removed;
            do
            {
                removed = false;
                foreach (var buff in GetBuffs(IsPandawaAlcoholBuff).ToArray())
                {
                    TrackPandawaBuffRemoval(buff, statesToForceOff, spellIdsToDispell);
                    RemoveBuff(buff);
                    removed = true;
                }
            }
            while (removed && GetBuffs(IsPandawaAlcoholBuff).Any());

            foreach (var buff in GetBuffs(x =>
                x != null
                && x.Target == this
                && (x is StateBuff || x is SpellBuff || x is SkinBuff || x is StatsBuff)
                && (IsPandawaBreedSpell(x.Spell) || IsPandawaAlcoholSpell(x.Spell) || IsPandawaAlcoholEffect(x.Effect))).ToArray())
            {
                TrackPandawaBuffRemoval(buff, statesToForceOff, spellIdsToDispell);
                RemoveBuff(buff);
            }

            ClearSpellBoostsAndNotify(spellIdsToDispell);

            foreach (var state in statesToForceOff.ToArray())
            {
                while (HasState(state))
                    RemoveState(state);
            }

            foreach (var state in statesToForceOff.Distinct())
                m_states.RemoveAll(x => x == state);

            ForceSendStateInactive(statesToForceOff);
            BreakCarryLink();

            if (Fight != null)
                ContextHandler.SendGameFightSynchronizeMessage(Fight.Clients, this);

            if (this is CharacterFighter characterFighter)
            {
                characterFighter.Character?.RefreshStats();

                if (characterFighter.Character?.Client != null)
                {
                    InventoryHandler.SendSpellListMessage(characterFighter.Character.Client, true);
                    RefreshControllingClientFightState();
                }
            }
        }

        private void ClearSpellBoostsAndNotify(IEnumerable<int> spellIdsToNotify = null)
        {
            var spellIds = new HashSet<int>();

            if (spellIdsToNotify != null)
            {
                foreach (var spellId in spellIdsToNotify)
                {
                    if (spellId > 0)
                        spellIds.Add(spellId);
                }
            }

            foreach (var spell in m_boostedSpells.Keys.Where(x => x != null).ToArray())
                spellIds.Add(spell.Id);

            m_boostedSpells.Clear();

            if (Fight == null)
                return;

            foreach (var spellId in spellIds)
                Fight.Clients.ForEach(x => x.Send(new GameActionFightDispellSpellMessage(950, Id, Id, spellId)));
        }

        public void EnsurePandawaAlcoholClientState(Spell spell)
        {
            if (Fight == null || spell == null)
                return;

            // Certaines bases privées n'appliquent que le skin de Picole/Zato.
            // On pose alors un vrai StateBuff Saoul/Picole pour que le serveur,
            // le panel de sorts et la timeline client soient synchronisés.
            foreach (var state in GetPandawaAlcoholStatesFromSpell(spell))
            {
                bool hasStateBuff = GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == state).Any();
                if (!hasStateBuff)
                {
                    var stateEffect = new Effect
                    {
                        Id = EffectsEnum.Effect_AddState,
                        DiceNum = (uint)(int)state,
                        Value = (int)state,
                        Duration = 1000
                    };

                    AddBuff(new StateBuff(this, this, spell, stateEffect, 1000, true, state));
                }

                if (!HasState(state))
                    AddState(state);

                Fight.Clients.ForEach(x => x.Send(new GameActionFightStateChangeMessage(950, Id, Id, (short)state, true)));
            }

            if (this is CharacterFighter characterFighter)
            {
                characterFighter.Character?.RefreshStats();

                if (characterFighter.Character?.Client != null)
                {
                    InventoryHandler.SendSpellListMessage(characterFighter.Character.Client, true);
                    RefreshControllingClientFightState();
                }
            }
        }

        private void ForceSendStateInactive(IEnumerable<SpellStatesEnum> states)
        {
            if (Fight == null || states == null)
                return;

            foreach (var state in states.Distinct())
            {
                m_states.RemoveAll(x => x == state);
                Fight.Clients.ForEach(x => x.Send(new GameActionFightStateChangeMessage(950, Id, Id, (short)state, false)));
            }
        }
        public virtual void ResetUsedPoints()
        {
            Stats.AP.Used = 0;
            Stats.MP.Used = 0;
        }

        public void ResetFightProperties()
        {
            Fight?.RemoveTriggers(this);
            ResetUsedPoints();
            m_deathHandled = false;
            RemoveAllBuffs();
            KillAllSummons();
            foreach (StatsEnum stats in typeof(StatsEnum).GetEnumValues())
                this.Stats[stats].Context = 0;

            // Les monstres et invocations ne doivent jamais conserver des PV perdus d'un combat précédent.
            // Pour les personnages, on conserve les dégâts afin de respecter la régénération hors combat,
            // mais on borne tout pour éviter les PV négatifs/décalés.
            NormalizeFightHealth(!(this is CharacterFighter));
        }

        public void UseAP(short amount)
        {
            if (IsGodModeProtected())
            {
                Fight?.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_WIN, this, this, amount);
                return;
            }

            Stats.AP.Used += amount;
            Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_USE, this, this, -amount);
            TriggerLoseHpByUsingApBuffs(amount);
        }

        private void TriggerLoseHpByUsingApBuffs(short usedAp)
        {
            if (usedAp <= 0 || IsDead())
                return;

            var buffs = GetBuffs(x => x is LoseHpByUsingApBuff)
                .Cast<LoseHpByUsingApBuff>()
                .ToArray();

            foreach (var buff in buffs)
                buff.OnApUsed(usedAp);
        }

        public void UseMP(short amount)
        {
            if (IsGodModeProtected())
            {
                Fight?.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_WIN, this, this, amount);
                return;
            }

            Stats.MP.Used += amount;
            Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_USE, this, this, -amount);
        }

        protected void RefreshControllingClientFightState()
        {
            if (Fight == null || this is not CharacterFighter characterFighter || characterFighter.Client == null)
                return;

            // A full fight synchronize right after a spell/move cuts client-side animations and
            // makes several actions look like teleports (summons, pushes, pulls, walls, etc.).
            // Keep only the local stats refresh here; action/position packets already did the visual work.
            characterFighter.Character?.RefreshStats();
        }

        protected void SynchronizeFightStateIfIdle()
        {
            if (Fight == null || Fight.IsVisualSequenceActive)
                return;

            ContextHandler.SendGameFightSynchronizeMessage(Fight.Clients, this);
        }

        public void RegainAP(short amount)
        {
            Stats.AP.Used -= amount;
            Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_WIN, this, this, amount);
        }
        public void RegainMP(short amount)
        {
            Stats.MP.Used -= amount;
            Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_WIN, this, this, amount);
        }

        public void LostAP(short amount)
        {
            if (IsGodModeProtected())
            {
                Fight?.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_WIN, this, this, amount);
                return;
            }

            Stats.AP.Used += amount;
            Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_ACTION_POINTS_LOST, this, this, -amount);
        }

        public void LostMP(short amount)
        {
            if (IsGodModeProtected())
            {
                Fight?.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_WIN, this, this, amount);
                return;
            }

            Stats.MP.Used += amount;
            Fight.OnFightPointsVariation(ActionsEnum.ACTION_CHARACTER_MOVEMENT_POINTS_LOST, this, this, -amount);
        }

        public short RollMPLose(FightActor caster, int amount)
        {
            short subMP = 0;
            int totalMP = this.Stats.MP.Total;
            int totalMaxMP = this.Stats.MP.TotalMax;
            int MPAttack = caster.Stats[StatsEnum.MPAttack].TotalMax;
            int MPDodge = this.Stats[StatsEnum.DodgeMPProbability].TotalMax;

            for (int i = 0; i < amount; i++)
            {
                double percent = 50 * ((double)(MPAttack / (MPDodge + 1))) * (((double)totalMaxMP / totalMP));

                if (percent >= 90)
                    percent = 90;
                else if (percent <= 10)
                    percent = 10;

                double rdn = new AsyncRandom().Next(1, 100);
                if (percent >= (int)rdn)
                {
                    subMP++;
                    totalMaxMP--;
                }
            }
            return subMP;
        }

        public short RollAPLose(FightActor caster, int amount)
        {
            short subAP = 0;
            int totalAP = this.Stats.AP.Total;
            int totalMaxAP = this.Stats.AP.TotalMax;
            int APAttack = caster.Stats[StatsEnum.APAttack].TotalMax;
            int APDodge = this.Stats[StatsEnum.DodgeAPProbability].TotalMax;

            for (int i = 0; i < amount; i++)
            {
                double percent = 50 * ((double)(APAttack / (APDodge + 1))) * (((double)totalMaxAP / totalAP));

                double rdn = new AsyncRandom().Next(1, 100);

                if (percent >= 90)
                    percent = 90;
                else if (percent <= 10)
                    percent = 10;


                if (percent >= (int)rdn)
                {
                    subAP++;
                    totalMaxAP--;
                }
            }
            return subAP;
        }

        protected virtual int CalculateDamage(FightActor caster, int damage, EffectSchoolEnum type)
        {
            if (caster == null)
                return Math.Max(0, damage);

            int result;
            switch (type)
            {
                case EffectSchoolEnum.Neutral:
                    result = (int)((double)(damage * (100 + caster.Stats[StatsEnum.Strength].TotalMax + caster.Stats[StatsEnum.DamageBonusPercent].TotalMax +
                    caster.Stats[StatsEnum.DamageMultiplicator].TotalMax * 100)) / 100.0 + (double)(caster.Stats[StatsEnum.DamageBonus].TotalMax +
                    caster.Stats[StatsEnum.PhysicalDamage].TotalMax + caster.Stats[StatsEnum.NeutralDamageBonus].TotalMax));
                    break;
                case EffectSchoolEnum.Earth:
                    result = (int)((double)(damage * (100 + caster.Stats[StatsEnum.Strength].TotalMax + caster.Stats[StatsEnum.DamageBonusPercent].TotalMax +
                    caster.Stats[StatsEnum.DamageMultiplicator].TotalMax * 100)) / 100.0 + (double)(caster.Stats[StatsEnum.DamageBonus].TotalMax +
                    caster.Stats[StatsEnum.PhysicalDamage].TotalMax + caster.Stats[StatsEnum.EarthDamageBonus].TotalMax));
                    break;
                case EffectSchoolEnum.Water:
                    result = (int)((double)(damage * (100 + caster.Stats[StatsEnum.Chance].TotalMax + caster.Stats[StatsEnum.DamageBonusPercent].TotalMax +
                    caster.Stats[StatsEnum.DamageMultiplicator].TotalMax * 100)) / 100.0 + (double)(caster.Stats[StatsEnum.DamageBonus].TotalMax +
                    caster.Stats[StatsEnum.MagicDamage].TotalMax + caster.Stats[StatsEnum.WaterDamageBonus].TotalMax));
                    break;
                case EffectSchoolEnum.Air:
                    result = (int)((double)(damage * (100 + caster.Stats[StatsEnum.Agility].TotalMax + caster.Stats[StatsEnum.DamageBonusPercent].TotalMax +
                    caster.Stats[StatsEnum.DamageMultiplicator].TotalMax * 100)) / 100.0 + (double)(caster.Stats[StatsEnum.DamageBonus].TotalMax +
                    caster.Stats[StatsEnum.MagicDamage].TotalMax + caster.Stats[StatsEnum.AirDamageBonus].TotalMax));
                    break;
                case EffectSchoolEnum.Fire:
                    result = (int)((double)(damage * (100 + caster.Stats[StatsEnum.Intelligence].TotalMax + caster.Stats[StatsEnum.DamageBonusPercent].TotalMax +
                    caster.Stats[StatsEnum.DamageMultiplicator].TotalMax * 100)) / 100.0 + (double)(caster.Stats[StatsEnum.DamageBonus].TotalMax +
                    caster.Stats[StatsEnum.MagicDamage].TotalMax + caster.Stats[StatsEnum.FireDamageBonus].TotalMax));
                    break;
                default:
                    result = damage;
                    break;
            }

            return Math.Max(0, result);
        }

        private int CalculateDamageResistance(FightActor caster, int damage, EffectSchoolEnum type, bool pvp, double armor = 0, bool isPoisoned = false)
        {
            if (isPoisoned)
                return 0;

            double percentResist;
            double flatResist;

            switch (type)
            {
                case EffectSchoolEnum.Neutral:
                    percentResist = GetDisplayedResistPercent(StatsEnum.NeutralResistPercent) +
                                    (pvp ? GetAdditionalPvpResistPercent(StatsEnum.NeutralResistPercent, StatsEnum.PvpNeutralResistPercent) : 0);
                    flatResist = Stats[StatsEnum.NeutralElementReduction].TotalMax +
                                 (pvp ? Stats[StatsEnum.PvpNeutralElementReduction].TotalMax : 0) +
                                 Stats[StatsEnum.PhysicalDamageReduction].TotalMax;
                    break;

                case EffectSchoolEnum.Earth:
                    percentResist = GetDisplayedResistPercent(StatsEnum.EarthResistPercent) +
                                    (pvp ? GetAdditionalPvpResistPercent(StatsEnum.EarthResistPercent, StatsEnum.PvpEarthResistPercent) : 0);
                    flatResist = Stats[StatsEnum.EarthElementReduction].TotalMax +
                                 (pvp ? Stats[StatsEnum.PvpEarthElementReduction].TotalMax : 0) +
                                 Stats[StatsEnum.PhysicalDamageReduction].TotalMax;
                    break;

                case EffectSchoolEnum.Water:
                    percentResist = GetDisplayedResistPercent(StatsEnum.WaterResistPercent) +
                                    (pvp ? GetAdditionalPvpResistPercent(StatsEnum.WaterResistPercent, StatsEnum.PvpWaterResistPercent) : 0);
                    flatResist = Stats[StatsEnum.WaterElementReduction].TotalMax +
                                 (pvp ? Stats[StatsEnum.PvpWaterElementReduction].TotalMax : 0) +
                                 Stats[StatsEnum.MagicDamageReduction].TotalMax;
                    break;

                case EffectSchoolEnum.Air:
                    percentResist = GetDisplayedResistPercent(StatsEnum.AirResistPercent) +
                                    (pvp ? GetAdditionalPvpResistPercent(StatsEnum.AirResistPercent, StatsEnum.PvpAirResistPercent) : 0);
                    flatResist = Stats[StatsEnum.AirElementReduction].TotalMax +
                                 (pvp ? Stats[StatsEnum.PvpAirElementReduction].TotalMax : 0) +
                                 Stats[StatsEnum.MagicDamageReduction].TotalMax;
                    break;

                case EffectSchoolEnum.Fire:
                    percentResist = GetDisplayedResistPercent(StatsEnum.FireResistPercent) +
                                    (pvp ? GetAdditionalPvpResistPercent(StatsEnum.FireResistPercent, StatsEnum.PvpFireResistPercent) : 0);
                    flatResist = Stats[StatsEnum.FireElementReduction].TotalMax +
                                 (pvp ? Stats[StatsEnum.PvpFireElementReduction].TotalMax : 0) +
                                 Stats[StatsEnum.MagicDamageReduction].TotalMax;
                    break;

                default:
                    return Math.Max(0, damage);
            }

            return Math.Max(0, (int)((1.0 - percentResist / 100.0) * ((double)damage - (flatResist + armor))));
        }

        private double CalculateArmorReduction(EffectSchoolEnum damageType, FightActor caster, bool isPoisoned = false)
        {
            if (isPoisoned)
                return 0;

            switch (damageType)
            {
                case EffectSchoolEnum.Neutral:
                    return (double)(this.Stats[StatsEnum.NeutralDamageArmor].TotalMax * (100 + 5 * caster.Level) / 100);
                case EffectSchoolEnum.Earth:
                    return (double)(this.Stats[StatsEnum.EarthDamageArmor].TotalMax * (100 + 5 * caster.Level) / 100);
                case EffectSchoolEnum.Water:
                    return (double)(this.Stats[StatsEnum.WaterDamageArmor].TotalMax * (100 + 5 * caster.Level) / 100);
                case EffectSchoolEnum.Air:
                    return (double)(this.Stats[StatsEnum.AirDamageArmor].TotalMax * (100 + 5 * caster.Level) / 100);
                case EffectSchoolEnum.Fire:
                    return (double)(this.Stats[StatsEnum.FireDamageArmor].TotalMax * (100 + 5 * caster.Level) / 100);
                default:
                    return 0;
            }
        }

        public int CalculateHeal(int heal)
        {
            return (int)((double)(heal * (100 + this.Stats[StatsEnum.Intelligence].TotalMax)) / 100.0 + (double)this.Stats[StatsEnum.HealBonus].TotalMax);
        }

        public int CalculateErosionDamage(int damages)
        {
            if (damages <= 0)
                return 0;

            var erosion = Stats[StatsEnum.Erosion].TotalMax;

            if (erosion > 50)
                erosion = 50;

            return (damages * (erosion + 10 > 50 ? 50 : erosion + 10)) / 100;
        }

        public void GeneratePosition()
        {
            if (Map == null)
                return;

            Map.EnsureFightCells();

            var cells = IsAttacker() ? (Map.RedCells ?? new List<short>()) : (Map.BlueCells ?? new List<short>());

            if (Position == null)
                Position = new ObjectPosition(Map, 0, IsAttacker() ? DirectionsEnum.DIRECTION_EAST : DirectionsEnum.DIRECTION_WEST);
            else
            {
                Position.Map = Map;
                Position.Direction = IsAttacker() ? DirectionsEnum.DIRECTION_EAST : DirectionsEnum.DIRECTION_WEST;
            }

            short? selectedCell = null;

            foreach (var cell in cells)
            {
                if (Fight == null || Fight.IsCellFree(cell))
                {
                    selectedCell = cell;
                    break;
                }
            }

            if (!selectedCell.HasValue && Position != null && Map.IsValidFightPlacementCell(Position.Cell) && (Fight == null || Fight.IsCellFree(Position.Cell)))
                selectedCell = Position.Cell;

            if (!selectedCell.HasValue && Map.Cells != null)
            {
                for (short i = 0; i < Map.Cells.Length; i++)
                {
                    if (Map.IsValidFightPlacementCell(i) && (Fight == null || Fight.IsCellFree(i)))
                    {
                        selectedCell = i;
                        break;
                    }
                }
            }

            Position.Cell = selectedCell ?? (short)0;
        }



        public int PopNextBuffId()
        {
            return this.m_buffIdProvider.Pop();
        }

        public void FreeBuffId(int id)
        {
            this.m_buffIdProvider.Push(id);
        }

        public bool HasState(SpellStatesEnum state)
        {
            return this.m_states.Contains(state);
        }

        public void AddState(SpellStatesEnum state)
        {
            if (this.m_states.Contains(state))
                return;

            this.m_states.Add(state);
            Fight?.Clients.ForEach(x => x.Send(new GameActionFightStateChangeMessage(950, Id, Id, (short)state, true)));
        }

        public void RemoveState(SpellStatesEnum state)
        {
            if (!this.m_states.Contains(state))
                return;

            this.m_states.RemoveAll(x => x == state);
            Fight?.Clients.ForEach(x => x.Send(new GameActionFightStateChangeMessage(950, Id, Id, (short)state, false)));
        }

        public IEnumerable<Buff> GetBuffs()
        {
            return m_buffList;
        }

        public IEnumerable<Buff> GetBuffs(Predicate<Buff> predicate)
        {
            return
                from entry in m_buffList
                where predicate(entry)
                select entry;
        }

        public void AddBuff(Buff buff, bool apply = true)
        {
            if (this.BuffMaxStackReached(buff))
                return;

            this.m_buffList.Add(buff);
            ContextHandler.SendGameActionFightDispellableEffectMessage(Fight.Clients, buff, false);

            if (buff is Fights.Buffs.Spells.DamageOverTimeBuff || buff is Fights.Buffs.Spells.HealOverTimeBuff)
                FightCombatLogger.LogBuffAdd(Fight, this, buff);

            if (apply)
                buff.Apply();

            SynchronizeFightStateIfIdle();

            if (this is CharacterFighter characterFighter)
                characterFighter.Character?.RefreshStats();
        }

        public void TriggerFightBuffs(BuffTriggerType trigger, object token = null)
        {
            foreach (var buff in GetBuffs(x => x is TriggerBuff).Cast<TriggerBuff>().ToArray())
            {
                FightCombatLogger.LogTrigger(Fight, this, trigger, buff);
                buff.TryTrigger(trigger, token);
            }
        }

        public void RemoveBuff(Buff buff, bool dispell = true)
        {
            if (buff is TriggerBuff triggerBuff)
                triggerBuff.TryTrigger(BuffTriggerType.BUFF_ENDED);

            this.FreeBuffId(buff.Id);
            this.m_buffList.Remove(buff);
            ActionsHandler.SendGameActionFightDispellEffectMessage(Fight.Clients, buff.Caster, buff.Target, buff);

            if (dispell)
                buff.Dispell();

            SynchronizeFightStateIfIdle();

            if (this is CharacterFighter characterFighter)
                characterFighter.Character?.RefreshStats();
        }

        public void UpdateBuff(Buff buff)
        {
            ContextHandler.SendGameActionFightDispellableEffectMessage(Fight.Clients, buff, true);
            SynchronizeFightStateIfIdle();

            if (this is CharacterFighter characterFighter)
                characterFighter.Character?.RefreshStats();
        }

        public void RemoveAllBuffs()
        {
            Buff[] buffs = this.m_buffList.ToArray();
            Buff[] buffs2 = buffs;
            for (int i = 0; i < buffs2.Length; i++)
                this.RemoveBuff(buffs2[i]);

        }

        public bool BuffMaxStackReached(Buff buff)
        {
            return buff.Spell.Template.MaxStack > 0 &&
                buff.Spell.Template.MaxStack <= this.m_buffList.Count((Buff entry) => entry.Spell == buff.Spell && entry.Effect.Id == buff.Effect.Id);

        }

        public void DecrementBuffsDuration(FightActor caster)
        {
            Buff[] buffs = this.m_buffList.Where(x => x.Caster == caster && x.IsBuffEnded()).ToArray();
            Buff[] buffs2 = buffs;
            for (int i = 0; i < buffs2.Length; i++)
            {
                if (buffs2[i] is Fights.Buffs.Spells.DamageOverTimeBuff || buffs2[i] is Fights.Buffs.Spells.HealOverTimeBuff)
                    FightCombatLogger.LogBuffExpire(Fight, this, buffs2[i], "duration");
                this.RemoveBuff(buffs2[i]);
            }
        }

        public void DecrementsAllBuffsDuration()
        {
            DecrementBuffsDuration();
            foreach (var fighter in Fight.GetAllFighters())
                DecrementBuffsDuration(fighter);
        }

        private void DecrementBuffsDuration()
        {
            foreach (var buff in this.m_buffList)
            {
                if (buff.Duration - 1 >= 0)
                    buff.Duration -= 1;
            }
        }

        public short GetSpellBoost(Spell spell)
        {
            if (m_boostedSpells.ContainsKey(spell))
                return m_boostedSpells[spell];
            return 0;
        }

        public void AddSpellBoost(Spell spell, short boost)
        {
            if (m_boostedSpells.ContainsKey(spell))
                m_boostedSpells[spell] += boost;
            else
                m_boostedSpells.Add(spell, boost);
        }

        public void AddStatBoost(StatsEnum stat, short boost)
        {
            bool lifeMaximumChanged = StatAffectsLifeMaximum(stat);
            int lifeBefore = lifeMaximumChanged ? GetCurrentLife() : 0;

            this.Stats[stat].Context += boost;

            if (lifeMaximumChanged)
                PreserveCurrentLifeAfterMaxLifeChange(lifeBefore, this, true);

            SynchronizeFightStateIfIdle();
        }

        public void RemoveSpellBoost(Spell spell, short boost)
        {
            if (m_boostedSpells.ContainsKey(spell))
            {
                Dictionary<Spell, short> buffedSpells;
                (buffedSpells = m_boostedSpells)[spell] = Convert.ToInt16(buffedSpells[spell] - boost);
                if (m_boostedSpells[spell] == 0)
                    m_boostedSpells.Remove(spell);
            }
        }

        public int SummonedCount { get; set; }

        public bool CanSummon()
        {
            return this.SummonedCount < this.Stats[StatsEnum.SummonLimit].Total;
        }

        public void AddSummon(ISummoned summon)
        {
            if (summon is SummonedMonster && (summon as SummonedMonster).Monster.Record.UseSummonSlot)
                SummonedCount++;
            this.m_summons.Add(summon);
        }

        public void RemoveSummon(ISummoned summon)
        {
            if (summon == null)
                return;

            if (!this.m_summons.Remove(summon))
                return;

            if (summon is SummonedMonster && (summon as SummonedMonster).Monster.Record.UseSummonSlot && SummonedCount > 0)
                SummonedCount--;
        }

        public void RemoveAllSummons()
        {
            SummonedCount = 0;
            this.m_summons.Clear();
        }

        public RogueImageFighter[] GetActiveRogueImages()
        {
            return m_summons
                .OfType<RogueImageFighter>()
                .Where(x => x != null && x.IsAlive)
                .ToArray();
        }

        public bool HasActiveRogueImages()
        {
            return GetActiveRogueImages().Length > 0;
        }

        public void RemoveRogueImages(FightActor byFighter = null)
        {
            var images = GetActiveRogueImages();
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null)
                    continue;

                image.Die(byFighter ?? this);
            }
        }

        public void KillAllSummons()
        {
            ISummoned[] summons = m_summons.ToArray();
            RemoveAllSummons();

            for (int i = 0; i < summons.Length; i++)
            {
                var summoned = summons[i];
                if (summoned == null)
                    continue;

                if (summoned is BombFighter bomb)
                {
                    bomb.DestroySilently();
                    continue;
                }

                summoned.Die();
            }
        }

        public GameActionFightInvisibilityStateEnum GetVisibleFor(Character character)
        {
            if (character == null || Fight == null)
                return Visibility;

            if (!character.IsInFight() || character.Fight == null || character.Fight.Id != Fight.Id)
                return Visibility;

            if (character.Fighter == null)
                return Visibility;

            if (character.Fighter.IsFriendlyWith(this))
            {
                if (this.Visibility == GameActionFightInvisibilityStateEnum.INVISIBLE)
                    return GameActionFightInvisibilityStateEnum.DETECTED;
            }
            return Visibility;
        }

        public bool IsVisibleFor(Character character)
        {
            return GetVisibleFor(character) != GameActionFightInvisibilityStateEnum.INVISIBLE;
        }

        public bool IsVisibleFor(FightActor fighter)
        {
            return GetVisibleFor(fighter) != GameActionFightInvisibilityStateEnum.INVISIBLE;
        }

        public GameActionFightInvisibilityStateEnum GetVisibleFor(FightActor fighter)
        {
            if (fighter == null)
                return Visibility;

            if (fighter.IsFriendlyWith(this))
            {
                if (this.Visibility != GameActionFightInvisibilityStateEnum.VISIBLE)
                    return GameActionFightInvisibilityStateEnum.DETECTED;
            }
            return Visibility;
        }

        public void SetInvisibilityState(GameActionFightInvisibilityStateEnum state, FightActor caster)
        {
            var lastVisibleState = Visibility;
            Visibility = state;
            foreach (CharacterFighter fighter in Fight.GetAllFighters().Where(x => x is CharacterFighter))
            {
                ActionsHandler.SendGameActionFightInvisibilityMessage(fighter.Client, caster, this, GetVisibleFor(fighter));
                if (lastVisibleState == GameActionFightInvisibilityStateEnum.INVISIBLE && state != GameActionFightInvisibilityStateEnum.INVISIBLE)
                    ContextHandler.SendGameFightShowFighterMessage(fighter.Client, this);
            }
        }

        public FightSpellCastCriticalEnum RollCriticalDice(Spell spell)
        {
            AsyncRandom asyncRandom = new AsyncRandom();
            FightSpellCastCriticalEnum result = FightSpellCastCriticalEnum.NORMAL;
            if (spell.Template.CriticalFailureProbability != 0u && asyncRandom.Next((int)spell.Template.CriticalFailureProbability) == 0)
                result = FightSpellCastCriticalEnum.CRITICAL_FAIL;
            else
            {
                if (spell.Template.CriticalHitProbability != 0u && asyncRandom.Next((int)this.CalculateCriticRate(spell.Template.CriticalHitProbability)) == 0)
                    result = FightSpellCastCriticalEnum.CRITICAL_HIT;
            }
            return result;
        }

        protected double CalculateCriticRate(double baseRate)
        {
            double num = System.Math.Floor((baseRate - (double)this.Stats[StatsEnum.CriticalHit].TotalMax) * 2.99011001130495 / System.Math.Log((double)(this.Stats[StatsEnum.Agility].TotalMax + 12), 2.7182818284590451));
            return (num > 2.0) ? num : 2.0;
        }

        public FightActor Clone()
        {
            return (FightActor)this.MemberwiseClone();
        }
    }
}
