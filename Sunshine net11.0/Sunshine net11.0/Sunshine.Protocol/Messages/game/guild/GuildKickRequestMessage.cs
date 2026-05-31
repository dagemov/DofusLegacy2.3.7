
















// Generated on 10/13/2017 02:18:57
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildKickRequestMessage : Message
{

public const uint Id = 5887;
public override uint MessageId
{
    get { return Id; }
}

public int kickedId;
        

public GuildKickRequestMessage()
{
}

public GuildKickRequestMessage(int kickedId)
        {
            this.kickedId = kickedId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(kickedId);
            

}

public override void Deserialize(IDataReader reader)
{

kickedId = reader.ReadInt();
            if (kickedId < 0)
                throw new Exception("Forbidden value on kickedId = " + kickedId + ", it doesn't respect the following condition : kickedId < 0");
            

}


}


}
