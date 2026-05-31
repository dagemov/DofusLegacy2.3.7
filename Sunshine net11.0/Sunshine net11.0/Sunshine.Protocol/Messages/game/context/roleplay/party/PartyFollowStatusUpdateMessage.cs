
















// Generated on 10/13/2017 02:18:52
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyFollowStatusUpdateMessage : Message
{

public const uint Id = 5581;
public override uint MessageId
{
    get { return Id; }
}

public bool success;
        public int followedId;
        

public PartyFollowStatusUpdateMessage()
{
}

public PartyFollowStatusUpdateMessage(bool success, int followedId)
        {
            this.success = success;
            this.followedId = followedId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(success);
            writer.WriteInt(followedId);
            

}

public override void Deserialize(IDataReader reader)
{

success = reader.ReadBoolean();
            followedId = reader.ReadInt();
            if (followedId < 0)
                throw new Exception("Forbidden value on followedId = " + followedId + ", it doesn't respect the following condition : followedId < 0");
            

}


}


}
