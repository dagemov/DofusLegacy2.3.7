
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeReadyMessage : Message
{

public const uint Id = 5511;
public override uint MessageId
{
    get { return Id; }
}

public bool ready;
        

public ExchangeReadyMessage()
{
}

public ExchangeReadyMessage(bool ready)
        {
            this.ready = ready;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(ready);
            

}

public override void Deserialize(IDataReader reader)
{

ready = reader.ReadBoolean();
            

}


}


}
