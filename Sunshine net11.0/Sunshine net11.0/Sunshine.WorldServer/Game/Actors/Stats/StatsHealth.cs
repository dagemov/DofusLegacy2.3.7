using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Stats
{
    public class StatsHealth : StatsData
    {
        public StatsHealth(object owner, int baseValue) 
            : base(baseValue, null)
        {
            Owner = owner;
        }

        public object Owner { get; set; }

        // Référence vers l'instance StatsFields qui possède CE StatsHealth.
        // Important pour les monstres/invocations : leurs stats sont clonées par combattant,
        // donc le calcul des PV max doit lire la Vitality du clone, pas celle du template Monster.
        public StatsFields OwnerStats { get; set; }

        public int Taken { get; set; }

        public int PermanentTaken { get; set; }

        public override int Total
        {
            get
            {
                return (int)(((TotalMax - Taken)) > 0 ? (int)((TotalMax - Taken)) : 0);
            }
        }

        public override int TotalMax
        {
            get
            {
                int vitality = GetVitalityContributionToLife();
                int total = (Base + vitality + Equiped + Context) - PermanentTaken;

                // Un combattant ne doit jamais avoir 0 PV max. 0 PV doit uniquement représenter l'état mort.
                return Math.Max(1, total);
            }
        }

        private int GetVitalityContributionToLife()
        {
            StatsData vitalityStat = null;

            // Priorité à OwnerStats : c'est l'instance de stats réellement portée par le combattant.
            // Sans ça, un MonsterFighter cloné peut lire la Vitality du template Monster.
            if (OwnerStats != null)
                vitalityStat = OwnerStats[StatsEnum.Vitality];

            if (vitalityStat == null)
            {
                switch (Owner != null ? Owner.GetType().Name : string.Empty)
                {
                    case "Character":
                        vitalityStat = (Owner as Character)?.Stats?[StatsEnum.Vitality];
                        break;

                    case "Monster":
                        vitalityStat = (Owner as Monster)?.Stats?[StatsEnum.Vitality];
                        break;
                }
            }

            if (vitalityStat == null)
                return 0;

            string ownerName = Owner != null ? Owner.GetType().Name : string.Empty;

            // Les grades de monstres ont déjà leurs PV max dans LifePoints.
            // Leur Vitality de base est une caractéristique de combat et ne doit pas être ajoutée une 2e fois.
            // On garde seulement les bonus/malus de vitalité appliqués pendant le combat.
            if (ownerName == "Monster" || ownerName == "TaxCollector")
                return vitalityStat.Equiped + vitalityStat.Context;

            // Pour les personnages, la vitalité brute augmente/réduit bien les PV max.
            return vitalityStat.Base + vitalityStat.Equiped + vitalityStat.Context;
        }
    }
}
