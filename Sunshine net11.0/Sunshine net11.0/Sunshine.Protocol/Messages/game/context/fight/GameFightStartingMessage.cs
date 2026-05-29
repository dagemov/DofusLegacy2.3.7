
















// Generated on 10/13/2017 02:18:46
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameFightStartingMessage : Message
{

public const uint Id = 700;
public override uint MessageId
{
    get { return Id; }
}

public sbyte fightType;
        

public GameFightStartingMessage()
{
}

public GameFightStartingMessage(sbyte fightType)
        {
            this.fightType = fightType;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(fightType);
            

}

public override void Deserialize(IDataReader reader)
{

fightType = reader.ReadSByte();
            if (fightType < 0)
                throw new Exception("Forbidden value on fightType = " + fightType + ", it doesn't respect the following condition : fightType < 0");
            

}


}


}
