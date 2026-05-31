
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyInvitationMessage : Message
{

public const uint Id = 5586;
public override uint MessageId
{
    get { return Id; }
}

public int partyId;
        public int fromId;
        public string fromName;
        public int toId;
        

public PartyInvitationMessage()
{
}

public PartyInvitationMessage(int partyId, int fromId, string fromName, int toId)
        {
            this.partyId = partyId;
            this.fromId = fromId;
            this.fromName = fromName;
            this.toId = toId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(partyId);
            writer.WriteInt(fromId);
            writer.WriteUTF(fromName);
            writer.WriteInt(toId);
            

}

public override void Deserialize(IDataReader reader)
{

partyId = reader.ReadInt();
            if (partyId < 0)
                throw new Exception("Forbidden value on partyId = " + partyId + ", it doesn't respect the following condition : partyId < 0");
            fromId = reader.ReadInt();
            if (fromId < 0)
                throw new Exception("Forbidden value on fromId = " + fromId + ", it doesn't respect the following condition : fromId < 0");
            fromName = reader.ReadUTF();
            toId = reader.ReadInt();
            if (toId < 0)
                throw new Exception("Forbidden value on toId = " + toId + ", it doesn't respect the following condition : toId < 0");
            

}


}


}
