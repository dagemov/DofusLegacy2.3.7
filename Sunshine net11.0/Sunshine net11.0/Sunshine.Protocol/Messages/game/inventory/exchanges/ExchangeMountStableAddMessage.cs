
















// Generated on 10/13/2017 02:19:00
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeMountStableAddMessage : Message
{

public const uint Id = 5971;
public override uint MessageId
{
    get { return Id; }
}

public Types.MountClientData mountDescription;
        

public ExchangeMountStableAddMessage()
{
}

public ExchangeMountStableAddMessage(Types.MountClientData mountDescription)
        {
            this.mountDescription = mountDescription;
        }
        

public override void Serialize(IDataWriter writer)
{

mountDescription.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

mountDescription = new Types.MountClientData();
            mountDescription.Deserialize(reader);
            

}


}


}
