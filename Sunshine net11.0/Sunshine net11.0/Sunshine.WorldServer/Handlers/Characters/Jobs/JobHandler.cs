using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using Sunshine.WorldServer.Game.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Types;

namespace Sunshine.WorldServer.Handlers.Characters.Jobs
{
    public class JobHandler : WorldPacketHandler
    {
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
            if (client == null || client.Character == null || client.Character.Jobs == null)
                return;

            SendJobDescriptionMessage(client);
            SendJobExperienceUpdateMessage(client);
            SendJobCrafterDirectorySettingsMessage(client);
        }

        public static void SendMaskedJobDataMessage(WorldClient client)
        {
            if (client == null)
                return;

            client.Send(new JobDescriptionMessage(Array.Empty<JobDescription>()));
            client.Send(new JobExperienceMultiUpdateMessage(Array.Empty<JobExperience>()));
            client.Send(new JobCrafterDirectorySettingsMessage(Array.Empty<JobCrafterDirectorySettings>()));
        }

        public static void SendObjectFoundWhileRecoltingMessage(WorldClient client, int guid, int quantity)
        {
            client.Send(new ObjectFoundWhileRecoltingMessage(guid, quantity, guid));
        }
    }
}
