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
            sbyte job = sbyte.Parse(Parameters[0] as string);
            bool alreadyKnown = Client.Character.Jobs.HasJob(job);

            if (alreadyKnown)
            {
                Logger.WriteInfo($"[NpcAction] type=LearnJob jobId={job} result=already_known");
                Logger.WriteInfo($"[JobLearn] charId={Client.Character.Id} jobId={job} alreadyKnown=true saved=false notified=false");
                return false;
            }

            int baseCountBefore = Client.Character.Jobs.GetJobs().Count(x => !JobsCollection.IsSpecializationStatic(x.Job));
            Client.Character.Jobs.AddJob(job);
            bool added = Client.Character.Jobs.HasJob(job);

            if (!added)
            {
                Logger.WriteInfo($"[NpcAction] type=LearnJob jobId={job} result=rejected_max_jobs");
                Logger.WriteInfo($"[JobLearn] charId={Client.Character.Id} jobId={job} alreadyKnown=false saved=false notified=false baseCountBefore={baseCountBefore}");
                return false;
            }

            CharacterManager.Instance.Save(Client.Character);
            JobHandler.SendVisibleJobDataMessage(Client);
            Client.Send(new JobListedUpdateMessage(true, job));

            Logger.WriteInfo($"[NpcAction] type=LearnJob jobId={job} result=success");
            Logger.WriteInfo($"[JobLearn] charId={Client.Character.Id} jobId={job} alreadyKnown=false saved=true notified=true baseCountBefore={baseCountBefore}");
            return true;
        }
    }
}
