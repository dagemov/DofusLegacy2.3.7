
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyMemberRemoveMessage : Message
{

public const uint Id = 5579;
public override uint MessageId
{
    get { return Id; }
}

public int leavingPlayerId;
        

public PartyMemberRemoveMessage()
{
}

public PartyMemberRemoveMessage(int leavingPlayerId)
        {
            this.leavingPlayerId = leavingPlayerId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(leavingPlayerId);
            

}

public override void Deserialize(IDataReader reader)
{

leavingPlayerId = reader.ReadInt();
            if (leavingPlayerId < 0)
                throw new Exception("Forbidden value on leavingPlayerId = " + leavingPlayerId + ", it doesn't respect the following condition : leavingPlayerId < 0");
            

}


}


}
