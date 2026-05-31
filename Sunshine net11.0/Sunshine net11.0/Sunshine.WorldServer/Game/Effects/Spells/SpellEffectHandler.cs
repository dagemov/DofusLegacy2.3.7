using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Effects.Spells
{
    public abstract class SpellEffectHandler
    {
        private EffectsEnum _id;
        private uint _diceNum;
        private uint _diceFace;
        private int _value;
        private int _delay;
        private int _duration;
        private SpellTargetType _targetType;
        private short _targetedCell;
        private IEnumerable<FightActor> _affectedActors;
        private FightActor _caster;
        private Spell _spell;
        private Effect _effect;
        private short _trapCell;
        private ObjectPosition _firstPosition;
        private int _countPushed;

        public abstract void Apply();

        public void Initialize(List<object> parameters)
        {
            Prepare(parameters);
            this.Apply();
        }

        public void Prepare(List<object> parameters)
        {
            _id = (EffectsEnum)parameters[0];
            _diceNum = (uint)parameters[1];
            _diceFace = (uint)parameters[2];
            _value = (int)parameters[3];
            _delay = (int)parameters[4];
            _duration = (int)parameters[5];
            _targetType = (SpellTargetType)parameters[6];
            _targetedCell = (short)parameters[7];
            _affectedActors = (IEnumerable<FightActor>)parameters[8];
            _caster = (FightActor)parameters[9];
            _spell = (Spell)parameters[10];
            _effect = (Effect)parameters[11];
            _trapCell = (short)parameters[12];
            _firstPosition = (ObjectPosition)parameters[13];
            _countPushed = (int)parameters[14];
        }

        public EffectsEnum Id { get { return _id; } set { _id = value; } }

        public uint DiceNum { get { return _diceNum; } set { _diceNum = value; } }

        public uint DiceFace { get { return _diceFace; } set { _diceFace = value; } }

        public int Value { get { return _value; } set { _value = value; } }

        public int Delay { get { return _delay; } set { _delay = value; } }

        public int Duration { get { return _duration; } set { _duration = value; } }

        public SpellTargetType TargetType { get { return _targetType; } set { _targetType = value; } }

        public IEnumerable<FightActor> GetAffectedActors() { return _affectedActors; }

        public FightActor Caster { get { return _caster; } }

        public Spell Spell { get { return _spell; } }

        public Effect Effect { get { return _effect; } }

        public short TargetedCell { get { return _targetedCell; } set { _targetedCell = value; } }

        public Fight Fight { get { return Caster != null ? Caster.Fight : null; } }

        public short TrapCell { get { return _trapCell; } }

        public ObjectPosition FirstPosition { get { return _firstPosition; } }

        public int CountPushed { get { return _countPushed; } }
        
        public virtual bool RequireSilentCast()
        {
            return false;
        }
    }
}
