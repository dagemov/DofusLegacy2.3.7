
















// Generated on 10/13/2017 02:18:45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameFightLeaveMessage : Message
{

public const uint Id = 721;
public override uint MessageId
{
    get { return Id; }
}

public int charId;
        

public GameFightLeaveMessage()
{
}

public GameFightLeaveMessage(int charId)
        {
            this.charId = charId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(charId);
            

}

public override void Deserialize(IDataReader reader)
{

charId = reader.ReadInt();
            

}


}


}
