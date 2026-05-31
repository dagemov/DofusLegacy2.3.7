
















// Generated on 10/13/2017 02:18:45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameMapChangeOrientationRequestMessage : Message
{

public const uint Id = 945;
public override uint MessageId
{
    get { return Id; }
}

public sbyte direction;
        

public GameMapChangeOrientationRequestMessage()
{
}

public GameMapChangeOrientationRequestMessage(sbyte direction)
        {
            this.direction = direction;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(direction);
            

}

public override void Deserialize(IDataReader reader)
{

direction = reader.ReadSByte();
            if (direction < 0)
                throw new Exception("Forbidden value on direction = " + direction + ", it doesn't respect the following condition : direction < 0");
            

}


}


}
