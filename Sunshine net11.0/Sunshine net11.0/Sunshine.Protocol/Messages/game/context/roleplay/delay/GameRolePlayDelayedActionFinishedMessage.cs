
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameRolePlayDelayedActionFinishedMessage : Message
{

public const uint Id = 6150;
public override uint MessageId
{
    get { return Id; }
}

public sbyte delayTypeId;
        

public GameRolePlayDelayedActionFinishedMessage()
{
}

public GameRolePlayDelayedActionFinishedMessage(sbyte delayTypeId)
        {
            this.delayTypeId = delayTypeId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(delayTypeId);
            

}

public override void Deserialize(IDataReader reader)
{

delayTypeId = reader.ReadSByte();
            if (delayTypeId < 0)
                throw new Exception("Forbidden value on delayTypeId = " + delayTypeId + ", it doesn't respect the following condition : delayTypeId < 0");
            

}


}


}
