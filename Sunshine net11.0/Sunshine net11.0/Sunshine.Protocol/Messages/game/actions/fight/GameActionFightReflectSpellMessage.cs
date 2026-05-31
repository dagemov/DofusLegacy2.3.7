
















// Generated on 10/13/2017 02:18:40
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionFightReflectSpellMessage : AbstractGameActionMessage
{

public const uint Id = 5531;
public override uint MessageId
{
    get { return Id; }
}

public int targetId;
        

public GameActionFightReflectSpellMessage()
{
}

public GameActionFightReflectSpellMessage(short actionId, int sourceId, int targetId)
         : base(actionId, sourceId)
        {
            this.targetId = targetId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(targetId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            targetId = reader.ReadInt();
            

}


}


}
