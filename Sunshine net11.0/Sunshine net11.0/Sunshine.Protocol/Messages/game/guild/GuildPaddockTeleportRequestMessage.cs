
















// Generated on 10/13/2017 02:18:57
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildPaddockTeleportRequestMessage : Message
{

public const uint Id = 5957;
public override uint MessageId
{
    get { return Id; }
}

public int paddockId;
        

public GuildPaddockTeleportRequestMessage()
{
}

public GuildPaddockTeleportRequestMessage(int paddockId)
        {
            this.paddockId = paddockId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(paddockId);
            

}

public override void Deserialize(IDataReader reader)
{

paddockId = reader.ReadInt();
            

}


}


}
