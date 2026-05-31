
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class DungeonLeftMessage : Message
{

public const uint Id = 6149;
public override uint MessageId
{
    get { return Id; }
}

public int dungeonId;
        

public DungeonLeftMessage()
{
}

public DungeonLeftMessage(int dungeonId)
        {
            this.dungeonId = dungeonId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(dungeonId);
            

}

public override void Deserialize(IDataReader reader)
{

dungeonId = reader.ReadInt();
            if (dungeonId < 0)
                throw new Exception("Forbidden value on dungeonId = " + dungeonId + ", it doesn't respect the following condition : dungeonId < 0");
            

}


}


}
