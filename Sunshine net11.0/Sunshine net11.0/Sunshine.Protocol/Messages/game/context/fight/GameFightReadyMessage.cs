
















// Generated on 10/13/2017 02:18:46
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameFightReadyMessage : Message
{

public const uint Id = 708;
public override uint MessageId
{
    get { return Id; }
}

public bool isReady;
        

public GameFightReadyMessage()
{
}

public GameFightReadyMessage(bool isReady)
        {
            this.isReady = isReady;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(isReady);
            

}

public override void Deserialize(IDataReader reader)
{

isReady = reader.ReadBoolean();
            

}


}


}
