using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Bombs;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class BombFighter : SummonedMonster
    {
        public const int BombLimit = 3;
        public const int WallMinSize = 1;
        public const int WallMaxSize = 6;
        public const int ExplosionZone = 2;
        public const int ComboStart = 40;
        public const int ComboIncrease = 20;
        public const int ComboTurnsLimit = 3;

        private const int FireElement = 2;
        private const int AirElement = 3;
        private const int WaterElement = 4;

        private const int FireExplosionSpellId = 2822;
        private const int AirExplosionSpellId = 2845;
        private const int WaterExplosionSpellId = 2830;

        // In 2.3.x the visible bomb explosion spell triggers dedicated per-target damage spells.
        // Using the target spell ids avoids replaying chain/helper effects as extra hits.
        private const int FireExplosionDamageSpellId = 2823;
        private const int AirExplosionDamageSpellId = 2827;
        private const int WaterExplosionDamageSpellId = 2831;

        private const int FireWallSpellId = 2825;
        private const int AirWallSpellId = 2829;
        private const int WaterWallSpellId = 2833;

        private static readonly Dictionary<int, int> ExplosionSpellIds = new Dictionary<int, int>
        {
            { FireElement, FireExplosionSpellId },
            { AirElement, AirExplosionSpellId },
            { WaterElement, WaterExplosionSpellId },
        };

        private static readonly Dictionary<int, int> ExplosionDamageSpellIds = new Dictionary<int, int>
        {
            { FireElement, FireExplosionDamageSpellId },
            { AirElement, AirExplosionDamageSpellId },
            { WaterElement, WaterExplosionDamageSpellId },
        };

        private static readonly Dictionary<int, int> WallSpellIds = new Dictionary<int, int>
        {
            { FireElement, FireWallSpellId },
            { AirElement, AirWallSpellId },
            { WaterElement, WaterWallSpellId },
        };

        private static readonly Dictionary<int, Color> WallColors = new Dictionary<int, Color>
        {
            { FireElement, Color.FromArgb(255, 0, 0) },
            { AirElement, Color.Olive },
            { WaterElement, Color.FromArgb(102, 204, 255) },
        };

        private bool _deathHandled;
        private int _extraGrowthSteps;
        private readonly Color _wallColor;
        private readonly List<Game.Fights.Triggers.BombWallBinding> _wallBindings = new List<Game.Fights.Triggers.BombWallBinding>();

        public DateTime SummonedAtUtc { get; } = DateTime.UtcNow;

        public int Combo { get; private set; }
        public int ComboTurns { get; private set; }
        public int DelayedExplosionTurns { get; private set; }
        public int Element { get; }
        public bool IsExploded { get; private set; }
        public Spell ExplosionSpell { get; }
        public Spell ExplosionDamageSpell { get; }
        public Spell WallSpell { get; }

        public BombFighter(Monster monster, FightActor summoner, ObjectPosition position, int element)
            : base(monster, summoner, position)
        {
            Element = element;
            Combo = 0;
            ComboTurns = 0;
            IsExploded = false;
            ExplosionSpell = ResolveExplosionSpell(element, (sbyte)(monster?.Grade?.GradeId ?? 1)) ?? monster?.Spells?.FirstOrDefault();
            ExplosionDamageSpell = ResolveExplosionDamageSpell(element, (sbyte)(monster?.Grade?.GradeId ?? 1)) ?? ExplosionSpell;
            WallSpell = ResolveWallSpell(element, (sbyte)(monster?.Grade?.GradeId ?? 1));
            _wallColor = ResolveWallColor(element);
            ApplyGrowthLook();
        }

        public Color GetWallColor()
        {
            return _wallColor;
        }

        public IReadOnlyCollection<Game.Fights.Triggers.BombWallBinding> WallBindings => _wallBindings.AsReadOnly();

        public void AddWallBinding(Game.Fights.Triggers.BombWallBinding binding)
        {
            if (binding == null || _wallBindings.Contains(binding))
                return;

            binding.Removed += OnWallBindingRemoved;
            _wallBindings.Add(binding);
        }

        private void OnWallBindingRemoved(Game.Fights.Triggers.BombWallBinding binding)
        {
            if (binding == null)
                return;

            binding.Removed -= OnWallBindingRemoved;
            _wallBindings.Remove(binding);
        }

        public bool IncreaseCombo(bool allowOvergrowth = false)
        {
            if (ComboTurns >= ComboTurnsLimit)
            {
                if (!allowOvergrowth)
                    return false;

                _extraGrowthSteps++;
                ApplyGrowthLook();

                if (Fight != null)
                    ContextHandler.SendGameFightShowFighterMessage(Fight.Clients, new List<FightActor> { this });

                return true;
            }

            Combo += ComboStart + (ComboIncrease * ComboTurns);
            ComboTurns++;
            ApplyGrowthLook();

            if (Fight != null)
                ContextHandler.SendGameFightShowFighterMessage(Fight.Clients, new List<FightActor> { this });

            return true;
        }

        public void AddComboBonus(int amount, bool grow = false)
        {
            if (amount <= 0)
            {
                if (grow)
                    IncreaseCombo();
                return;
            }

            Combo += amount;
            if (grow && ComboTurns < ComboTurnsLimit)
                ComboTurns++;

            if (grow)
                ApplyGrowthLook();

            if (Fight != null)
                ContextHandler.SendGameFightShowFighterMessage(Fight.Clients, new List<FightActor> { this });
        }

        public void ScheduleDelayedExplosion(int turns)
        {
            if (turns < 1)
                turns = 1;

            DelayedExplosionTurns = turns;
            UpdateCountdownStates();
        }

        public bool AdvanceDelayedExplosion()
        {
            if (DelayedExplosionTurns <= 0)
                return false;

            DelayedExplosionTurns--;
            UpdateCountdownStates();
            return DelayedExplosionTurns <= 0;
        }

        public void ClearDelayedExplosion()
        {
            if (DelayedExplosionTurns == 0 && !HasState(SpellStatesEnum.Countdown__1) && !HasState(SpellStatesEnum.Countdown__2))
                return;

            DelayedExplosionTurns = 0;
            UpdateCountdownStates();
        }

        private void UpdateCountdownStates()
        {
            RemoveCountdownState(SpellStatesEnum.Countdown__1);
            RemoveCountdownState(SpellStatesEnum.Countdown__2);

            if (DelayedExplosionTurns <= 0)
                return;

            AddState(DelayedExplosionTurns == 1 ? SpellStatesEnum.Countdown__1 : SpellStatesEnum.Countdown__2);
        }

        private void RemoveCountdownState(SpellStatesEnum state)
        {
            var buffs = GetBuffs(x => x is StateBuff stateBuff && stateBuff.State == state).ToArray();
            foreach (var buff in buffs)
                RemoveBuff(buff);

            if (HasState(state))
                RemoveState(state);
        }

        public bool IsBoundWith(BombFighter bomb)
        {
            if (bomb == null || bomb == this || Fight == null || Summoner == null || bomb.Summoner != Summoner)
                return false;

            if (Element != bomb.Element || Position == null || bomb.Position == null)
                return false;

            var dist = Position.Point.DistanceToCell(bomb.Position.Point);
            if (dist <= WallMinSize || dist > (WallMaxSize + 1))
                return false;

            if (!Position.Point.IsInLine(bomb.Position.Point))
                return false;

            var betweenCells = Position.Point.GetCellsOnLineBetween(bomb.Position.Point)
                .Select(x => x.CellId)
                .ToArray();

            return Fight.GetAllFighters()
                .OfType<BombFighter>()
                .Where(x => x != null && x != this && x != bomb && x.IsAlive && !x.IsExploded && x.Summoner == Summoner && x.Element == Element && x.Position != null)
                .All(x => !betweenCells.Contains(x.Position.Cell));
        }

        public bool IsInExplosionZone(BombFighter bomb)
        {
            if (bomb == null || bomb == this || Position == null || bomb.Position == null)
                return false;

            return Position.Point.DistanceToCell(bomb.Position.Point) <= ExplosionZone;
        }

        public BombFighter[] GetBombsBoundedWith()
        {
            if (Fight == null || Summoner == null)
                return new[] { this };

            var availableBombs = Fight.GetAllFighters()
                .OfType<BombFighter>()
                .Where(x => x != null && x.IsAlive && !x.IsExploded && x.Summoner == Summoner)
                .ToArray();

            var result = new List<BombFighter>();
            var queue = new Queue<BombFighter>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || result.Contains(current))
                    continue;

                result.Add(current);

                foreach (var other in availableBombs)
                {
                    if (other == current || result.Contains(other))
                        continue;

                    if (current.IsBoundWith(other) || current.IsInExplosionZone(other))
                        queue.Enqueue(other);
                }
            }

            return result.ToArray();
        }

        private static Spell ResolveSpell(Dictionary<int, int> source, int element, sbyte fallbackLevel)
        {
            int spellId;
            if (!source.TryGetValue(element, out spellId))
                return null;

            var spellManager = SpellManager.Instance;
            if (spellManager != null && spellManager.Spells != null && spellManager.Spells.ContainsKey(spellId))
            {
                var levels = spellManager.Spells[spellId];
                if (levels != null && levels.Count > 0)
                {
                    int index = Math.Max(0, Math.Min(levels.Count - 1, fallbackLevel - 1));
                    return levels[index];
                }
            }

            return new Spell(spellId, fallbackLevel);
        }

        private static Spell ResolveExplosionSpell(int element, sbyte fallbackLevel)
        {
            return ResolveSpell(ExplosionSpellIds, element, fallbackLevel);
        }

        private static Spell ResolveExplosionDamageSpell(int element, sbyte fallbackLevel)
        {
            return ResolveSpell(ExplosionDamageSpellIds, element, fallbackLevel);
        }

        private static Spell ResolveWallSpell(int element, sbyte fallbackLevel)
        {
            return ResolveSpell(WallSpellIds, element, fallbackLevel);
        }

        private static Color ResolveWallColor(int element)
        {
            Color color;
            if (WallColors.TryGetValue(element, out color))
                return color;

            return WallColors[FireElement];
        }

        private void ApplyGrowthLook()
        {
            if (Look == null)
                return;

            int growthSteps = ComboTurns + _extraGrowthSteps;
            short scale = (short)Math.Min(100 + (growthSteps * 20), 260);
            if (Look.Scales.Count == 0)
            {
                Look.Scales.Add(scale);
                Look.Scales.Add(scale);
                return;
            }

            for (int i = 0; i < Look.Scales.Count; i++)
                Look.Scales[i] = scale;

            while (Look.Scales.Count < 2)
                Look.Scales.Add(scale);
        }

        public bool WasJustSummonedForSameAction(FightActor caster)
        {
            return caster == Summoner && (DateTime.UtcNow - SummonedAtUtc).TotalMilliseconds < 750;
        }

        private void TriggerExplosion(int? chainBonus = null)
        {
            if (IsExploded)
                return;

            IsExploded = true;
            BombManager.Instance.Explode(this, chainBonus);
        }

        public void Explode(FightActor source = null, int? chainBonus = null)
        {
            if (_deathHandled || IsExploded)
                return;

            TriggerExplosion(chainBonus);
            Die(source ?? Summoner ?? this);
        }

        private void DeleteWallBindings()
        {
            foreach (var binding in _wallBindings.ToArray())
                binding.Delete();

            _wallBindings.Clear();
        }

        private void CleanupFromFight(Fight fight)
        {
            if (fight == null)
                return;

            fight.TimeLine?.Fighters?.Remove(this);
            fight.TimeLine?.Leavers?.Remove(this);
            fight.Team?.Attackers?.Remove(this);
            fight.Team?.Defenders?.Remove(this);
            fight.RemoveTriggers(this);
            DeleteWallBindings();
        }

        public new void Die(FightActor byFighter = null)
        {
            if (_deathHandled)
                return;

            _deathHandled = true;
            ClearDelayedExplosion();
            IsExploded = true;
            Stats.Health.Taken = Stats.Health.TotalMax;

            var fight = Fight;
            var summoner = Summoner;
            var killer = byFighter ?? summoner ?? this;

            CleanupFromFight(fight);

            summoner?.RemoveSummon(this);

            base.OnDead(this, killer);

            if (fight != null)
                fight.TimeLine?.Leavers?.Remove(this);

            if (fight != null)
            {
                BombManager.Instance.CheckWalls(fight, summoner);
                ContextHandler.SendGameFightTurnListMessage(fight.Clients, fight);
            }
        }

        public override void OnDead(FightActor target, FightActor fighter)
        {
            if (target != this)
            {
                base.OnDead(target, fighter);
                return;
            }

            if (_deathHandled)
                return;

            Die(fighter ?? Summoner ?? this);
        }
        public void DestroySilently()
        {
            if (_deathHandled)
                return;

            _deathHandled = true;
            ClearDelayedExplosion();
            IsExploded = true;
            Stats.Health.Taken = Stats.Health.TotalMax;

            var fight = Fight;
            var summoner = Summoner;

            CleanupFromFight(fight);

            summoner?.RemoveSummon(this);

            if (fight != null)
            {
                BombManager.Instance.CheckWalls(fight, summoner);
                ContextHandler.SendGameFightTurnListMessage(fight.Clients, fight);
            }
        }
    }
}
