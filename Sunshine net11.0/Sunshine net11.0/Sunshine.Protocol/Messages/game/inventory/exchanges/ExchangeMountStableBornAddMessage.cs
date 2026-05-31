
















// Generated on 10/13/2017 02:19:00
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeMountStableBornAddMessage : ExchangeMountStableAddMessage
{

public const uint Id = 5966;
public override uint MessageId
{
    get { return Id; }
}



public ExchangeMountStableBornAddMessage()
{
}

public ExchangeMountStableBornAddMessage(Types.MountClientData mountDescription)
         : base(mountDescription)
        {
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            

}


}


}
