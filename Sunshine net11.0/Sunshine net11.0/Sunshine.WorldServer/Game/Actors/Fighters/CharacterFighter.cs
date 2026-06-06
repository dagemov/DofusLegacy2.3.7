using Sunshine.WorldServer.Game.Characters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Handlers.Context.Roleplay;
using Sunshine.WorldServer.Handlers.Context;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.WorldServer.Game.Items;
using Sunshine.MySql.Database.World.Items;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.MySql.Database.World.Spells;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Effects.Items;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.Protocol.Utils;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class CharacterFighter : FightActor
    {
        public Character Character { get; set; }

        private int _weaponUses;
        private bool _isUsingWeapon;
        private int _criticalWeaponBonus;

        public CharacterFighter(Character character)
        {
            Character = character;
            Visibility = GameActionFightInvisibilityStateEnum.VISIBLE;
            SpellHistory = new SpellHistory(this);
            Spells = character.Spells;
            FightLoot = new Fights.Results.FightLoot(this);
        }

        public WorldClient Client { get { return Character.Client; } }

        public override Fight Fight { get { return Character.Fight; } }

        public override int Id { get { return Character.Id; } set { this.Id = value; } }

        public string Name { get { return Character.Name; } }

        public override short Level { get { return Character.Level; } }

        public override ActorLook Look { get { return Character.Look; } set { Character.Look = value; } }

        public override FightTeam Team { get { return Fight.Team; } }

        public override bool IsAttacker() { return Fight != null ? Fight.Team.Attackers.Contains(this) : false; }

        public override StatsFields Stats { get { return Character.Stats; } set { this.Stats = value; } }

        public override SpellHistory SpellHistory { get; set; }

        public override GameActionFightInvisibilityStateEnum Visibility { get; set; }

        public void SetReadyForNextTurn()
        {
            Fight?.Checker?.ToggleReady(this);
        }

        public bool IsSlaveTurn()
        {
            var slave = Fight?.FighterPlaying as SlaveFighter;
            return slave != null && slave.IsControlledBySummoner() && slave.Summoner == this;
        }

        public SlaveFighter GetSlave()
        {
            var slave = Fight?.FighterPlaying as SlaveFighter;
            return slave != null && slave.Summoner == this ? slave : null;
        }

        public FightActor GetCurrentPlayableFighter()
        {
            return IsSlaveTurn() ? (FightActor)GetSlave() : this;
        }

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        { 
           return new GameFightCharacterInformations(Id, Character.GetDisplayedEntityLook(), GetEntityDispositionInformations(client), IsAttacker() ? (sbyte) TeamEnum.TEAM_CHALLENGER : (sbyte) TeamEnum.TEAM_DEFENDER, IsAlive,
                GetGameFightMinimalStats(client), Name, Level, Character.GetActorAlignmentInformations());
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            return new GameFightCharacterInformations(Id, Character.GetDisplayedEntityLook(), GetEntityDispositionInformations(client), IsAttacker() ? (sbyte)TeamEnum.TEAM_CHALLENGER : (sbyte)TeamEnum.TEAM_DEFENDER, IsAlive,
                GetGameFightMinimalStatsPreparation(client), Name, Level, Character.GetActorAlignmentInformations());
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberCharacterInformations(Id, Character.Name, Character.Level);
        }

        public int GetCloseCombatWeaponGenericId()
        {
            BasePlayerItem weaponItem;
            WeaponTemplate weaponTemplate;
            return TryGetEquippedWeapon(out weaponItem, out weaponTemplate) && weaponTemplate != null ? weaponTemplate.Id : 0;
        }

        public SpellCastResult CanCastCloseCombat(short cell)
        {
            if (!IsFighterTurn() || IsDead())
                return SpellCastResult.CANNOT_PLAY;

            if (Position == null)
                return SpellCastResult.NOT_IN_ZONE;

            if (HasState(SpellStatesEnum.Weakened))
                return SpellCastResult.STATE_FORBIDDEN;

            BasePlayerItem weaponItem;
            WeaponTemplate weaponTemplate;
            TryGetEquippedWeapon(out weaponItem, out weaponTemplate);

            short apCost = (short)(weaponTemplate != null ? weaponTemplate.ApCost : 4);
            if (Stats.AP.Total < apCost)
                return SpellCastResult.NOT_ENOUGH_AP;

            var targetPoint = MapPoint.GetPoint(cell);
            if (targetPoint == null)
                return SpellCastResult.NOT_IN_ZONE;

            var distance = Position.Point.DistanceToCell(targetPoint);
            int minRange = weaponTemplate != null ? weaponTemplate.MinRange : 1;
            int maxRange = weaponTemplate != null ? weaponTemplate.Range : 1;

            if (distance < minRange || distance > maxRange)
                return SpellCastResult.NOT_IN_ZONE;

            if (weaponTemplate != null && weaponTemplate.CastInLine && !Position.Point.IsInLine(targetPoint))
                return SpellCastResult.NOT_IN_ZONE;

            if (weaponTemplate == null && Fight != null)
            {
                var target = Fight.GetOneFighter(cell);
                if (target == null || !target.IsAlive || target == this)
                    return SpellCastResult.CELL_NOT_FREE;
            }

            return SpellCastResult.OK;
        }

        public void CastCloseCombat(short cell)
        {
            var result = CanCastCloseCombat(cell);
            if (result != SpellCastResult.OK || Fight == null)
                return;

            BasePlayerItem weaponItem;
            WeaponTemplate weaponTemplate;
            TryGetEquippedWeapon(out weaponItem, out weaponTemplate);

            var apCost = (short)(weaponTemplate != null ? weaponTemplate.ApCost : 4);
            var weaponSpell = CreateCloseCombatSpell(weaponTemplate);
            var critical = RollWeaponCritical(weaponTemplate);
            var currentFight = Fight;

            currentFight.StartSequence(SequenceTypeEnum.SEQUENCE_WEAPON);

            bool silentCast = false;
            _isUsingWeapon = true;
            _criticalWeaponBonus = critical == FightSpellCastCriticalEnum.CRITICAL_HIT && weaponTemplate != null
                ? weaponTemplate.CriticalHitBonus
                : 0;

            try
            {
                currentFight.OnSpellCasted(this, weaponSpell, cell, critical, silentCast);
                UseAP(apCost);

                if (critical != FightSpellCastCriticalEnum.CRITICAL_FAIL)
                {
                    foreach (var effect in GetCloseCombatEffects(weaponItem, weaponTemplate))
                        EffectDispatcher.Dispatch(this, weaponSpell, effect, cell);
                }
            }
            finally
            {
                _isUsingWeapon = false;
                _criticalWeaponBonus = 0;
            }

            if (Fight != currentFight || currentFight.State != FightStateEnum.Fighting)
                return;

            currentFight.EndSequence(SequenceTypeEnum.SEQUENCE_WEAPON, ActionsEnum.ACTION_FIGHT_CLOSE_COMBAT);
            RefreshControllingClientFightState();
            currentFight.CheckFightEnd();
        }

        protected override int CalculateDamage(FightActor caster, int damage, EffectSchoolEnum type)
        {
            if (_isUsingWeapon)
                damage += Math.Max(0, _criticalWeaponBonus + Stats[StatsEnum.WeaponDamageBonus].TotalMax);

            return base.CalculateDamage(caster, damage, type);
        }

        private bool TryGetEquippedWeapon(out BasePlayerItem weaponItem, out WeaponTemplate weaponTemplate)
        {
            weaponItem = Character != null && Character.Inventory != null
                ? Character.Inventory.GetItem(CharacterInventoryPositionEnum.ACCESSORY_POSITION_WEAPON)
                : null;

            weaponTemplate = weaponItem != null ? weaponItem.Template as WeaponTemplate : null;
            return weaponTemplate != null;
        }

        private Spell CreateCloseCombatSpell(WeaponTemplate weaponTemplate)
        {
            return new Spell(0, 1)
            {
                Template = new SpellTemplate
                {
                    SpellId = 0,
                    ApCost = weaponTemplate != null ? weaponTemplate.ApCost : 4,
                    MinRange = weaponTemplate != null ? weaponTemplate.MinRange : 1,
                    Range = weaponTemplate != null ? weaponTemplate.Range : 1,
                    CastInLine = weaponTemplate != null && weaponTemplate.CastInLine,
                    CastInDiagonal = weaponTemplate != null && weaponTemplate.CastInDiagonal,
                    CastTestLos = weaponTemplate != null && weaponTemplate.CastTestLos,
                    NeedTakenCell = false,
                    NeedFreeCell = false,
                    NeedFreeTrapCell = false,
                    CriticalFailureProbability = weaponTemplate != null ? weaponTemplate.CriticalFailureProbability : 0,
                    CriticalHitProbability = weaponTemplate != null ? weaponTemplate.CriticalHitProbability : 0,
                    HideEffects = false
                },
                Effects = new List<Effect>(),
                CriticalEffects = new List<Effect>(),
                StatesForbidden = new short[0],
                StatesRequired = new short[0]
            };
        }

        private IEnumerable<Effect> GetCloseCombatEffects(BasePlayerItem weaponItem, WeaponTemplate weaponTemplate)
        {
            var primaryEffects = weaponItem != null && weaponItem.Effects != null && weaponItem.Effects.Count > 0
                ? weaponItem.Effects
                : null;

            var secondaryEffects = weaponTemplate != null ? weaponTemplate.EffectsBase : null;

            bool yieldedEffect = false;
            foreach (var effect in EnumerateAllowedCloseCombatEffects(primaryEffects, weaponTemplate))
            {
                yieldedEffect = true;
                yield return effect;
            }

            if (!yieldedEffect && secondaryEffects != null && !ReferenceEquals(primaryEffects, secondaryEffects))
            {
                foreach (var effect in EnumerateAllowedCloseCombatEffects(secondaryEffects, weaponTemplate))
                {
                    yieldedEffect = true;
                    yield return effect;
                }
            }

            if (!yieldedEffect)
                yield return new Effect(EffectsEnum.Effect_DamageNeutral, 1, 5, 0, 0, 0, SpellTargetType.ENEMY_ALL, SpellShapeEnum.P, 0, 0);
        }

        private IEnumerable<Effect> EnumerateAllowedCloseCombatEffects(IEnumerable<Effect> effects, WeaponTemplate weaponTemplate)
        {
            if (effects == null)
                yield break;

            foreach (var sourceEffect in effects.Where(x => x != null && EffectManager.Instance.SpellEffects.ContainsKey(x.Id)))
            {
                if (!IsAllowedCloseCombatEffect(sourceEffect))
                    continue;

                var effect = sourceEffect.Clone();
                if (effect.Target == 0)
                    effect.Target = SpellTargetType.ENEMY_ALL;

                ApplyCloseCombatWeaponZone(effect, weaponTemplate);

                yield return effect;
            }
        }

        private static void ApplyCloseCombatWeaponZone(Effect effect, WeaponTemplate weaponTemplate)
        {
            if (effect == null)
                return;

            if (weaponTemplate != null)
            {
                switch ((ItemTypeEnum)weaponTemplate.TypeId)
                {
                    case ItemTypeEnum.MARTEAU:
                        // Dofus close-combat hammer damage is applied on the target cell plus the four adjacent cells.
                        effect.ZoneShape = SpellShapeEnum.C;
                        effect.ZoneSize = 1;
                        effect.ZoneMinSize = 0;
                        return;

                    case ItemTypeEnum.BATON:
                        // Staff damage is applied on a 3-cell perpendicular line centered on the targeted cell.
                        effect.ZoneShape = SpellShapeEnum.T;
                        effect.ZoneSize = 1;
                        effect.ZoneMinSize = 0;
                        return;
                }
            }

            if (effect.ZoneShape == 0)
            {
                effect.ZoneShape = SpellShapeEnum.P;
                effect.ZoneSize = 0;
                effect.ZoneMinSize = 0;
            }
        }

        private static bool IsAllowedCloseCombatEffect(Effect effect)
        {
            if (effect == null)
                return false;

            // Weapon item lines often contain equip stats (+vita, +PA, +agi, etc.).
            // Those must never be replayed as combat effects on the target or they appear in the timeline as buffs/debuffs.
            if (ItemEffectHandler.TryGetRelatedStat(effect.Id, out _) || ItemEffectHandler.IsNegativeEffectForStats(effect.Id))
                return false;

            // Keep close-combat effects instantaneous only. Duration-based effects from the item are equipment stats,
            // not on-hit spell effects.
            if (effect.Duration > 0)
                return false;

            return true;
        }

        private FightSpellCastCriticalEnum RollWeaponCritical(WeaponTemplate weaponTemplate)
        {
            if (weaponTemplate == null)
                return FightSpellCastCriticalEnum.NORMAL;

            var random = new AsyncRandom();
            if (weaponTemplate.CriticalFailureProbability > 0 && random.Next((int)weaponTemplate.CriticalFailureProbability) == 0)
                return FightSpellCastCriticalEnum.CRITICAL_FAIL;

            if (weaponTemplate.CriticalHitProbability > 0 && random.Next((int)CalculateWeaponCriticRate(weaponTemplate.CriticalHitProbability)) == 0)
                return FightSpellCastCriticalEnum.CRITICAL_HIT;

            return FightSpellCastCriticalEnum.NORMAL;
        }

        private double CalculateWeaponCriticRate(double baseRate)
        {
            double num = Math.Floor((baseRate - (double)Stats[StatsEnum.CriticalHit].TotalMax) * 2.99011001130495 / Math.Log((double)(Stats[StatsEnum.Agility].TotalMax + 12), 2.7182818284590451));
            return num > 2.0 ? num : 2.0;
        }
    }
}
