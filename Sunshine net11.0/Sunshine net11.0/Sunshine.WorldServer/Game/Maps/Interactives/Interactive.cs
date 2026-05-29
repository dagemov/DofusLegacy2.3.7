using Sunshine.MySql.Database.Managers;
using Sunshine.MySql.Database.World.Maps.Interactives;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps.Interactives
{
    public class Interactive
    {
        private InteractiveSpawn _record;

        public Interactive(InteractiveSpawn record)
        {
            _record = record;
            Skills = record.SkillsCSV == null || record.SkillsCSV == "" ? new int[0] : 
                     record.SkillsCSV.Split(',').Select(x => int.Parse(x));
            Parameters = record.ParametersCSV == null || record.ParametersCSV == "" ? new List<int>() :
                     record.ParametersCSV.Split(',').Select(x => int.Parse(x)).ToList();
            GetObstacles = new List<MapObstacle>();
            LoadInteractive();
        }

        public int MapId { get { return _record.Map; } }

        public int Element { get { return _record.Element; } }

        public int Type { get { return _record.Type; } }

        public int HouseId { get { return _record.HouseId; } }

        public int PaddockInstanceId { get { return _record.PaddockInstanceId; } }

        public bool IsHouseInteractive { get { return HouseId > 0; } }

        public bool IsPaddockInstanceInteractive { get { return PaddockInstanceId > 0; } }

        public IEnumerable<int> Skills { get; set; }

        public List<int> Parameters { get; set; }

        public InteractiveElement GetInteractiveElement { get; set; }

        public StatedElement GetStatedElement { get; set; }

        public List<MapObstacle> GetObstacles { get; set; }

        private void LoadInteractive()
        {
            var interactiveSkills = new List<InteractiveElementSkill>();
            foreach (var skill in Skills)
            {
                if (skill == -1)
                {
                    GetStatedElement = new StatedElement(Element, (short)Parameters[0], (int)InteractiveStateEnum.STATE_NORMAL);
                    for(int i = 0; i < Parameters.Count; i ++)
                        GetObstacles.Add(new MapObstacle((short)Parameters[i], (sbyte)InteractiveStateEnum.STATE_ANIMATED));
                }
                else
                {
                    var skillUid = InteractiveManager.Instance.GenerateId();
                    interactiveSkills.Add(new InteractiveElementSkill(skill, skillUid));
                    InteractiveManager.Instance.RegisterSkill(skillUid, _record.Map, skill, Element, _record.HouseId, _record.PaddockInstanceId);
                    GetInteractiveElement = new InteractiveElement(Element, Type, interactiveSkills, new List<InteractiveElementSkill>());
                }
            }
        }
    }
}
