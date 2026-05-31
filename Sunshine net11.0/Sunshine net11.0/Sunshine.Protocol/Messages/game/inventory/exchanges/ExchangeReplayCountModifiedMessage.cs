
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeReplayCountModifiedMessage : Message
{

public const uint Id = 6023;
public override uint MessageId
{
    get { return Id; }
}

public int count;
        

public ExchangeReplayCountModifiedMessage()
{
}

public ExchangeReplayCountModifiedMessage(int count)
        {
            this.count = count;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(count);
            

}

public override void Deserialize(IDataReader reader)
{

count = reader.ReadInt();
            

}


}


}
