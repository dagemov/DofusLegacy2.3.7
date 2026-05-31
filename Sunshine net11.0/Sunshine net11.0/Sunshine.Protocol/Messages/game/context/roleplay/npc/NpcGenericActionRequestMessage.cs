
















// Generated on 10/13/2017 02:18:51
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class NpcGenericActionRequestMessage : Message
{

public const uint Id = 5898;
public override uint MessageId
{
    get { return Id; }
}

public int npcId;
        public sbyte npcActionId;
        public int npcMapId;
        

public NpcGenericActionRequestMessage()
{
}

public NpcGenericActionRequestMessage(int npcId, sbyte npcActionId, int npcMapId)
        {
            this.npcId = npcId;
            this.npcActionId = npcActionId;
            this.npcMapId = npcMapId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(npcId);
            writer.WriteSByte(npcActionId);
            writer.WriteInt(npcMapId);
            

}

public override void Deserialize(IDataReader reader)
{

npcId = reader.ReadInt();
            npcActionId = reader.ReadSByte();
            if (npcActionId < 0)
                throw new Exception("Forbidden value on npcActionId = " + npcActionId + ", it doesn't respect the following condition : npcActionId < 0");
            npcMapId = reader.ReadInt();
            

}


}


}
