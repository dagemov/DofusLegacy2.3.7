using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Characters
{
    public class CharacterAlignment
    {       
        private Character _character;

        public CharacterAlignment(Character character)
        {
            _character = character;
            Record = CharacterManager.Instance.GetCharacterAlign(character.Id);
        }

        public CharacterAlignmentRecord Record { get; set; }

        public int OwnerId { get { return Record.OwnerId; } }

        public AlignmentSideEnum Side
        {
            get { return (AlignmentSideEnum)Record.Side; }
            set { Record.Side = (sbyte)value; }           
        }

        public sbyte Value
        {
            get { return Record.Value; }
            set { Record.Value = value; }
        }

        public sbyte Grade
        {
            get { return Record.Grade; }
            set { Record.Grade = value; }
        }

        public ushort Honor
        {
            get { return (ushort)Record.Honor; }
            set
            {
                Record.Honor = value;

                var computedGrade = ExperienceManager.Instance.GetNextGradeHonorFloor(value);
                if (computedGrade < 0)
                    computedGrade = 0;
                if (computedGrade > 10)
                    computedGrade = 10;

                if (Grade != computedGrade)
                {
                    Grade = computedGrade;
                    _character.Map.Refresh(_character);
                    _character.RefreshStats();
                }
            }
        }

        public ushort Dishonor
        {
            get { return (ushort)Record.Dishonor; }
            set { Record.Dishonor = value; }
        }
      
        public bool Enabled
        {
            get { return Record.Enabled; }
            set
            {
                Record.Enabled = value;
                if (!value)
                    Honor -= (ushort)(Honor / 35);
            }
        }

        public ushort HonorGradeFloor { get { return ExperienceManager.Instance.GetHonorGradeFloor(Grade); } }

        public ushort HonorNextGradeFloor { get { return ExperienceManager.Instance.GetHonorNextGradeFloor(Grade); } }
    }
}
