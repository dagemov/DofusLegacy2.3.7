using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Game.Actors.Characters.Jobs;
using Sunshine.WorldServer.Handlers.Characters.Jobs;
using Sunshine.Logs;
using System.Linq;

namespace Sunshine.WorldServer.Game.Actors.Npcs.Replies
{
    [ReplyHandler(8)]
    public class LearnJobReply : Reply
    {
        public override bool Execute()
        {
            string jobParam = Parameters != null && Parameters.Count > 0 ? Parameters[0] as string ?? string.Empty : string.Empty;
            Logger.WriteInfo($"[JobLearn] Enter charId={Client.Character.Id} npcId={Npc?.Record?.Id} jobParam={jobParam}");

            sbyte job = sbyte.Parse(jobParam);
            bool alreadyKnown = Client.Character.Jobs.HasJob(job);

            if (alreadyKnown)
            {
                NpcReplyActionDiagnostics.LogLearnJob(Npc, 0, job, "already_known");
                Logger.WriteInfo($"[JobLearn] charId={Client.Character.Id} jobId={job} alreadyKnown=true saved=false notified=false");
                return false;
            }

            int baseCountBefore = Client.Character.Jobs.GetJobs().Count(x => !JobsCollection.IsSpecializationStatic(x.Job));
            Client.Character.Jobs.AddJob(job);
            bool added = Client.Character.Jobs.HasJob(job);

            if (!added)
            {
                NpcReplyActionDiagnostics.LogLearnJob(Npc, 0, job, "rejected_max_jobs");
                Logger.WriteInfo($"[JobLearn] charId={Client.Character.Id} jobId={job} alreadyKnown=false saved=false notified=false baseCountBefore={baseCountBefore}");
                return false;
            }

            CharacterManager.Instance.Save(Client.Character);
            JobHandler.SendVisibleJobDataMessage(Client);
            Client.Send(new JobListedUpdateMessage(true, job));

            NpcReplyActionDiagnostics.LogLearnJob(Npc, 0, job, "success");
            Logger.WriteInfo($"[JobLearn] charId={Client.Character.Id} jobId={job} alreadyKnown=false saved=true notified=true baseCountBefore={baseCountBefore}");
            return true;
        }
    }
}
