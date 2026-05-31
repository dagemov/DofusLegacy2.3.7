
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyRefuseInvitationMessage : Message
{

public const uint Id = 5582;
public override uint MessageId
{
    get { return Id; }
}

public int partyId;
        

public PartyRefuseInvitationMessage()
{
}

public PartyRefuseInvitationMessage(int partyId)
        {
            this.partyId = partyId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(partyId);
            

}

public override void Deserialize(IDataReader reader)
{

partyId = reader.ReadInt();
            if (partyId < 0)
                throw new Exception("Forbidden value on partyId = " + partyId + ", it doesn't respect the following condition : partyId < 0");
            

}


}


}
