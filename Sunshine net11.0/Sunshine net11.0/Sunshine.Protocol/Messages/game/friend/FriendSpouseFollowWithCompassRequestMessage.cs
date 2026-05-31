
















// Generated on 10/13/2017 02:18:55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class FriendSpouseFollowWithCompassRequestMessage : Message
{

public const uint Id = 5606;
public override uint MessageId
{
    get { return Id; }
}

public bool enable;
        

public FriendSpouseFollowWithCompassRequestMessage()
{
}

public FriendSpouseFollowWithCompassRequestMessage(bool enable)
        {
            this.enable = enable;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(enable);
            

}

public override void Deserialize(IDataReader reader)
{

enable = reader.ReadBoolean();
            

}


}


}
