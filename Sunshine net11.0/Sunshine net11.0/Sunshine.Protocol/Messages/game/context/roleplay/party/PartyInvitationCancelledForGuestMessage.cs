
















// Generated on 10/13/2017 02:18:52
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyInvitationCancelledForGuestMessage : Message
{

public const uint Id = 6256;
public override uint MessageId
{
    get { return Id; }
}

public int partyId;
        public int cancelerId;
        

public PartyInvitationCancelledForGuestMessage()
{
}

public PartyInvitationCancelledForGuestMessage(int partyId, int cancelerId)
        {
            this.partyId = partyId;
            this.cancelerId = cancelerId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(partyId);
            writer.WriteInt(cancelerId);
            

}

public override void Deserialize(IDataReader reader)
{

partyId = reader.ReadInt();
            if (partyId < 0)
                throw new Exception("Forbidden value on partyId = " + partyId + ", it doesn't respect the following condition : partyId < 0");
            cancelerId = reader.ReadInt();
            if (cancelerId < 0)
                throw new Exception("Forbidden value on cancelerId = " + cancelerId + ", it doesn't respect the following condition : cancelerId < 0");
            

}


}


}
