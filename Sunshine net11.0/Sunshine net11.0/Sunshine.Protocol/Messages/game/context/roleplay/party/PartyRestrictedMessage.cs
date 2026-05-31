
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyRestrictedMessage : Message
{

public const uint Id = 6175;
public override uint MessageId
{
    get { return Id; }
}

public bool restricted;
        

public PartyRestrictedMessage()
{
}

public PartyRestrictedMessage(bool restricted)
        {
            this.restricted = restricted;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(restricted);
            

}

public override void Deserialize(IDataReader reader)
{

restricted = reader.ReadBoolean();
            

}


}


}
