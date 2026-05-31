using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Monsters;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.AI;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Actors.Monsters
{
    public class Monster
    {
        private MonsterGrade[] _grades;

        public MonsterTemplate Record { get; set; }

        public AIEnum[] AI { get; set; }

        public Monster(MonsterTemplate record, MonsterGrade[] grades)
        {
            _grades = grades;
            Record = record;
            Look = EntityManager.Instance.GetActorLook(record.EntityLook);
            Drops = MonsterManager.Instance.GetMonsterDrops(record.Id);
            GenerateGrade();
            LoadSpells();
            AI = ResolveAI(record);
        }

        public Monster(MonsterTemplate record, MonsterGrade grade)
        {
            Record = record;
            Grade = grade;
            Look = EntityManager.Instance.GetActorLook(record.EntityLook);
            Drops = MonsterManager.Instance.GetMonsterDrops(record.Id);
            Stats = new StatsFields(this);
            LoadSpells();
            AI = ResolveAI(record);
        }


        private static AIEnum[] ResolveAI(MonsterTemplate record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.AI))
                return Array.Empty<AIEnum>();

            var values = new List<AIEnum>();
            foreach (var raw in record.AI.Split(','))
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (!byte.TryParse(raw.Trim(), out var parsed))
                    continue;

                if (!Enum.IsDefined(typeof(AIEnum), (int)parsed))
                    continue;

                values.Add((AIEnum)parsed);
            }

            return values.ToArray();
        }

        public int Id { get; set; }

        public IEnumerable<MonsterDrop> Drops { get; set; }

        public ActorLook Look { get; set; }

        public MonsterGrade Grade { get; set; }

        public StatsFields Stats { get; set; }

        public List<Spell> Spells { get; set; }

        public MonsterGroup GetMonsterGroup { get; set; }

        public bool CanPlay() { return Record.CanPlay; }

        public bool CanTackle() { return Record.CanTackle; }

        public MonsterInGroupInformations GetMonsterInGroupInformations
            => new MonsterInGroupInformations(Record.Id, (sbyte)Grade.GradeId, Look.GetEntityLook());

        public void GenerateGrade()
        {
            AsyncRandom rdn = new AsyncRandom();
            Grade = _grades[rdn.Next(0, _grades.Length)];
        }

        private void LoadSpells()
        {
            List<Spell> spells;
            if (!MonsterManager.Instance.MonsterSpells.TryGetValue(Record.Id, out spells))
                spells = new List<Spell>();

            Spells = spells != null ? new List<Spell>(spells) : new List<Spell>();
        }

        public Monster Clone()
        {
            return (Monster)this.MemberwiseClone();
        }
    }
}
