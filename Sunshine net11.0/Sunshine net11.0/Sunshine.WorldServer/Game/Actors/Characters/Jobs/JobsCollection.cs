using Sunshine.MySql.Database.Managers;
using Sunshine.BaseServer.Configuration;
using Sunshine.MySql.Database.World.Characters;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Characters;
using Sunshine.Logs;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Characters.Jobs
{
    public class JobsCollection
    {
        private const int MaxDisplayedBaseJobs = 3;
        private const int MaxDisplayedSpecializationJobs = 3;

        private static readonly HashSet<sbyte> SpecializationJobs = new HashSet<sbyte>
        {
            43, 44, 45, 46, 47, 48, 49, 50, 62, 63, 64
        };

        // Specialization job id -> required base job id (base must be level 61+ to specialize).
        public static readonly Dictionary<sbyte, sbyte> SpecializationBaseMap = new Dictionary<sbyte, sbyte>
        {
            { 43, 17 }, { 44, 11 }, { 45, 14 }, { 46, 20 }, { 47, 31 },
            { 48, 13 }, { 49, 19 }, { 50, 18 }, { 62, 15 }, { 63, 16 }, { 64, 27 }
        };

        private readonly Character _character;
        private List<CharacterJobRecord> _jobs;
        private List<CharacterJobRecord> _displayJobsCache;

        public JobsCollection(Character character)
        {
            _character = character;
            _jobs = CharacterManager.Instance.GetCharacterJobs(character.Id) ?? new List<CharacterJobRecord>();
            NormalizeJobs();
        }

        private void NormalizeJobs()
        {
            if (_jobs == null)
            {
                _jobs = new List<CharacterJobRecord>();
                _displayJobsCache = new List<CharacterJobRecord>();
                return;
            }

            var validJobs = JobManager.Instance.JobSkills.Keys
                .Where(x => x > 0)
                .Select(x => (sbyte)x)
                .ToHashSet();

            _jobs = _jobs
                .Where(x => x != null && x.Job > 0 && validJobs.Contains(x.Job))
                .GroupBy(x => x.Job)
                .Select(x => x.OrderByDescending(y => y.Experience).First())
                .OrderBy(x => x.Job)
                .ToList();

            _displayJobsCache = null;
        }

        public static bool IsSpecializationStatic(sbyte jobId)
        {
            return SpecializationJobs.Contains(jobId);
        }

        private static bool IsSpecialization(sbyte jobId)
        {
            return SpecializationJobs.Contains(jobId);
        }

        private IEnumerable<CharacterJobRecord> GetDisplayedJobs()
        {
            if (_displayJobsCache != null)
                return _displayJobsCache;

            var baseJobs = _jobs
                .Where(x => x != null && !IsSpecialization(x.Job))
                .OrderByDescending(x => ExperienceManager.Instance.GetJobLevelExperienceFloor(x.Experience))
                .ThenBy(x => x.Job)
                .Take(MaxDisplayedBaseJobs);

            var specializationJobs = _jobs
                .Where(x => x != null && IsSpecialization(x.Job))
                .OrderByDescending(x => ExperienceManager.Instance.GetJobLevelExperienceFloor(x.Experience))
                .ThenBy(x => x.Job)
                .Take(MaxDisplayedSpecializationJobs);

            _displayJobsCache = baseJobs.Concat(specializationJobs).ToList();
            return _displayJobsCache;
        }

        public IEnumerable<CharacterJobRecord> GetJobs()
        {
            return _jobs;
        }

        public IEnumerable<CharacterJobRecord> GetVisibleJobs()
        {
            return GetDisplayedJobs();
        }

        public CharacterJobRecord GetJob(sbyte id)
        {
            return _jobs.FirstOrDefault(x => x.Job == id);
        }

        public IEnumerable<JobCrafterDirectorySettings> GetJobsCraftersDirectorySettings()
        {
            var jobsCrafterSettings = new List<JobCrafterDirectorySettings>();
            foreach (var job in GetDisplayedJobs())
            {
                var level = ExperienceManager.Instance.GetJobLevelExperienceFloor(job.Experience);
                jobsCrafterSettings.Add(new JobCrafterDirectorySettings(job.Job, JobManager.Instance.GetJobSlot(level), 0));
            }

            return jobsCrafterSettings;
        }

        public IEnumerable<JobDescription> GetJobsDescriptions()
        {
            var jobsDescriptions = new List<JobDescription>();
            foreach (var job in GetDisplayedJobs())
            {
                jobsDescriptions.Add(new JobDescription(job.Job,
                    JobManager.Instance.GetSkillActionDescription(job.Job,
                        ExperienceManager.Instance.GetJobLevelExperienceFloor(job.Experience))));
            }

            return jobsDescriptions;
        }

        public IEnumerable<JobExperience> GetJobsExperiences()
        {
            var jobsExperiences = new List<JobExperience>();
            foreach (var job in GetDisplayedJobs())
            {
                var jobLevel = ExperienceManager.Instance.GetJobLevelExperienceFloor(job.Experience);
                var jobExp = job.Experience;
                var jobExpTotal = ExperienceManager.Instance.GetJobExperienceLevelFloor(jobLevel);
                var jobNextExp = ExperienceManager.Instance.GetJobNextExperienceLevelFloor(jobLevel);
                jobsExperiences.Add(new JobExperience(job.Job, jobLevel, jobExp, jobExpTotal, jobNextExp));
            }

            return jobsExperiences;
        }

        public int GetJobsLevelTotal()
        {
            return _jobs.Sum(x => ExperienceManager.Instance.GetJobLevelExperienceFloor(x.Experience));
        }

        public int GetJobsCount(int level)
        {
            return _jobs.Count(x => ExperienceManager.Instance.GetJobLevelExperienceFloor(x.Experience) >= level);
        }

        public bool HasJob(sbyte jobId)
        {
            return _jobs.Any(x => x.Job == jobId);
        }

        public void AddJob(sbyte job)
        {
            if (HasJob(job))
                return;

            bool isSpecialization = IsSpecialization(job);
            int currentBaseCount = _jobs.Count(x => !IsSpecialization(x.Job));
            int currentSpecCount = _jobs.Count(x => IsSpecialization(x.Job));

            if (!isSpecialization && currentBaseCount >= MaxDisplayedBaseJobs)
                return;

            if (isSpecialization && currentSpecCount >= MaxDisplayedSpecializationJobs)
                return;

            _jobs.Add(new CharacterJobRecord
            {
                OwnerId = _character.Id,
                Job = job,
                Experience = ExperienceManager.Instance.GetJobExperienceLevelFloor(1)
            });

            NormalizeJobs();
        }

        public void AddExperience(sbyte job, long amount)
        {
            amount = GameRates.ApplyJobXp(amount);

            if (_character?.Client?.Account?.Vip == true)
                amount = (long)System.Math.Min(amount * 1.5, long.MaxValue);

            var jobRecord = GetJob(job);
            if (jobRecord == null)
                return;

            var jobLevel = ExperienceManager.Instance.GetJobLevelExperienceFloor(jobRecord.Experience);
            var oldXp = jobRecord.Experience;
            var jobNextExp = ExperienceManager.Instance.GetNextExperienceLevelFloor(jobLevel);

            if (jobLevel >= 100)
                return;

            jobRecord.Experience += amount;
            var nextLevel = ExperienceManager.Instance.GetNextJobLevelExperienceFloor(jobRecord.Experience);
            Logger.WriteInfo($"[JobXp] charId={_character.Id} jobId={job} oldXp={oldXp} newXp={jobRecord.Experience} oldLevel={jobLevel} newLevel={nextLevel}");
            if (jobLevel < nextLevel)
            {
                var jobExpTotal = ExperienceManager.Instance.GetJobExperienceLevelFloor(nextLevel);
                jobNextExp = ExperienceManager.Instance.GetJobNextExperienceLevelFloor(nextLevel);

                _character.Client.Send(new JobLevelUpMessage(nextLevel,
                    new JobDescription(job,
                        JobManager.Instance.GetSkillActionDescription(jobRecord.Job,
                            ExperienceManager.Instance.GetJobLevelExperienceFloor(jobRecord.Experience)))));

                _character.Client.Send(new JobExperienceUpdateMessage(new JobExperience(job, nextLevel, jobRecord.Experience, jobExpTotal, jobNextExp)));
            }
        }
    }
}
