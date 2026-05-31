
















// Generated on 10/13/2017 02:18:55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class FriendAddedMessage : Message
{

public const uint Id = 5599;
public override uint MessageId
{
    get { return Id; }
}

public Types.FriendInformations friendAdded;
        

public FriendAddedMessage()
{
}

public FriendAddedMessage(Types.FriendInformations friendAdded)
        {
            this.friendAdded = friendAdded;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(friendAdded.TypeId);
            friendAdded.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

friendAdded = Types.ProtocolTypeManager.GetInstance<Types.FriendInformations>(reader.ReadShort());
            friendAdded.Deserialize(reader);
            

}


}


}
