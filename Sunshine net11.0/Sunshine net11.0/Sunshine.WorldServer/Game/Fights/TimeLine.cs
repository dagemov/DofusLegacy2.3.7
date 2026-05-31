using Sunshine.WorldServer.Game.Actors.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights
{
    public class TimeLine
    {
        private Fight _fight;
        private int _roundNumber = 1;

        public TimeLine(Fight fight)
        {
            _fight = fight;
            Fighters = new List<FightActor>();
            Leavers = new List<FightActor>();
        }

        public List<FightActor> Fighters { get; set; }
        
        public List<FightActor> Leavers { get; set; }

        public void AddLeavers(FightActor leaver)
        {
            if (!Leavers.Contains(leaver))
                Leavers.Add(leaver);
        }

        public void AddFighter(FightActor fighter)
        {
            if (fighter == null)
                return;

            bool alreadyExists = Fighters.Contains(fighter) ||
                                 Fighters.Any(x => x != null && (x.Id == fighter.Id ||
                                     (x is CharacterFighter existingCharacter && fighter is CharacterFighter newCharacter && existingCharacter.Character?.Id == newCharacter.Character?.Id)));

            if (!alreadyExists)
                Fighters.Add(fighter);
        }

        public int RoundNumber
        {
            get { return _roundNumber; }
            set { _roundNumber = value; }
        }
    }
}
