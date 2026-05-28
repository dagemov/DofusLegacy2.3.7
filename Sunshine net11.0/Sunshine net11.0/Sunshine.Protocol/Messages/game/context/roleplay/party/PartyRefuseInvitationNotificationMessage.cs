
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyRefuseInvitationNotificationMessage : Message
{

public const uint Id = 5596;
public override uint MessageId
{
    get { return Id; }
}

public int guestId;
        

public PartyRefuseInvitationNotificationMessage()
{
}

public PartyRefuseInvitationNotificationMessage(int guestId)
        {
            this.guestId = guestId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(guestId);
            

}

public override void Deserialize(IDataReader reader)
{

guestId = reader.ReadInt();
            if (guestId < 0)
                throw new Exception("Forbidden value on guestId = " + guestId + ", it doesn't respect the following condition : guestId < 0");
            

}


}


}
