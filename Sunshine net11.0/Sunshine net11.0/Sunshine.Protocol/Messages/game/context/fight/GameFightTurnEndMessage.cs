
















// Generated on 10/13/2017 02:18:46
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameFightTurnEndMessage : Message
{

public const uint Id = 719;
public override uint MessageId
{
    get { return Id; }
}

public int id;
        

public GameFightTurnEndMessage()
{
}

public GameFightTurnEndMessage(int id)
        {
            this.id = id;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(id);
            

}

public override void Deserialize(IDataReader reader)
{

id = reader.ReadInt();
            

}


}


}
