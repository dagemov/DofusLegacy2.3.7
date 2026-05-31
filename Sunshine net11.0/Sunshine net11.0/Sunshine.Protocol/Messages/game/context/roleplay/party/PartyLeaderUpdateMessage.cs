
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyLeaderUpdateMessage : Message
{

public const uint Id = 5578;
public override uint MessageId
{
    get { return Id; }
}

public int partyLeaderId;
        

public PartyLeaderUpdateMessage()
{
}

public PartyLeaderUpdateMessage(int partyLeaderId)
        {
            this.partyLeaderId = partyLeaderId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(partyLeaderId);
            

}

public override void Deserialize(IDataReader reader)
{

partyLeaderId = reader.ReadInt();
            if (partyLeaderId < 0)
                throw new Exception("Forbidden value on partyLeaderId = " + partyLeaderId + ", it doesn't respect the following condition : partyLeaderId < 0");
            

}


}


}
