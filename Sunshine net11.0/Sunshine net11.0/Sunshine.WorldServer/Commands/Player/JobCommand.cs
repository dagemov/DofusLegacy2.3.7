using Sunshine.Protocol.Enums;
using Sunshine.MySql.Database.Managers;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Handlers.Characters.Jobs;
using Sunshine.WorldServer.Commands;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sunshine.WorldServer.Commands.Player
{
    [CommandHandler("oficio", RoleEnum.Player)]
    public class JobCommand : WorldCommand
    {
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
            { 43, "Forjamago de Dagas" }, { 44, "Forjamago de Espadas" },
            { 45, "Forjamago de Martillos" }, { 46, "Forjamago de Palas" },
            { 47, "Forjamago de Hachas" }, { 48, "Carpintero Mago de Arcos" },
            { 49, "Carpintero Mago de Varetas" }, { 50, "Carpintero Mago de Bastones" },
            { 62, "Zapatero Mago" }, { 63, "Joyero Mago" }, { 64, "Sastre Mago" }
        };

        public override string Description => "Gestiona tus oficios. Usa .oficio para ayuda.";

        public override void Execute()
        {
            if (Client?.Character?.Jobs == null)
                return;

            if (Parameters.Length == 0 || Parameters[0] == null)
            {
                ShowHelp();
                return;
            }

            string action = Parameters[0].ToString().ToLower();

            if (action == "lista" || action == "list")
            {
                ShowJobList();
                return;
            }

            if (action == "aprender" || action == "learn")
            {
                if (Parameters.Length < 2)
                {
                    Client.Character.SendServerMessage("Uso: .oficio aprender <id>", Color.Orange);
                    return;
                }
                LearnJob(Parameters[1].ToString());
                return;
            }

            if (action == "especializar" || action == "specialize")
            {
                if (Parameters.Length < 2)
                {
                    Client.Character.SendServerMessage("Uso: .oficio especializar <id>", Color.Orange);
                    return;
                }
                SpecializeJob(Parameters[1].ToString());
                return;
            }

            ShowHelp();
        }

        private void ShowHelp()
        {
            var jobs = Client.Character.Jobs;
            var displayedJobs = jobs.GetVisibleJobs().ToList();

            Client.Character.SendServerMessage("=== OFICIOS ===", Color.Aqua);
            Client.Character.SendServerMessage(".oficio lista - Muestra todos los oficios disponibles", Color.White);
            Client.Character.SendServerMessage(".oficio aprender <id> - Aprende un oficio base (máx. 3)", Color.White);
            Client.Character.SendServerMessage(".oficio especializar <id> - Especializa un oficio (nivel 61+ req.)", Color.White);

            if (displayedJobs.Count > 0)
            {
                Client.Character.SendServerMessage("--- Tus oficios actuales ---", Color.Yellow);
                foreach (var job in displayedJobs)
                {
                    int level = ExperienceManager.Instance.GetJobLevelExperienceFloor(job.Experience);
                    string name = GetJobName(job.Job);
                    Client.Character.SendServerMessage($"  [{job.Job}] {name} - Nivel {level}", Color.LawnGreen);
                }
            }
        }

        private void ShowJobList()
        {
            var jobs = Client.Character.Jobs;

            Client.Character.SendServerMessage("=== OFICIOS BASE ===", Color.Aqua);
            foreach (var kv in JobNames)
            {
                sbyte id = kv.Key;
                if (JobsCollection.IsSpecializationStatic(id))
                    continue;

                string status = jobs.HasJob(id) ? " [APRENDIDO]" : "";
                Client.Character.SendServerMessage($"  [{id}] {kv.Value} {status}", Color.White);
            }

            Client.Character.SendServerMessage("=== ESPECIALIZACIONES ===", Color.Aqua);
            foreach (var kv in JobsCollection.SpecializationBaseMap)
            {
                sbyte specId = kv.Key;
                sbyte baseId = kv.Value;

                string status = jobs.HasJob(specId) ? " [APRENDIDO]" : "";
                string baseName = GetJobName(baseId);
                string specName = GetJobName(specId);
                Client.Character.SendServerMessage($"  [{specId}] {specName} (requiere: {baseName} lvl 61) {status}", Color.White);
            }
        }

        private void LearnJob(string jobStr)
        {
            if (!sbyte.TryParse(jobStr, out sbyte jobId))
            {
                Client.Character.SendServerMessage("ID de oficio inválido.", Color.Red);
                return;
            }

            var jobs = Client.Character.Jobs;

            if (JobsCollection.IsSpecializationStatic(jobId))
            {
                Client.Character.SendServerMessage("Ese es un oficio de especialización. Usa .oficio especializar <id>.", Color.Orange);
                return;
            }

            if (jobs.HasJob(jobId))
            {
                Client.Character.SendServerMessage("Ya tienes ese oficio.", Color.Red);
                return;
            }

            int baseCount = jobs.GetJobs().Count(x => !JobsCollection.IsSpecializationStatic(x.Job));
            if (baseCount >= 3)
            {
                Client.Character.SendServerMessage("Ya tienes 3 oficios base. Debes abandonar uno antes de aprender otro.", Color.Red);
                return;
            }

            jobs.AddJob(jobId);
            RefreshJobData();
            string name = GetJobName(jobId);
            Client.Character.SendServerMessage($"¡Has aprendido el oficio {name} [{jobId}]!", Color.Green);
        }

        private void SpecializeJob(string jobStr)
        {
            if (!sbyte.TryParse(jobStr, out sbyte specId))
            {
                Client.Character.SendServerMessage("ID de especialización inválido.", Color.Red);
                return;
            }

            var jobs = Client.Character.Jobs;

            if (!JobsCollection.IsSpecializationStatic(specId))
            {
                Client.Character.SendServerMessage("Ese no es un oficio de especialización. Usa .oficio aprender <id>.", Color.Orange);
                return;
            }

            if (jobs.HasJob(specId))
            {
                Client.Character.SendServerMessage("Ya tienes esa especialización.", Color.Red);
                return;
            }

            if (!JobsCollection.SpecializationBaseMap.TryGetValue(specId, out sbyte baseJobId))
            {
                Client.Character.SendServerMessage("Especialización no encontrada.", Color.Red);
                return;
            }

            var baseJob = jobs.GetJob(baseJobId);
            if (baseJob == null)
            {
                string baseName = GetJobName(baseJobId);
                Client.Character.SendServerMessage($"Necesitas el oficio base {baseName} [{baseJobId}] nivel 61 para esta especialización.", Color.Red);
                return;
            }

            int baseLevel = ExperienceManager.Instance.GetJobLevelExperienceFloor(baseJob.Experience);
            if (baseLevel < 61)
            {
                string baseName = GetJobName(baseJobId);
                Client.Character.SendServerMessage($"Necesitas nivel 61 en {baseName} (tienes nivel {baseLevel}).", Color.Red);
                return;
            }

            int specCount = jobs.GetJobs().Count(x => JobsCollection.IsSpecializationStatic(x.Job));
            if (specCount >= 3)
            {
                Client.Character.SendServerMessage("Ya tienes 3 especializaciones.", Color.Red);
                return;
            }

            jobs.AddJob(specId);
            RefreshJobData();
            string specName = GetJobName(specId);
            Client.Character.SendServerMessage($"¡Te has especializado como {specName} [{specId}]!", Color.Green);
        }

        private void RefreshJobData()
        {
            JobHandler.SendJobDescriptionMessage(Client);
            JobHandler.SendJobExperienceUpdateMessage(Client);
            JobHandler.SendJobCrafterDirectorySettingsMessage(Client);
        }

        private static string GetJobName(sbyte jobId)
        {
            return JobNames.TryGetValue(jobId, out string name) ? name : $"Desconocido [{jobId}]";
        }
    }
}
