using Sunshine.Protocol.Messages;
using Sunshine.WorldServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunshine.Protocol.Utils.Extensions;
using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Handlers.Basic
{
    public class BasicHandler : WorldPacketHandler
    {
        public static void SendBasicTimeMessage(WorldClient client)
        {
            client.Send(new BasicTimeMessage(DateTime.Now.GetUnixTimeStamp(), 120));
        }

        public static void SendBasicNoOperationMessage(WorldClient client)
        {
            client.Send(new BasicNoOperationMessage());
        }

        public static void SendTextInformationMessage(WorldClient client, TextInformationTypeEnum msgType, short msgId, object[] parameters)
        {
            client.Send(new TextInformationMessage((sbyte)msgType, msgId, from entry in parameters
                                                                          select entry.ToString()));
        }

        public static void SendTextInformationMessage(WorldClient client, TextInformationTypeEnum msgType, short msgId)
        {
            client.Send(new TextInformationMessage((sbyte)msgType, msgId, new string[] { }));
        }

        public static void SendNotificationByServerMessage(WorldClient client, string msg, NotificationEnum notification, bool isForced)
        {
            client.Send(new NotificationByServerMessage((ushort)notification, new string[] { msg }, isForced));        
        }
    }
}
