
















// Generated on 10/13/2017 02:19:02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeWaitingResultMessage : Message
{

public const uint Id = 5786;
public override uint MessageId
{
    get { return Id; }
}

public bool bwait;
        

public ExchangeWaitingResultMessage()
{
}

public ExchangeWaitingResultMessage(bool bwait)
        {
            this.bwait = bwait;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(bwait);
            

}

public override void Deserialize(IDataReader reader)
{

bwait = reader.ReadBoolean();
            

}


}


}
