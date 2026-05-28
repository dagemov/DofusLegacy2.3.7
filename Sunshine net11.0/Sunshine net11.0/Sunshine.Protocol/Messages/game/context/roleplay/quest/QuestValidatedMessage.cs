
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class QuestValidatedMessage : Message
{

public const uint Id = 6097;
public override uint MessageId
{
    get { return Id; }
}

public ushort questId;
        

public QuestValidatedMessage()
{
}

public QuestValidatedMessage(ushort questId)
        {
            this.questId = questId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUShort(questId);
            

}

public override void Deserialize(IDataReader reader)
{

questId = reader.ReadUShort();
            if (questId < 0 || questId > 65535)
                throw new Exception("Forbidden value on questId = " + questId + ", it doesn't respect the following condition : questId < 0 || questId > 65535");
            

}


}


}
