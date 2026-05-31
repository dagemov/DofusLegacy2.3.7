using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Stats
{
    public class StatsFields
    {   
        private Dictionary<StatsEnum, StatsData> _stats;

        public const int APLimit = 12;

        public const int MPLimit = 6;

        public const int RangeLimit = 6;

        public CharacterStatsRecord Record { get; set; }

        public StatsFields(object owner)
        {
            _stats = new Dictionary<StatsEnum, StatsData>();

            foreach (StatsEnum stats in typeof(StatsEnum).GetEnumValues())
            {
                switch (stats)
                {
                    case StatsEnum.Health:
                        _stats.Add(stats, new StatsHealth(owner, 0));
                        break;

                    case StatsEnum.AP:
                        _stats.Add(stats, new StatsAP(0));
                        break;

                    case StatsEnum.MP:
                        _stats.Add(stats, new StatsMP(0));
                        break;

                    default:
                        _stats.Add(stats, new StatsData(0, null));
                        break;
                }
            }

            BindHealthOwnerStats();

            switch (owner.GetType().Name)
            {
                case "Character":
                    Initialize((owner as Character));
                    break;

                case "Monster":
                    Initialize((owner as Monster));
                    break;

                case "TaxCollector":
                    Initialize((owner as TaxCollector));
                    break;
            }                
        }

        public StatsData this[StatsEnum stat]
        {
            get
            {
                if (_stats.ContainsKey(stat))
                    return _stats[stat];
                else
                    return null;
            }
        }

        public StatsAP AP
        {
            get { return (StatsAP)this[StatsEnum.AP]; }
        }

        public StatsMP MP
        {
            get { return (StatsMP)this[StatsEnum.MP]; }
        }

        public StatsHealth Health
        {
            get { return (StatsHealth)this[StatsEnum.Health]; }
        }

        public StatsData Shield
        {
            get { return this[StatsEnum.Shield]; }
        }

        public StatsData Prospecting
        {
            get { return this[StatsEnum.Prospecting]; }
        }

        public StatsData Initiative
        {
            get { return this[StatsEnum.Initiative]; }
        }

        public StatsData PushDamageBonus
        {
            get { return this[StatsEnum.PushDamageBonus]; }
        }

        public StatsData PushDamageBonusReduction
        {
            get { return this[StatsEnum.PushDamageReduction]; }
        }

        private void Initialize(Character character)
        {
            Record = CharacterManager.Instance.GetCharacterStats(character.Id);
            if (Record == null)
            {
                Record = new CharacterStatsRecord
                {
                    OwnerId = character.Id,
                    AP = 6,
                    MP = 3,
                    Health = Math.Max(55, 50 + (character.Level * 5)),
                    Vitality = 0,
                    Strength = 0,
                    Chance = 0,
                    Wisdom = 0,
                    Intelligence = 0,
                    Agility = 0,
                    DamageTaken = 0,
                };
            }

            this[StatsEnum.AP].Base = Record.AP;
            this[StatsEnum.MP].Base = Record.MP;
            this[StatsEnum.Vitality].Base = Record.Vitality;
            this[StatsEnum.Health].Base = Record.Health;
            (this[StatsEnum.Health] as StatsHealth).Taken = Record.DamageTaken;
            this[StatsEnum.Strength].Base = Record.Strength;
            this[StatsEnum.Chance].Base = Record.Chance;
            this[StatsEnum.Wisdom].Base = Record.Wisdom;
            this[StatsEnum.Intelligence].Base = Record.Intelligence;
            this[StatsEnum.Agility].Base = Record.Agility;
            this[StatsEnum.SummonLimit].Base = 1;
            this[StatsEnum.Initiative].Base = (short)GetInitiative(character.Level);
            this[StatsEnum.Prospecting].Base = (short)(100 + (this[StatsEnum.Chance].Total / 10));
            this[StatsEnum.DodgeAPProbability].Base = (short)(Record.Wisdom / 10);
            this[StatsEnum.DodgeMPProbability].Base = (short)(Record.Wisdom / 10);
            this[StatsEnum.APAttack].Base = (short)(Record.Wisdom / 10);
            this[StatsEnum.MPAttack].Base = (short)(Record.Wisdom / 10);
            this[StatsEnum.TackleBlock].Base = (short)(Record.Agility / 10);
            this[StatsEnum.TackleEvade].Base = (short)(Record.Agility / 10);
            this[StatsEnum.AP].Limit = APLimit;
            this[StatsEnum.MP].Limit = MPLimit;
            this[StatsEnum.Range].Limit = RangeLimit;
        }

        private void Initialize(Monster monster)
        {
            var Record = monster.Grade;
            this[StatsEnum.AP].Base = Record.ActionPoints;
            this[StatsEnum.MP].Base = Record.MovementPoints;
            this[StatsEnum.Vitality].Base = Record.Vitality;
            this[StatsEnum.Health].Base = Math.Max(1, Record.LifePoints);
            this.Health.Taken = 0;
            this.Health.PermanentTaken = 0;
            this[StatsEnum.Strength].Base = Record.Strength;
            this[StatsEnum.Chance].Base = Record.Chance;
            this[StatsEnum.Wisdom].Base = Record.Wisdom;
            this[StatsEnum.Intelligence].Base = Record.Intelligence;
            this[StatsEnum.Agility].Base = Record.Agility;
            this[StatsEnum.Initiative].Base = (short)Record.Initiative;
            this[StatsEnum.EarthResistPercent].Base = Record.EarthResistance;
            this[StatsEnum.WaterResistPercent].Base = Record.WaterResistance;
            this[StatsEnum.FireResistPercent].Base = Record.FireResistance;
            this[StatsEnum.NeutralResistPercent].Base = Record.NeutralResistance;
            this[StatsEnum.AirResistPercent].Base = Record.AirResistance;
            this[StatsEnum.DodgeAPProbability].Base = Record.PaDodge;
            this[StatsEnum.DodgeMPProbability].Base = Record.PmDodge;
            this[StatsEnum.APAttack].Base = (short)(Record.Wisdom / 10);
            this[StatsEnum.MPAttack].Base = (short)(Record.Wisdom / 10);
            this[StatsEnum.TackleBlock].Base = Record.TackleBlock;
            this[StatsEnum.TackleEvade].Base = Record.TackleEvade;
        }

        private void Initialize(TaxCollector taxCollector)
        {
            this[StatsEnum.AP].Base = 6;
            this[StatsEnum.MP].Base = 5;
            this[StatsEnum.Health].Base = 3000;
            this[StatsEnum.Initiative].Base = 0;
            this[StatsEnum.EarthResistPercent].Base = 25;
            this[StatsEnum.WaterResistPercent].Base = 25;
            this[StatsEnum.FireResistPercent].Base = 25;
            this[StatsEnum.NeutralResistPercent].Base = 25;
            this[StatsEnum.AirResistPercent].Base = 25;
            this[StatsEnum.DodgeAPProbability].Base = (short)(taxCollector.Guild.Wisdom / 10);
            this[StatsEnum.DodgeMPProbability].Base = (short)(taxCollector.Guild.Wisdom / 10);
            this[StatsEnum.DamageBonus].Base = taxCollector.Guild.TaxCollectorDamageBonus;
            this[StatsEnum.APAttack].Base = (short)(taxCollector.Guild.Wisdom / 10);
            this[StatsEnum.MPAttack].Base = (short)(taxCollector.Guild.Wisdom / 10);
        }

        private int GetInitiative(short level)
        {
           return this[StatsEnum.Strength].Total + this[StatsEnum.Intelligence].Total + this[StatsEnum.Agility].Total
                + this[StatsEnum.Chance].Total * (this[StatsEnum.Health].Base / (50 + (level * 5) + this[StatsEnum.Vitality].Total));           
        }

        public void RefreshCharacterDerivedStats(Character character)
        {
            if (character == null)
                return;

            this[StatsEnum.Initiative].Base = (short)GetInitiative(character.Level);
            this[StatsEnum.Prospecting].Base = (short)(100 + (this[StatsEnum.Chance].Total / 10));
            this[StatsEnum.DodgeAPProbability].Base = (short)(this[StatsEnum.Wisdom].Base / 10);
            this[StatsEnum.DodgeMPProbability].Base = (short)(this[StatsEnum.Wisdom].Base / 10);
            this[StatsEnum.APAttack].Base = (short)(this[StatsEnum.Wisdom].Base / 10);
            this[StatsEnum.MPAttack].Base = (short)(this[StatsEnum.Wisdom].Base / 10);
            this[StatsEnum.TackleBlock].Base = (short)(this[StatsEnum.Agility].Base / 10);
            this[StatsEnum.TackleEvade].Base = (short)(this[StatsEnum.Agility].Base / 10);
        }

        public void Update()
        {
            Record.AP = this[StatsEnum.AP].Base;
            Record.MP = this[StatsEnum.MP].Base;
            Record.Strength = this[StatsEnum.Strength].Base;
            Record.Intelligence = this[StatsEnum.Intelligence].Base;
            Record.Wisdom = this[StatsEnum.Wisdom].Base;
            Record.Vitality = this[StatsEnum.Vitality].Base;
            Record.Agility = this[StatsEnum.Agility].Base;
            Record.Chance = this[StatsEnum.Chance].Base;
            Record.Health = this[StatsEnum.Health].Base;
            Record.DamageTaken = (this[StatsEnum.Health] as StatsHealth).Taken;
        }

        private void BindHealthOwnerStats()
        {
            if (_stats != null
                && _stats.ContainsKey(StatsEnum.Health)
                && _stats[StatsEnum.Health] is StatsHealth health)
            {
                health.OwnerStats = this;
            }
        }

        public StatsFields Clone()
        {
            StatsFields fields = (StatsFields)this.MemberwiseClone();
            fields._stats = this._stats.ToDictionary(x => x.Key, x => x.Value.Clone());

            if (this._stats.ContainsKey(StatsEnum.Health)
                && fields._stats.ContainsKey(StatsEnum.Health)
                && this._stats[StatsEnum.Health] is StatsHealth sourceHealth
                && fields._stats[StatsEnum.Health] is StatsHealth clonedHealth)
            {
                clonedHealth.Owner = sourceHealth.Owner;
                clonedHealth.OwnerStats = fields;
            }

            fields.BindHealthOwnerStats();
            return fields;
        }

        public StatsFields CloneAndChangeOwner(object owner)
        {
            StatsFields fields = new StatsFields(owner);
            Dictionary<StatsEnum, StatsData> statsClone = this._stats.ToDictionary(x => x.Key, x => x.Value.Clone());

            if (statsClone[StatsEnum.Health] is StatsHealth health)
            {
                health.Owner = owner;
                health.OwnerStats = fields;
            }

            fields._stats = statsClone;
            fields.BindHealthOwnerStats();
            return fields;
        }
    }
}
