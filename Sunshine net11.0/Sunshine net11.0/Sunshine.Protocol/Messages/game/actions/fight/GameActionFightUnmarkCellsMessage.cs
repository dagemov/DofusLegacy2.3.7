
















// Generated on 10/13/2017 02:18:41
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionFightUnmarkCellsMessage : AbstractGameActionMessage
{

public const uint Id = 5570;
public override uint MessageId
{
    get { return Id; }
}

public short markId;
        

public GameActionFightUnmarkCellsMessage()
{
}

public GameActionFightUnmarkCellsMessage(short actionId, int sourceId, short markId)
         : base(actionId, sourceId)
        {
            this.markId = markId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteShort(markId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            markId = reader.ReadShort();
            

}


}


}
