
















// Generated on 10/13/2017 02:18:48
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MapFightCountMessage : Message
{

public const uint Id = 210;
public override uint MessageId
{
    get { return Id; }
}

public short fightCount;
        

public MapFightCountMessage()
{
}

public MapFightCountMessage(short fightCount)
        {
            this.fightCount = fightCount;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(fightCount);
            

}

public override void Deserialize(IDataReader reader)
{

fightCount = reader.ReadShort();
            if (fightCount < 0)
                throw new Exception("Forbidden value on fightCount = " + fightCount + ", it doesn't respect the following condition : fightCount < 0");
            

}


}


}
