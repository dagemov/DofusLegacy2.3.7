
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyInvitationRequestMessage : Message
{

public const uint Id = 5585;
public override uint MessageId
{
    get { return Id; }
}

public string name;
        

public PartyInvitationRequestMessage()
{
}

public PartyInvitationRequestMessage(string name)
        {
            this.name = name;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(name);
            

}

public override void Deserialize(IDataReader reader)
{

name = reader.ReadUTF();
            

}


}


}
