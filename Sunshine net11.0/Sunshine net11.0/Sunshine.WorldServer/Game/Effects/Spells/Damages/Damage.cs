using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    public class Damage
    {
        private uint _diceNum { get; set; }
        private uint _diceFace { get; set; }
        
        public Damage(EffectSchoolEnum effectSchool, uint diceNum, uint diceFace, Spell spell, FightActor source)
        {
            this.EffectSchool = effectSchool;
            this._diceNum = diceNum;
            this._diceFace = diceFace;
            this.Spell = spell;
            this.Source = source;
            this.BaseMaxDamages = Math.Max((int)diceFace, (int)diceNum);
            this.BaseMinDamages = Math.Min((int)diceFace, (int)diceNum);
        }

        public Damage(int damage)
        {
            this.EffectSchool = EffectSchoolEnum.Unknown;
            this.BaseMaxDamages = Math.Max(damage, damage);
            this.BaseMinDamages = Math.Min(damage, damage);
        }     
        
        public void GenerateDamages()
        {
            switch (this.EffectGenerationType)
            {
                case EffectGenerationEnum.MaxEffects:
                    this.Amount = BaseMaxDamages;
                    break;
                case EffectGenerationEnum.MinEffects:
                    this.Amount = BaseMinDamages;
                    break;
                default:
                    AsyncRandom asyncRandom = new AsyncRandom();
                    Amount = asyncRandom.Next(BaseMinDamages, BaseMaxDamages + 1);
                    break;
            }
        }

        public EffectSchoolEnum EffectSchool { get; set; }

        public FightActor Source { get; set; }

        public Spell Spell { get; set; }

        public int BaseMaxDamages { get; set; }

        public int BaseMinDamages { get; set; }

        public int Amount { get; set; }

        public int FixedBonus { get; set; }

        public EffectGenerationEnum EffectGenerationType { get; set; }
       
    }
}
