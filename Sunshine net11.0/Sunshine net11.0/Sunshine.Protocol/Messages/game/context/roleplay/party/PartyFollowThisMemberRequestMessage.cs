
















// Generated on 10/13/2017 02:18:52
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyFollowThisMemberRequestMessage : PartyFollowMemberRequestMessage
{

public const uint Id = 5588;
public override uint MessageId
{
    get { return Id; }
}

public bool enabled;
        

public PartyFollowThisMemberRequestMessage()
{
}

public PartyFollowThisMemberRequestMessage(int playerId, bool enabled)
         : base(playerId)
        {
            this.enabled = enabled;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteBoolean(enabled);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            enabled = reader.ReadBoolean();
            

}


}


}
