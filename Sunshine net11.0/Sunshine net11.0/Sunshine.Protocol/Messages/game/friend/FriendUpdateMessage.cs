
















// Generated on 10/13/2017 02:18:55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class FriendUpdateMessage : Message
{

public const uint Id = 5924;
public override uint MessageId
{
    get { return Id; }
}

public Types.FriendInformations friendUpdated;
        

public FriendUpdateMessage()
{
}

public FriendUpdateMessage(Types.FriendInformations friendUpdated)
        {
            this.friendUpdated = friendUpdated;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(friendUpdated.TypeId);
            friendUpdated.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

friendUpdated = Types.ProtocolTypeManager.GetInstance<Types.FriendInformations>(reader.ReadShort());
            friendUpdated.Deserialize(reader);
            

}


}


}
