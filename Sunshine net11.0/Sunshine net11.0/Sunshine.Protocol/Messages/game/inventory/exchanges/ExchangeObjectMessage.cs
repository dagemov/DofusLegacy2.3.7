
















// Generated on 10/13/2017 02:19:00
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeObjectMessage : Message
{

public const uint Id = 5515;
public override uint MessageId
{
    get { return Id; }
}

public bool remote;
        

public ExchangeObjectMessage()
{
}

public ExchangeObjectMessage(bool remote)
        {
            this.remote = remote;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(remote);
            

}

public override void Deserialize(IDataReader reader)
{

remote = reader.ReadBoolean();
            

}


}


}
