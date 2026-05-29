
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class JobAllowMultiCraftRequestSetMessage : Message
{

public const uint Id = 5749;
public override uint MessageId
{
    get { return Id; }
}

public bool enabled;
        

public JobAllowMultiCraftRequestSetMessage()
{
}

public JobAllowMultiCraftRequestSetMessage(bool enabled)
        {
            this.enabled = enabled;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(enabled);
            

}

public override void Deserialize(IDataReader reader)
{

enabled = reader.ReadBoolean();
            

}


}


}
