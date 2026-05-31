using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.AI
{
    public abstract class AITypeHandler
    {
        public void Initialize(Monster source, FightActor fighter)
        {
            Source = source;
            Fighter = fighter;
            Spells = source.Spells;
            this.Play();
        }

        public void Initialize(TaxCollector source, FightActor fighter)
        {
            Fighter = fighter;
            Spells = source.Spells;
            this.Play();
        }

        public Monster Source { get; private set; }

        public FightActor Fighter { get; private set; }

        public Fight Fight { get { return Fighter?.Fight; } }

        public List<Spell> Spells { get; private set; }

        public abstract void Play();

        #region functions

        public bool CanRox()
        {
            return GetSpellsRox().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool CanHeal()
        {
            return GetSpellsHeal().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool CanTeleport()
        {
            return GetSpellsTeleport().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool CanPull()
        {
            return GetSpellsPull().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool CanSummon()
        {
            return GetSpellsInvoc().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool CanBoost()
        {
            return GetSpellsBoost().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool CanDebuffStats()
        {
            return GetSpellsDebuffStats().Count() > 0 && Fighter.Stats.AP.TotalMax > 0;
        }

        public bool NeedHeal()
        {
            return Fighter.Stats.Health.Total < Fighter.Stats.Health.TotalMax;
        }

        public IEnumerable<FightActor> GetAllCharacterFighters(int MP, bool isFriendly = false)
        {
            return Fight.GetAllFighters<CharacterFighter>(x => x.IsAlive && isFriendly ? x.IsFriendlyWith(Fighter) : x.IsEnnemyWith(Fighter) && x.Position.Point.DistanceToCell(Fighter.Position.Point) <= MP);
        }
            
        public IEnumerable<FightActor> GetAllFightersFarAway(int MP, bool isFriendly = false)
        {
            return Fight.GetAllFighters(x => x.IsAlive && isFriendly ? x.IsFriendlyWith(Fighter) : x.IsEnnemyWith(Fighter) && x.Position.Point.DistanceToCell(Fighter.Position.Point) > MP);
        }

        public IEnumerable<FightActor> GetAllFightersAdjacent(int MP, bool isFriendly = false)
        {
            return Fight.GetAllFighters(x => x.IsAlive && isFriendly ? x.IsFriendlyWith(Fighter) : x.IsEnnemyWith(Fighter) && x.Position.Point.DistanceToCell(Fighter.Position.Point) <= MP);
        }

        public IEnumerable<FightActor> GetAllFighters(int MP, bool isAdjacent = false)
        {
            return isAdjacent ? Fight.GetAllFighters(x => x.IsAlive && x.Position.Point.DistanceToCell(Fighter.Position.Point) <= MP) : Fight.GetAllFighters(x => x.IsAlive && x.Position.Point.DistanceToCell(Fighter.Position.Point) > MP);
        }

        public FightActor GetFighterAdjacent(IEnumerable<FightActor> fighters, int MP, bool isFriendly = false)
        {
            return fighters.FirstOrDefault(x => x.IsAlive && isFriendly ? x.IsFriendlyWith(Fighter) : x.IsEnnemyWith(Fighter) && x.Position.Point.DistanceToCell(Fighter.Position.Point) <= MP);
        }

        public FightActor GetFightersAdjacent(int MP, bool isFriendly = false)
        {
            return Fight.GetAllFighters(x => x.IsAlive && isFriendly ? x.IsFriendlyWith(Fighter) : x.IsEnnemyWith(Fighter) && x.Position.Point.DistanceToCell(Fighter.Position.Point) <= MP).FirstOrDefault();
        }

        public FightActor GetFighterFarAway(IEnumerable<FightActor> fighters, int MP, out int movementPoints, bool isFriendly = false)
        {
            var fighter = Fight.GetAllFighters(x => x.IsAlive && isFriendly ? x.IsFriendlyWith(Fighter) : x.IsEnnemyWith(Fighter) && x.Position.Point.DistanceToCell(Fighter.Position.Point) > MP).FirstOrDefault();
            movementPoints = (int)fighter?.Position.Point.DistanceToCell(Fighter.Position.Point);
            return fighter;
        }

        public FightActor GetFirstFriendNeedHeal(IEnumerable<FightActor> friends)
        {
            if (friends == null || friends.Count() <= 0)
                return null;

            FightActor _friend = null;
            FightActor _summon = null;

            if (friends.Count() >= 1)
            {
                _friend = friends.Where(x => !(x != null && x is ISummoned))?.FirstOrDefault();
                var summons = friends.Where(x => (x != null && x is ISummoned));
                _summon = summons.Count() > 0 ? summons.FirstOrDefault() : null;
            }

            foreach (var friend in friends)
            {
                int friendHealth1 = friend.Stats.Health.TotalMax - friend.Stats.Health.Total;
                int friendHealth2 = _friend.Stats.Health.TotalMax - _friend.Stats.Health.Total;

                if (friendHealth2 <= friendHealth1 && !(friend is ISummoned) && !friend.HasState(SpellStatesEnum.Unhealable))
                    _friend = friend;

                if (_summon != null && (friendHealth1 <= (_summon.Stats.Health.TotalMax - _summon.Stats.Health.Total)
                       && (friend is ISummoned)) && !friend.HasState(SpellStatesEnum.Unhealable))
                    _summon = friend;
            }

            return _friend.Stats.Health.Total == _friend.Stats.Health.TotalMax ? _summon : _friend;
        }

        public FightActor GetFirstFighterWithLessStats(IEnumerable<FightActor> fighters, StatsEnum stats)
        {
            if (fighters == null || fighters.Count() <= 0)
                return null;

            FightActor _fighter = null;

            if (fighters.Count() >= 1)
                _fighter = fighters.FirstOrDefault();

            foreach (var fighter in fighters)
            {
                if (_fighter != null && fighter.Stats[stats].TotalMax <= _fighter.Stats[stats].TotalMax && !(fighter is ISummoned))
                    _fighter = fighter;
            }

            return _fighter;
        }

        public FightActor GetFirstFighterWithLessStats(IEnumerable<FightActor> fighters, List<StatsEnum> stats, out StatsEnum statSpell)
        {
            if (fighters == null || fighters.Count() <= 0)
            {
                statSpell = stats[0];
                return null;
            }

            FightActor _fighter = null;

            StatsEnum stat = stats[0];

            if (fighters.Count() >= 1)
                _fighter = fighters.FirstOrDefault();

            foreach (var fighter in fighters)
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    bool statFighter = fighter.Stats[stats[i]].TotalMax <= _fighter?.Stats[stats[i]].TotalMax;
                    bool stat2Fighter = fighter.Stats[stats[i >= 1 ? i - 1 : 0]].TotalMax <= _fighter?.Stats[stats[i >= 1 ? i - 1 : 0]].TotalMax;
                    if (statFighter || stat2Fighter)
                    {
                        stat = statFighter ? stats[i] : stat2Fighter ? stats[i >= 1 ? i - 1 : 0] : stats[0];
                        _fighter = fighter;
                    }
                }
            }

            statSpell = stat;
            return _fighter;
        }

        public FightActor GetFirstFighterWithLessHealth(IEnumerable<FightActor> fighters)
        {
            if (fighters == null || fighters.Count() <= 0)
                return null;

            FightActor _fighter = null;

            if (fighters.Count() >= 1)
                _fighter = fighters.FirstOrDefault();

            foreach (var fighter in fighters)
            {
                if (_fighter != null && fighter.Stats.Health.Total <= _fighter.Stats.Health.Total)
                    _fighter = fighter;
            }

            return _fighter;
        }

        public FightActor GetFirstFighterWithMoreLevel(IEnumerable<FightActor> fighters)
        {
            if (fighters == null || fighters.Count() <= 0)
                return null;

            FightActor _fighter = null;

            if (fighters.Count() >= 1)
                _fighter = fighters.FirstOrDefault();

            foreach (var fighter in fighters)
            {
                if (_fighter != null && fighter.Level <= _fighter.Level)
                    _fighter = fighter;
            }

            return _fighter;
        }

        public FightActor GetFirstFighterWithLessHealth(IEnumerable<FightActor> fighters, SpellStatesEnum state)
        {
            if (fighters == null || fighters.Count() <= 0)
                return null;

            FightActor _fighter = null;

            if (fighters.Count() >= 1)
                _fighter = fighters.FirstOrDefault();

            foreach (var fighter in fighters)
            {
                if (_fighter != null && fighter.Stats.Health.Total <= _fighter.Stats.Health.Total && !fighter.HasState(state))
                    _fighter = fighter;
            }

            return _fighter;
        }

        public List<StatsEnum> GetSpellResistances(IEnumerable<Spell> spells)
        {
            List<StatsEnum> stats = new List<StatsEnum>();

            foreach (var spell in spells)
            {
                foreach (var effect in spell.Effects)
                {
                    switch (effect.Id)
                    {
                        case EffectsEnum.Effect_DamageAir:
                        case EffectsEnum.Effect_StealHPAir:
                        case EffectsEnum.Effect_DamageAirPerAP:
                        case EffectsEnum.Effect_DamageAirPerMP:
                            if (!stats.Contains(StatsEnum.AirResistPercent))
                                stats.Add(StatsEnum.AirResistPercent);
                            break;

                        case EffectsEnum.Effect_DamageEarth:
                        case EffectsEnum.Effect_StealHPEarth:
                        case EffectsEnum.Effect_DamageEarthPerAP:
                        case EffectsEnum.Effect_DamageEarthPerMP:
                            if (!stats.Contains(StatsEnum.EarthResistPercent))
                                stats.Add(StatsEnum.EarthResistPercent);
                            break;

                        case EffectsEnum.Effect_DamageFire:
                        case EffectsEnum.Effect_StealHPFire:
                        case EffectsEnum.Effect_DamageFirePerAP:
                        case EffectsEnum.Effect_DamageFirePerMP:
                            if (!stats.Contains(StatsEnum.FireResistPercent))
                                stats.Add(StatsEnum.FireResistPercent);
                            break;

                        case EffectsEnum.Effect_DamageWater:
                        case EffectsEnum.Effect_StealHPWater:
                        case EffectsEnum.Effect_DamageWaterPerAP:
                        case EffectsEnum.Effect_DamageWaterPerMP:
                            if (!stats.Contains(StatsEnum.WaterResistPercent))
                                stats.Add(StatsEnum.WaterResistPercent);
                            break;

                        default:
                            if (!stats.Contains(StatsEnum.NeutralResistPercent))
                                stats.Add(StatsEnum.NeutralResistPercent);
                            break;
                    }
                }
            }
            return stats;
        }

        public Spell GetFirstSpellRox(short targetedCell, int ApCost, StatsEnum stat)
        {
            var spells = GetSpellsRox().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            var effects = GetEffects(stat);
            var spell = spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost && y.Effects.Any(z => effects.Contains(z.Id)));

            return spell == null ? GetFirstSpellRox(targetedCell, ApCost) : spell;
        }

        public Spell GetFirstSpellRox(short targetedCell, int ApCost)
        {
            var spells = GetSpellsRox().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public Spell GetFirstSpellInvoc(short targetedCell, int ApCost)
        {
            var spells = GetSpellsInvoc().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public Spell GetFirstSpellBoost(short targetedCell, int ApCost)
        {
            var spells = GetSpellsBoost().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public Spell GetFirstSpellHeal(short targetedCell, int ApCost)
        {
            var spells = GetSpellsHeal().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public Spell GetFirstSpellTeleport(short targetedCell, int ApCost)
        {
            var spells = GetSpellsTeleport().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public Spell GetFirstSpellPull(short targetedCell, int ApCost)
        {
            var spells = GetSpellsPull().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public Spell GetFirstSpellStatsDebuff(short targetedCell, int ApCost)
        {
            var spells = GetSpellsDebuffStats().Where(x => Fighter.SpellHistory.CanCastSpell(x, targetedCell));
            return spells.OrderByDescending(x => x.Template.ApCost).FirstOrDefault(y => y.Template.ApCost <= ApCost);
        }

        public IEnumerable<Spell> GetSpellsRox()
        {
            return Spells.Where(x => IsRoxEffects(x.Effects));
        }

        public IEnumerable<Spell> GetSpellsRange(int distance)
        {
            return Spells.Where(x => x.Template.MinRange >= 1 && x.Template.Range >= distance);
        }

        public IEnumerable<Spell> GetSpellsInvoc()
        {
            return Spells.Where(x => x.Effects.Any(y => y.Id == EffectsEnum.Effect_Summon));
        }

        public IEnumerable<Spell> GetSpellsBoost()
        {
            return Spells.Where(x => IsBoostEffects(x.Effects));
        }

        public IEnumerable<Spell> GetSpellsHeal()
        {
            return Spells.Where(x => IsHealEffects(x.Effects));
        }

        public IEnumerable<Spell> GetSpellsTeleport()
        {
            return Spells.Where(x => x.Effects.Any(y => y.Id == EffectsEnum.Effect_Teleport));
        }

        public IEnumerable<Spell> GetSpellsPull()
        {
            return Spells.Where(x => x.Effects.Any(y => y.Id == EffectsEnum.Effect_PullForward));
        }

        public IEnumerable<Spell> GetSpellsDebuffStats()
        {
            return Spells.Where(x => IsStatsDebuff(x.Effects));
        }

        private bool IsStatsDebuff(List<Effect> effects)
        {
            return effects.FirstOrDefault(x => IsStatsDebuff(x.Id)) != null;
        }

        private bool IsStatsDebuff(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_SubAP:
                case EffectsEnum.Effect_SubAP_1079:
                case EffectsEnum.Effect_StealAP_84:
                case EffectsEnum.Effect_StealAP_440:
                case EffectsEnum.Effect_RemoveAP:
                case EffectsEnum.Effect_LosingAP:
                case EffectsEnum.Effect_LosingMP:
                case EffectsEnum.Effect_LostMP:
                case EffectsEnum.Effect_StealMP_441:
                case EffectsEnum.Effect_StealMP_77:
                case EffectsEnum.Effect_SubMP:
                case EffectsEnum.Effect_SubMP_1080:
                case EffectsEnum.Effect_SubMPFix:
                case EffectsEnum.Effect_ReduceEffectsDuration:
                case EffectsEnum.Effect_Debuff:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsHealEffects(List<Effect> effects)
        {
            return effects.FirstOrDefault(x => IsHealEffect(x.Id)) != null;
        }

        private bool IsHealEffect(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_HealHP_108:
                case EffectsEnum.Effect_HealHP_143:
                case EffectsEnum.Effect_HealHP_81:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsSummonEffects(List<Effect> effects)
        {
            return effects.FirstOrDefault(x => IsSummonEffect(x.Id)) != null;
        }

        private bool IsSummonEffect(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_Summon:
                case EffectsEnum.Effect_SummonBomb:
                case EffectsEnum.Effect_SummonOsa:
                case EffectsEnum.Effect_SummonSlave:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsBoostEffects(List<Effect> effects)
        {
            return effects.FirstOrDefault(x => IsBoostEffects(x.Id)) != null;
        }

        private bool IsBoostEffects(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_AddAgility:
                case EffectsEnum.Effect_AddAirDamageBonus:
                case EffectsEnum.Effect_AddChance:
                case EffectsEnum.Effect_AddWaterDamageBonus:
                case EffectsEnum.Effect_AddDamageBonus:
                case EffectsEnum.Effect_AddVitality:
                case EffectsEnum.Effect_AddRange:
                case EffectsEnum.Effect_AddRange_136:
                case EffectsEnum.Effect_AddDamageBonusPercent:
                case EffectsEnum.Effect_AddFireDamageBonus:
                case EffectsEnum.Effect_AddEarthDamageBonus:
                case EffectsEnum.Effect_AddNeutralDamageBonus:
                case EffectsEnum.Effect_AddAP_111:
                case EffectsEnum.Effect_RegainAP:
                case EffectsEnum.Effect_AddMP:
                case EffectsEnum.Effect_AddMP_128:
                case EffectsEnum.Effect_AddLock:
                case EffectsEnum.Effect_AddDodge:
                case EffectsEnum.Effect_IncreaseAPAvoid:
                case EffectsEnum.Effect_IncreaseMPAvoid:
                case EffectsEnum.Effect_AddArmorDamageReduction:
                case EffectsEnum.Effect_AddGlobalDamageReduction_105:
                case EffectsEnum.Effect_AddDamageReflection:
                case EffectsEnum.Effect_AddGlobalDamageReduction:
                case EffectsEnum.Effect_AddEarthResistPercent:
                case EffectsEnum.Effect_AddEarthElementReduction:
                case EffectsEnum.Effect_AddWaterResistPercent:
                case EffectsEnum.Effect_AddWaterElementReduction:
                case EffectsEnum.Effect_AddFireResistPercent:
                case EffectsEnum.Effect_AddFireElementReduction:
                case EffectsEnum.Effect_AddAirResistPercent:
                case EffectsEnum.Effect_AddAirElementReduction:
                case EffectsEnum.Effect_AddNeutralResistPercent:
                case EffectsEnum.Effect_AddNeutralElementReduction:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsRoxEffects(List<Effect> effects)
        {
            return effects.FirstOrDefault(x => IsRoxEffect(x.Id)) != null;
        }

        private bool IsRoxEffect(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_DamageAir:
                case EffectsEnum.Effect_DamageNeutral:
                case EffectsEnum.Effect_DamageNeutralPerAP:
                case EffectsEnum.Effect_DamageNeutralPerMP:
                case EffectsEnum.Effect_StealHPAir:
                case EffectsEnum.Effect_DamageAirPerAP:
                case EffectsEnum.Effect_DamageAirPerMP:
                case EffectsEnum.Effect_DamageEarth:
                case EffectsEnum.Effect_StealHPEarth:
                case EffectsEnum.Effect_DamageEarthPerAP:
                case EffectsEnum.Effect_DamageEarthPerMP:
                case EffectsEnum.Effect_DamageFire:
                case EffectsEnum.Effect_StealHPFire:
                case EffectsEnum.Effect_DamageFirePerAP:
                case EffectsEnum.Effect_DamageFirePerMP:
                case EffectsEnum.Effect_DamageWater:
                case EffectsEnum.Effect_StealHPWater:
                case EffectsEnum.Effect_DamageWaterPerAP:
                case EffectsEnum.Effect_DamageWaterPerMP:
                    return true;
                default:
                    return false;
            }
        }

        private List<EffectsEnum> GetEffects(StatsEnum stat)
        {
            List<EffectsEnum> effects = new List<EffectsEnum>();
            switch (stat)
            {
                case StatsEnum.AirResistPercent:
                    effects.Add(EffectsEnum.Effect_DamageAir);
                    effects.Add(EffectsEnum.Effect_StealHPAir);
                    effects.Add(EffectsEnum.Effect_DamageAirPerAP);
                    effects.Add(EffectsEnum.Effect_DamageAirPerMP);
                    break;

                case StatsEnum.EarthResistPercent:
                    effects.Add(EffectsEnum.Effect_DamageEarth);
                    effects.Add(EffectsEnum.Effect_StealHPEarth);
                    effects.Add(EffectsEnum.Effect_DamageEarthPerAP);
                    effects.Add(EffectsEnum.Effect_DamageEarthPerMP);
                    break;

                case StatsEnum.FireResistPercent:
                    effects.Add(EffectsEnum.Effect_DamageFire);
                    effects.Add(EffectsEnum.Effect_StealHPFire);
                    effects.Add(EffectsEnum.Effect_DamageFirePerAP);
                    effects.Add(EffectsEnum.Effect_DamageFirePerMP);
                    break;

                case StatsEnum.WaterResistPercent:
                    effects.Add(EffectsEnum.Effect_DamageWater);
                    effects.Add(EffectsEnum.Effect_StealHPWater);
                    effects.Add(EffectsEnum.Effect_DamageWaterPerAP);
                    effects.Add(EffectsEnum.Effect_DamageWaterPerMP);
                    break;

                default:
                    effects.Add(EffectsEnum.Effect_DamageNeutral);
                    effects.Add(EffectsEnum.Effect_StealHPNeutral);
                    effects.Add(EffectsEnum.Effect_DamageNeutralPerAP);
                    effects.Add(EffectsEnum.Effect_DamageNeutralPerMP);
                    break;
            }
            return effects;
        }
        #endregion

        public void Move(short startCell, short endCell, bool cantWalk = false, bool isDiagonal = false)
        {
            if (cantWalk)
                return;

            Pathfinder pathFinder = new Pathfinder(Fight.Map.CellsInfoProvider);

            if (pathFinder == null)
                return;

            Path path = pathFinder.FindPath(startCell, endCell, isDiagonal);

            if (path == null)
                return;

            Fighter.StartMove(path);
        }

        public void Move(FightActor target, Spell spell)
        {
            if (Fighter.Stats.MP.TotalMax <= 0 || target == null)
                return;

            this.Move(Fighter.Position.Cell, target.Position.Cell, false, spell.Template.CastInDiagonal);
        }

        public void ExecuteTeleport(IEnumerable<FightActor> fighters, FightActor target = null)
        {
            AsyncRandom rdn = new AsyncRandom();

            MapPoint[] points = null;

            MapPoint point = null;

            Spell spellTp = null;

            if (Fighter.HasState(SpellStatesEnum.Gravity))
                return;

            if (target != null)
            {
                if (target.Position.Point.DistanceToCell(Fighter.Position.Point) <= 1)
                    return;

                points = target.Position.Point.GetAdjacentCells((short entry) => Fight.IsCellFree(entry)).ToArray<MapPoint>();

                if (points.Length <= 0)
                    return;

                point = points[rdn.Next(0, points.Length - 1)];

                spellTp = GetFirstSpellTeleport(point.CellId, Fighter.Stats.AP.TotalMax);

                if (spellTp == null)
                    return;

                Fighter.CastSpell(spellTp, point.CellId);
            }
            else
            {             
                IEnumerable<FightActor> fightersFarAway = null;

                target = GetFirstFighterWithLessHealth(fighters.Count() > 0 ? fighters : fightersFarAway, SpellStatesEnum.Gravity);

                if (target == null || target.Position.Point.DistanceToCell(Fighter.Position.Point) <= 1) // CAC
                    return;

                points = target.Position.Point.GetAdjacentCells((short entry) => Fight.IsCellFree(entry)).ToArray<MapPoint>();

                if (points.Length <= 0)
                    return;

                point = points[rdn.Next(0, points.Length - 1)];

                spellTp = GetFirstSpellTeleport(point.CellId, Fighter.Stats.AP.TotalMax);

                if (spellTp == null)
                    return;

                Fighter.CastSpell(spellTp, point.CellId);
            }
        }

        public void ExecutePull(IEnumerable<FightActor> fighters, FightActor target = null)
        {
            Spell spellPull = null;

            if (target != null)
            {
                spellPull = GetFirstSpellPull(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                if (spellPull == null)
                    return;

                if (Fighter.NeedMoveToCastSpell(spellPull, target.Position.Cell))
                    this.Move(target, spellPull);

                Fighter.CastSpell(spellPull, target.Position.Cell);
            }
            else
            {
                if (fighters.Count() <= 0)
                    return;

                target = GetFirstFighterWithLessHealth(fighters, SpellStatesEnum.Rooted);

                if (target == null || target.Position.Point.DistanceToCell(Fighter.Position.Point) <= 1) // CAC
                    return;

                spellPull = GetFirstSpellPull(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                if (spellPull == null)
                    return;

                if (Fighter.NeedMoveToCastSpell(spellPull, target.Position.Cell))
                    this.Move(target, spellPull);

                Fighter.CastSpell(spellPull, target.Position.Cell);
            }
        }

        public void ExecuteSummon()
        {
            MapPoint[] points = Fighter.Position.Point.GetAdjacentCells((short entry) => Fight.IsCellFree(entry)).ToArray<MapPoint>();

            if (points.Length <= 0)
                return;

            AsyncRandom rdn = new AsyncRandom();

            MapPoint point = points[rdn.Next(0, points.Length - 1)];

            var spellInvoc = GetFirstSpellInvoc(point.CellId, Fighter.Stats.AP.TotalMax);

            if (spellInvoc == null)
                return;

            Fighter.CastSpell(spellInvoc, point.CellId);

            return;
        }

        public void ExecuteBoost(IEnumerable<FightActor> fighters, FightActor target = null)
        {
            Spell spellBoost = null;

            if (Fighter is ISummoned)
            {
                target = (Fighter as ISummoned).Summoner;

                spellBoost = GetFirstSpellBoost(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                if (spellBoost == null)
                {
                    target = GetFirstFighterWithMoreLevel(GetAllCharacterFighters(Fighter.Stats.MP.TotalMax, true));

                    if (target == null)
                        return;

                    spellBoost = GetFirstSpellBoost(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                    if (spellBoost == null)
                        return;

                    if (Fighter.NeedMoveToCastSpell(spellBoost, target.Position.Cell))
                        this.Move(target, spellBoost);

                    Fighter.CastSpell(spellBoost, target.Position.Cell);
                }
                else
                {
                    if (Fighter.NeedMoveToCastSpell(spellBoost, target.Position.Cell))
                        this.Move(target, spellBoost);

                    Fighter.CastSpell(spellBoost, target.Position.Cell);
                }
            }
            else {

                if (target != null)
                {
                    spellBoost = GetFirstSpellBoost(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                    if (spellBoost == null)
                        return;

                    if (Fighter.NeedMoveToCastSpell(spellBoost, target.Position.Cell))
                        this.Move(target, spellBoost);

                    Fighter.CastSpell(spellBoost, target.Position.Cell);
                }
                else
                {
                    if (fighters.Count() > 0)
                    {
                        target = GetFirstFighterWithMoreLevel(fighters);

                        if (target == null)
                        {
                            spellBoost = GetFirstSpellBoost(Fighter.Position.Cell, Fighter.Stats.AP.TotalMax);

                            if (spellBoost == null)
                                return;

                            Fighter.CastSpell(spellBoost, Fighter.Position.Cell);
                        }
                        else
                        {
                            spellBoost = GetFirstSpellBoost(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                            if (spellBoost == null)
                            {
                                spellBoost = GetFirstSpellBoost(Fighter.Position.Cell, Fighter.Stats.AP.TotalMax);

                                if (spellBoost == null)
                                    return;

                                Fighter.CastSpell(spellBoost, Fighter.Position.Cell);
                            }
                            else
                            {
                                if (Fighter.NeedMoveToCastSpell(spellBoost, target.Position.Cell))
                                    this.Move(target, spellBoost);

                                Fighter.CastSpell(spellBoost, target.Position.Cell);
                            }
                        }
                    }
                    else
                    {
                        spellBoost = GetFirstSpellBoost(Fighter.Position.Cell, Fighter.Stats.AP.TotalMax);

                        if (spellBoost == null)
                            return;

                        Fighter.CastSpell(spellBoost, Fighter.Position.Cell);
                    }
                }
            }
        }

        public void ExecuteHeal(IEnumerable<FightActor> fighters, FightActor friend = null)
        {
            Spell spellHeal = null;

            if (Fighter is ISummoned)
            {
                friend = (Fighter as ISummoned).Summoner;

                spellHeal = GetFirstSpellHeal(friend.Position.Cell, Fighter.Stats.AP.TotalMax);

                if (spellHeal == null)
                {
                    friend = GetFirstFighterWithLessHealth(GetAllCharacterFighters(Fighter.Stats.MP.TotalMax, true));

                    if (friend == null)
                        return;

                    spellHeal = GetFirstSpellHeal(friend.Position.Cell, Fighter.Stats.AP.TotalMax);

                    if (spellHeal == null)
                        return;

                    if (Fighter.NeedMoveToCastSpell(spellHeal, friend.Position.Cell))
                        this.Move(friend, spellHeal);

                    Fighter.CastSpell(spellHeal, friend.Position.Cell);
                }

                else
                {
                    if (Fighter.NeedMoveToCastSpell(spellHeal, friend.Position.Cell))
                        this.Move(friend, spellHeal);

                    Fighter.CastSpell(spellHeal, friend.Position.Cell);
                }
            }
            else
            {

                if (friend != null && friend.Stats.Health.Total < friend.Stats.Health.TotalMax)
                {
                    spellHeal = GetFirstSpellHeal(friend.Position.Cell, Fighter.Stats.AP.TotalMax);

                    if (spellHeal == null)
                        return;

                    if (Fighter.NeedMoveToCastSpell(spellHeal, friend.Position.Cell))
                        this.Move(friend, spellHeal);

                    Fighter.CastSpell(spellHeal, friend.Position.Cell);
                }
                else
                {
                    if (fighters.Count() > 0)
                    {
                        friend = GetFirstFriendNeedHeal(fighters);

                        if (friend == null)
                        {
                            spellHeal = GetFirstSpellHeal(Fighter.Position.Cell, Fighter.Stats.AP.TotalMax);

                            if (spellHeal == null || !NeedHeal())
                                return;

                            Fighter.CastSpell(spellHeal, Fighter.Position.Cell);
                        }
                        else
                        {
                            spellHeal = GetFirstSpellHeal(friend.Position.Cell, Fighter.Stats.AP.TotalMax);

                            if (spellHeal == null)
                                return;

                            if (Fighter.NeedMoveToCastSpell(spellHeal, friend.Position.Cell))
                                this.Move(friend, spellHeal);

                            Fighter.CastSpell(spellHeal, friend.Position.Cell);
                        }
                    }
                    else
                    {
                        spellHeal = GetFirstSpellHeal(Fighter.Position.Cell, Fighter.Stats.AP.TotalMax);

                        if (spellHeal == null || !NeedHeal())
                            return;

                        Fighter.CastSpell(spellHeal, Fighter.Position.Cell);
                    }
                }
            }
        }

        public void ExecuteStatsDebuff(IEnumerable<FightActor> fighters, FightActor target = null)
        {
            Spell spellStatsDebuff = null;

            bool isAPDebuff = GetSpellsDebuffStats().Any(x => x.Effects.Any(y => y.Id == EffectsEnum.Effect_SubAP ||
                                                                y.Id == EffectsEnum.Effect_SubAP_1079 || y.Id == EffectsEnum.Effect_RemoveAP));

            if (target != null)
            {
                spellStatsDebuff = GetFirstSpellStatsDebuff(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                if (spellStatsDebuff == null)
                    return;

                if (Fighter.NeedMoveToCastSpell(spellStatsDebuff, target.Position.Cell))
                    this.Move(target, spellStatsDebuff);

                Fighter.CastSpell(spellStatsDebuff, target.Position.Cell);
            }
            else
            {
                if (fighters.Count() <= 0)
                    return;
             
                target = GetFirstFighterWithLessStats(fighters, isAPDebuff ? StatsEnum.DodgeAPProbability : StatsEnum.DodgeMPProbability);

                if (target == null)
                    return;

                spellStatsDebuff = GetFirstSpellStatsDebuff(target.Position.Cell, Fighter.Stats.AP.TotalMax);

                if (spellStatsDebuff == null)
                    return;

                if (Fighter.NeedMoveToCastSpell(spellStatsDebuff, target.Position.Cell))
                    this.Move(target, spellStatsDebuff);

                Fighter.CastSpell(spellStatsDebuff, target.Position.Cell);
            }
        }

        public void ExecuteRox(IEnumerable<FightActor> fighters, FightActor target = null)
        {
            Spell spellRox = null;

            if (target != null)
            {
                List<StatsEnum> stats = GetSpellResistances(GetSpellsRox());

                StatsEnum firstStat = stats[0];                

                spellRox = GetFirstSpellRox(target.Position.Cell, Fighter.Stats.AP.TotalMax, firstStat);

                if (spellRox == null)
                    return;
              
                if (Fighter.NeedMoveToCastSpell(spellRox, target.Position.Cell))
                    this.Move(target, spellRox);

                Fighter.CastSpell(spellRox, target.Position.Cell);
            }
            else
            {
                if (fighters.Count() < 0)
                    return;

                List<StatsEnum> stats = GetSpellResistances(GetSpellsRox());

                StatsEnum firstStat = stats[0];

                target = GetFirstFighterWithLessStats(fighters, stats, out firstStat);

                if (target == null)
                {
                    target = GetFirstFighterWithLessHealth(fighters);

                    if (target == null)
                        return;
                }

                spellRox = GetFirstSpellRox(target.Position.Cell, Fighter.Stats.AP.TotalMax, firstStat);

                if (spellRox == null)
                    return;

                if (Fighter.NeedMoveToCastSpell(spellRox, target.Position.Cell))
                    this.Move(target, spellRox);

                Fighter.CastSpell(spellRox, target.Position.Cell);
            }
        }
    }
}
