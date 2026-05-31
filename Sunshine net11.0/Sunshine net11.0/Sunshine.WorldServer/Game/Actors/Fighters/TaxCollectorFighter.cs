using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.TaxCollectors;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class TaxCollectorFighter : AIFighter
    {
        public TaxCollector TaxCollector { get; set; }
        
        public TaxCollectorFighter(TaxCollector taxCollector, Fight fight)
            : base(ActorManager.Instance.GenerateId(true), taxCollector.Look, taxCollector.Guild.Level, taxCollector.Stats, fight)
        {
            taxCollector.RefreshSpells();
            TaxCollector = taxCollector;
        }

        public TaxCollectorFightersInformation GetTaxCollectorFightersInformation()
        {
            System.Collections.Generic.IEnumerable<CharacterMinimalPlusLookInformations> allyCharactersLooks;

            System.Collections.Generic.IEnumerable<CharacterMinimalPlusLookInformations> ennemyCharactersLooks;

            if (Fight.State == FightStateEnum.Placement && Fight is FightPvT)
                allyCharactersLooks = (Fight as FightPvT).DefendersQueue.Select(X => X.GetCharacterMinimalPlusLookInformations());             
            else
                allyCharactersLooks = Fight.Team.Defenders.Where(x => x is CharacterFighter).Select(x => (x as CharacterFighter).Character.GetCharacterMinimalPlusLookInformations());

            ennemyCharactersLooks = Fight.Team.Attackers.Where(x => x is CharacterFighter).Select(x => (x as CharacterFighter).Character.GetCharacterMinimalPlusLookInformations());

            return new TaxCollectorFightersInformation(TaxCollector.Id, allyCharactersLooks, ennemyCharactersLooks);
        }

        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        {
            return new GameFightTaxCollectorInformations(Id, Look.GetEntityLook(), GetEntityDispositionInformations(client), (sbyte)TeamEnum.TEAM_DEFENDER, IsAlive,
                GetGameFightMinimalStats(client), TaxCollector.FirstName, TaxCollector.LastName, TaxCollector.Level);
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        {
            return new GameFightTaxCollectorInformations(Id, Look.GetEntityLook(), GetEntityDispositionInformations(client), (sbyte)TeamEnum.TEAM_DEFENDER, IsAlive,
                GetGameFightMinimalStatsPreparation(client), TaxCollector.FirstName, TaxCollector.LastName, TaxCollector.Level);
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberTaxCollectorInformations(Id, TaxCollector.FirstName, TaxCollector.LastName, (byte)TaxCollector.Level, TaxCollector.Guild.Id, TaxCollector.Id);
        }
    }
}
