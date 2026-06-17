using Sunshine.Logs;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Messages;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Handlers.Characters.Jobs
{
    public class JobHandler : WorldPacketHandler
    {
        private const short LearnJobTextMessageId = 112;

        private static readonly Dictionary<sbyte, string> JobNames = new Dictionary<sbyte, string>
        {
            { 2, "Leñador" }, { 11, "Herrero de Espadas" }, { 13, "Carpintero de Arcos" },
            { 14, "Herrero de Martillos" }, { 15, "Zapatero" }, { 16, "Joyero" },
            { 17, "Herrero de Dagas" }, { 18, "Carpintero de Bastones" },
            { 19, "Carpintero de Varetas" }, { 20, "Herrero de Palas" },
            { 24, "Minero" }, { 25, "Panadero" }, { 26, "Alquimista" },
            { 27, "Sastre" }, { 28, "Campesino" }, { 31, "Herrero de Hachas" },
            { 36, "Pescador" }, { 41, "Cazador" }, { 56, "Carnicero" },
            { 58, "Pescadero" }, { 60, "Herrero de Escudos" }, { 65, "Manitas" },
        };

        public static string GetJobDisplayName(sbyte jobId)
        {
            return JobNames.TryGetValue(jobId, out var name) ? name : $"Oficio #{jobId}";
        }

        public static void SendJobCrafterDirectorySettingsMessage(WorldClient client)
        {
            client.Send(new JobCrafterDirectorySettingsMessage(client.Character.Jobs.GetJobsCraftersDirectorySettings()));
        }

        public static void SendJobDescriptionMessage(WorldClient client)
        {
            client.Send(new JobDescriptionMessage(client.Character.Jobs.GetJobsDescriptions()));
        }

        public static void SendJobExperienceUpdateMessage(WorldClient client)
        {
            client.Send(new JobExperienceMultiUpdateMessage(client.Character.Jobs.GetJobsExperiences()));
        }

        public static void SendVisibleJobDataMessage(WorldClient client)
        {
            if (client?.Character?.Jobs == null)
                return;

            SendJobDescriptionMessage(client);
            SendJobExperienceUpdateMessage(client);
            SendJobCrafterDirectorySettingsMessage(client);
        }

        public static void SendMaskedJobDataMessage(WorldClient client)
        {
            if (client == null)
                return;

            client.Send(new JobDescriptionMessage(System.Array.Empty<JobDescription>()));
            client.Send(new JobExperienceMultiUpdateMessage(System.Array.Empty<JobExperience>()));
            client.Send(new JobCrafterDirectorySettingsMessage(System.Array.Empty<JobCrafterDirectorySettings>()));
        }

        public static void SyncJobsOnLogin(WorldClient client)
        {
            if (client?.Character?.Jobs == null)
                return;

            var jobs = client.Character.Jobs.GetJobs().ToList();
            if (jobs.Count == 0)
            {
                LogJobSync("login", client, jobs, "empty");
                return;
            }

            SendVisibleJobDataMessage(client);
            foreach (var job in jobs)
                client.Send(new JobListedUpdateMessage(true, job.Job));

            LogJobSync("login", client, jobs, "5655+5809+5652+6016x" + jobs.Count);
        }

        public static void NotifyJobLearned(WorldClient client, sbyte jobId)
        {
            if (client?.Character?.Jobs == null)
                return;

            SendVisibleJobDataMessage(client);
            SendSingleJobExperienceUpdate(client, jobId);
            client.Send(new JobListedUpdateMessage(true, jobId));

            string jobName = GetJobDisplayName(jobId);
            client.Character.SendInformationMessage(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, LearnJobTextMessageId, jobName);
            client.Character.SendServerMessage($"Has aprendido el oficio: {jobName}.");

            LogJobSync("learn", client, client.Character.Jobs.GetJobs().Where(x => x.Job == jobId).ToList(),
                "5655+5809+5652+5654+6016+Text112");
            LogJobUi(client, jobId, panelExpected: true, infoMessage: true);
        }

        public static void NotifyJobsCleared(WorldClient client)
        {
            if (client?.Character?.Jobs == null)
                return;

            SendMaskedJobDataMessage(client);
            LogJobSync("clear", client, new List<MySql.Database.World.Characters.CharacterJobRecord>(), "masked");
            LogJobUi(client, 0, panelExpected: false, infoMessage: false);
        }

        public static void NotifyJobRemoved(WorldClient client, sbyte jobId)
        {
            if (client?.Character?.Jobs == null)
                return;

            SendVisibleJobDataMessage(client);
            client.Send(new JobListedUpdateMessage(false, jobId));
            client.Send(new JobUnlearntMessage(jobId));
            LogJobSync("remove", client, client.Character.Jobs.GetJobs().ToList(), "5655+5809+5652+6016false+5657");
        }

        public static void NotifyJobExperienceChanged(WorldClient client, sbyte jobId, long oldXp, long newXp)
        {
            if (client?.Character?.Jobs == null)
                return;

            SendSingleJobExperienceUpdate(client, jobId);
            LogJobSync("xp", client, client.Character.Jobs.GetJobs().Where(x => x.Job == jobId).ToList(),
                $"5654 oldXp={oldXp} newXp={newXp}");
        }

        public static void SendObjectFoundWhileRecoltingMessage(WorldClient client, int guid, int quantity)
        {
            client.Send(new ObjectFoundWhileRecoltingMessage(guid, quantity, guid));
        }

        private static void SendSingleJobExperienceUpdate(WorldClient client, sbyte jobId)
        {
            var jobRecord = client.Character.Jobs.GetJob(jobId);
            if (jobRecord == null)
                return;

            int level = ExperienceManager.Instance.GetJobLevelExperienceFloor(jobRecord.Experience);
            double jobExpTotal = ExperienceManager.Instance.GetJobExperienceLevelFloor((sbyte)level);
            double jobNextExp = ExperienceManager.Instance.GetJobNextExperienceLevelFloor((sbyte)level);
            client.Send(new JobExperienceUpdateMessage(new JobExperience(jobId, (sbyte)level, jobRecord.Experience, jobExpTotal, jobNextExp)));
        }

        private static void LogJobSync(string phase, WorldClient client, IList<MySql.Database.World.Characters.CharacterJobRecord> jobs, string packets)
        {
            if (client?.Character == null)
                return;

            string jobIds = jobs == null || jobs.Count == 0
                ? "-"
                : string.Join(",", jobs.Select(x => x.Job));

            Logger.WriteInfo($"[JobSync] phase={phase} charId={client.Character.Id} jobsCount={jobs?.Count ?? 0} jobIds={jobIds} packets={packets}");
        }

        private static void LogJobUi(WorldClient client, sbyte jobId, bool panelExpected, bool infoMessage)
        {
            if (client?.Character == null)
                return;

            Logger.WriteInfo($"[JobUi] charId={client.Character.Id} jobId={jobId} panelExpected={panelExpected} messageSent={infoMessage}");
        }
    }
}
