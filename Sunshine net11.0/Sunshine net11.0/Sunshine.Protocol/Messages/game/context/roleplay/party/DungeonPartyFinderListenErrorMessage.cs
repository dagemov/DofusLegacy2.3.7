
















// Generated on 10/13/2017 02:18:52
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class DungeonPartyFinderListenErrorMessage : Message
{

public const uint Id = 6248;
public override uint MessageId
{
    get { return Id; }
}

public short dungeonId;
        

public DungeonPartyFinderListenErrorMessage()
{
}

public DungeonPartyFinderListenErrorMessage(short dungeonId)
        {
            this.dungeonId = dungeonId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(dungeonId);
            

}

public override void Deserialize(IDataReader reader)
{

dungeonId = reader.ReadShort();
            if (dungeonId < 0)
                throw new Exception("Forbidden value on dungeonId = " + dungeonId + ", it doesn't respect the following condition : dungeonId < 0");
            

}


}


}
