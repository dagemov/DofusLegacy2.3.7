using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Characters.Spells;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Monsters;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Fights.History;
using Sunshine.WorldServer.Game.Fights.Teams;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Fighters
{
    public class MonsterFighter : AIFighter, IMonster
    {
        public Monster Monster { get; set; }

        public MonsterFighter(Monster monster, Fight fight)
            : base(monster.Id, monster.Look, monster.Grade.Level, monster.Stats.CloneAndChangeOwner(monster), fight)
        {
            Monster = monster;

            // Les instances de monstres peuvent être réutilisées par certains spawns/groupes.
            // On force donc des PV propres à l'entrée en combat pour éviter les barres de vie décalées.
            NormalizeFightHealth(true);
        }

        public IEnumerable<MonsterDrop> Drops { get { return Monster.Drops; } }

        public MonsterGrade Grade { get { return Monster.Grade; } }

        public bool IsDungeonBoss()
        {
            if (Fight == null || Fight.Map == null || !Fight.Map.IsDungeon() || Team == null)
                return false;

            var dungeonDefenders = Fight.Team != null
                ? Fight.Team.Defenders.OfType<MonsterFighter>().Where(x => x != null && x.Fight == Fight && x.IsAlive).ToArray()
                : new MonsterFighter[0];

            if (dungeonDefenders.Length == 0)
                return false;

            short highestLevel = dungeonDefenders.Max(x => x.Level);
            return Level >= highestLevel;
        }


        public override GameFightFighterInformations GetGameFightFighterInformations(WorldClient client = null)
        { 
            return new GameFightMonsterInformations(Id, Look.GetEntityLook(), GetEntityDispositionInformations(client), (sbyte)TeamEnum.TEAM_DEFENDER, IsAlive,
                GetGameFightMinimalStats(client), (short)Monster.Record.Id, (sbyte)Monster.Grade.GradeId);
        }

        public override GameFightFighterInformations GetGameFightFighterPreparationInformations(WorldClient client = null)
        { 
            return new GameFightMonsterInformations(Id, Look.GetEntityLook(), GetEntityDispositionInformations(client), (sbyte)TeamEnum.TEAM_DEFENDER, IsAlive,
                GetGameFightMinimalStatsPreparation(client), (short)Monster.Record.Id, (sbyte)Monster.Grade.GradeId);
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberMonsterInformations(Id, Monster.Record.Id, (sbyte)Monster.Grade.GradeId);
        }
    }
}
