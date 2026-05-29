
















// Generated on 10/13/2017 02:19:00
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeMountStableRemoveMessage : Message
{

public const uint Id = 5964;
public override uint MessageId
{
    get { return Id; }
}

public double mountId;
        

public ExchangeMountStableRemoveMessage()
{
}

public ExchangeMountStableRemoveMessage(double mountId)
        {
            this.mountId = mountId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteDouble(mountId);
            

}

public override void Deserialize(IDataReader reader)
{

mountId = reader.ReadDouble();
            

}


}


}
