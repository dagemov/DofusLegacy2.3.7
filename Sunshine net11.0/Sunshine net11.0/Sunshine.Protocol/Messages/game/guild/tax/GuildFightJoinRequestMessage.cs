
















// Generated on 10/13/2017 02:18:57
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildFightJoinRequestMessage : Message
{

public const uint Id = 5717;
public override uint MessageId
{
    get { return Id; }
}

public int taxCollectorId;
        

public GuildFightJoinRequestMessage()
{
}

public GuildFightJoinRequestMessage(int taxCollectorId)
        {
            this.taxCollectorId = taxCollectorId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(taxCollectorId);
            

}

public override void Deserialize(IDataReader reader)
{

taxCollectorId = reader.ReadInt();
            

}


}


}
