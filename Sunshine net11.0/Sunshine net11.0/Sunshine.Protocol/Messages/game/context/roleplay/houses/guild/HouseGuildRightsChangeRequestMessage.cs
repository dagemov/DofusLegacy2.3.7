
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class HouseGuildRightsChangeRequestMessage : Message
{

public const uint Id = 5702;
public override uint MessageId
{
    get { return Id; }
}

public uint rights;
        

public HouseGuildRightsChangeRequestMessage()
{
}

public HouseGuildRightsChangeRequestMessage(uint rights)
        {
            this.rights = rights;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUInt(rights);
            

}

public override void Deserialize(IDataReader reader)
{

rights = reader.ReadUInt();
            if (rights < 0 || rights > 4294967295)
                throw new Exception("Forbidden value on rights = " + rights + ", it doesn't respect the following condition : rights < 0 || rights > 4294967295");
            

}


}


}
