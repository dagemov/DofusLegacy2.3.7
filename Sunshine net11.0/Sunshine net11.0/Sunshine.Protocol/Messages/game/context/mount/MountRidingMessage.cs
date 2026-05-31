
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MountRidingMessage : Message
{

public const uint Id = 5967;
public override uint MessageId
{
    get { return Id; }
}

public bool isRiding;
        

public MountRidingMessage()
{
}

public MountRidingMessage(bool isRiding)
        {
            this.isRiding = isRiding;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(isRiding);
            

}

public override void Deserialize(IDataReader reader)
{

isRiding = reader.ReadBoolean();
            

}


}


}
