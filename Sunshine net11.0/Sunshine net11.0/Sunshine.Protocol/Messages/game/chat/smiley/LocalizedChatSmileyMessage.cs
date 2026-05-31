
















// Generated on 10/13/2017 02:18:44
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class LocalizedChatSmileyMessage : ChatSmileyMessage
{

public const uint Id = 6185;
public override uint MessageId
{
    get { return Id; }
}

public short cellId;
        

public LocalizedChatSmileyMessage()
{
}

public LocalizedChatSmileyMessage(int entityId, sbyte smileyId, int accountId, short cellId)
         : base(entityId, smileyId, accountId)
        {
            this.cellId = cellId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteShort(cellId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            cellId = reader.ReadShort();
            if (cellId < 0 || cellId > 559)
                throw new Exception("Forbidden value on cellId = " + cellId + ", it doesn't respect the following condition : cellId < 0 || cellId > 559");
            

}


}


}
