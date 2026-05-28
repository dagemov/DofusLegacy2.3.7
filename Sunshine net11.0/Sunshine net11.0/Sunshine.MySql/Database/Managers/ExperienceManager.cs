using Sunshine.MySql.Database.World.Experiences;
using System;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Mysql.Database;
using Sunshine.Protocol.Utils;

namespace Sunshine.MySql.Database.Managers
{
    public class ExperienceManager : Singleton<ExperienceManager>
    {
        public const sbyte MaxSupportedJobLevel = 100;

        private Dictionary<int, Experience> _experiencesByLevel = new Dictionary<int, Experience>();

        private Dictionary<long, Experience> _experiencesByCharacterExp = new Dictionary<long, Experience>();

        private Dictionary<long, Experience> _experiencesByGuildExp = new Dictionary<long, Experience>();

        private Dictionary<long, Experience> _experiencesByMountExp = new Dictionary<long, Experience>();

        private Dictionary<int, Experience> _experiencesByAlignmentExp = new Dictionary<int, Experience>();

        private Dictionary<long, Experience> _experiencesByJobExp = new Dictionary<long, Experience>();

        public void LoadAllExperiences()
        {
            _experiencesByLevel = DatabaseManager.Connection.Query<Experience>("SELECT * FROM experiences").ToDictionary(x => x.Level);
            _experiencesByCharacterExp = DatabaseManager.Connection.Query<Experience>("SELECT * FROM experiences").ToDictionary(x => x.CharacterExp);
            _experiencesByGuildExp = DatabaseManager.Connection.Query<Experience>("SELECT * FROM experiences").ToDictionary(x => x.GuildExp);
            _experiencesByMountExp = DatabaseManager.Connection.Query<Experience>("SELECT * FROM experiences WHERE Level <= '100'").ToDictionary(x => x.MountExp);
            _experiencesByAlignmentExp = DatabaseManager.Connection.Query<Experience>("SELECT * FROM experiences WHERE Level <= '10'").ToDictionary(x => x.AlignmentExp);
            _experiencesByJobExp = DatabaseManager.Connection.Query<Experience>("SELECT * FROM experiences WHERE JobExp IS NOT NULL AND Level <= '100'")
                .GroupBy(x => x.JobExp)
                .Select(x => x.First())
                .ToDictionary(x => x.JobExp);
        }

        public long GetExperienceLevelFloor(short level)
        {
            if (_experiencesByLevel.Count == 0)
                return 0;

            level = Math.Clamp(level, (short)1, (short)200);

            if (_experiencesByLevel.TryGetValue(level, out var experience))
                return experience.CharacterExp;

            return _experiencesByLevel.OrderBy(x => x.Key).Last().Value.CharacterExp;
        }

        public long GetNextExperienceLevelFloor(short level)
        {
            if (_experiencesByLevel.Count == 0)
                return 0;

            if (level >= 200)
                return GetExperienceLevelFloor(200);

            var nextLevel = (short)Math.Max(1, Math.Min(200, level + 1));
            if (_experiencesByLevel.TryGetValue(nextLevel, out var experience))
                return experience.CharacterExp;

            return _experiencesByLevel.OrderBy(x => x.Key).Last().Value.CharacterExp;
        }

        public short GetNextLevelExperienceFloor(long experience)
        {
            return (short)_experiencesByCharacterExp.LastOrDefault(x => x.Key <= experience).Value.Level;
        }

        public sbyte GetNextGradeHonorFloor(ushort honor)
        {
            var level = _experiencesByAlignmentExp.LastOrDefault(x => x.Key <= honor).Value;
            return (sbyte)(level != null ? Math.Max(0, Math.Min(10, level.Level)) : 0);
        }

        public ushort GetHonorGradeFloor(int grade)
        {
            if (grade <= 0)
                return 0;

            if (!_experiencesByLevel.ContainsKey(grade))
                grade = 10;

            return (ushort)_experiencesByLevel[grade].AlignmentExp;
        }

        public ushort GetHonorNextGradeFloor(int grade)
        {
            if (grade < 0)
                grade = 0;

            return grade >= 10 ? (ushort)17500 : (ushort)_experiencesByLevel[grade + 1].AlignmentExp;
        }

        public long GetJobExperienceLevelFloor(sbyte level)
        {
            if (_experiencesByLevel.Count == 0)
                return 0;

            var normalizedLevel = Math.Max(1, Math.Min(MaxSupportedJobLevel, (int)level));
            if (_experiencesByLevel.TryGetValue(normalizedLevel, out var experience))
                return experience.JobExp;

            return _experiencesByLevel
                .Where(x => x.Key <= MaxSupportedJobLevel && x.Value.JobExp > 0)
                .OrderBy(x => x.Key)
                .Last()
                .Value.JobExp;
        }

        public long GetJobNextExperienceLevelFloor(sbyte level)
        {
            if (_experiencesByLevel.Count == 0)
                return 0;

            var normalizedLevel = Math.Max(1, Math.Min(MaxSupportedJobLevel, (int)level));
            if (normalizedLevel >= MaxSupportedJobLevel)
                return GetJobExperienceLevelFloor(MaxSupportedJobLevel);

            var nextLevel = normalizedLevel + 1;
            if (_experiencesByLevel.TryGetValue(nextLevel, out var experience))
                return experience.JobExp;

            return _experiencesByLevel
                .Where(x => x.Key <= MaxSupportedJobLevel && x.Value.JobExp > 0)
                .OrderBy(x => x.Key)
                .Last()
                .Value.JobExp;
        }

        public sbyte GetJobLevelExperienceFloor(long experience)
        {
            if (_experiencesByJobExp.Count == 0)
                return 1;

            var entry = _experiencesByJobExp
                .Where(x => x.Value != null && x.Value.Level <= MaxSupportedJobLevel && x.Key <= experience)
                .OrderBy(x => x.Key)
                .LastOrDefault()
                .Value;

            return (sbyte)Math.Max(1, Math.Min(MaxSupportedJobLevel, entry != null ? entry.Level : 1));
        }

        public sbyte GetNextJobLevelExperienceFloor(long experience)
        {
            return GetJobLevelExperienceFloor(experience);
        }

        public byte GetGuildLevelExperience(long experience)
        {
            return (byte)_experiencesByGuildExp.LastOrDefault(x => x.Key <= experience).Value.Level;
        }

        public long GetMountExperienceLevelFloor(short level)
        {
            if (level <= 1)
                return 0;

            if (!_experiencesByLevel.ContainsKey(level))
                level = 100;

            return _experiencesByLevel[level].MountExp;
        }

        public long GetMountNextExperienceLevelFloor(short level)
        {
            if (level >= 100)
                return GetMountExperienceLevelFloor(100);

            return _experiencesByLevel[level + 1].MountExp;
        }

        public short GetMountLevelExperienceFloor(long experience)
        {
            if (experience <= 0 || _experiencesByMountExp.Count == 0)
                return 1;

            var level = _experiencesByMountExp.LastOrDefault(x => x.Key <= experience).Value;
            return (short)Math.Max(1, Math.Min(100, level != null ? level.Level : 1));
        }

        public double GetNextGuildExperienceLevelFloor(byte level)
        {
            return level >= 200 ? 22356230000 : _experiencesByLevel[level + 1].GuildExp;
        }

        public double GetGuildExperienceFloor(long experience)
        {
            return (double)_experiencesByGuildExp.LastOrDefault(x => x.Key <= experience).Value.GuildExp;
        }
    }
}
