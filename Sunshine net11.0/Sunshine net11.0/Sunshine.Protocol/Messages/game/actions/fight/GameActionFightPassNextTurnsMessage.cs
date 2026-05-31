
















// Generated on 10/13/2017 02:18:40
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionFightPassNextTurnsMessage : AbstractGameActionMessage
{

public const uint Id = 5529;
public override uint MessageId
{
    get { return Id; }
}

public int targetId;
        public sbyte turnCount;
        

public GameActionFightPassNextTurnsMessage()
{
}

public GameActionFightPassNextTurnsMessage(short actionId, int sourceId, int targetId, sbyte turnCount)
         : base(actionId, sourceId)
        {
            this.targetId = targetId;
            this.turnCount = turnCount;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(targetId);
            writer.WriteSByte(turnCount);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            targetId = reader.ReadInt();
            turnCount = reader.ReadSByte();
            if (turnCount < 0)
                throw new Exception("Forbidden value on turnCount = " + turnCount + ", it doesn't respect the following condition : turnCount < 0");
            

}


}


}
