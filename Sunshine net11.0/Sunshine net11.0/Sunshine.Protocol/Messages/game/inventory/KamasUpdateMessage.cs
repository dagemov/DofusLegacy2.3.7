
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class KamasUpdateMessage : Message
{

public const uint Id = 5537;
public override uint MessageId
{
    get { return Id; }
}

public int kamasTotal;
        

public KamasUpdateMessage()
{
}

public KamasUpdateMessage(int kamasTotal)
        {
            this.kamasTotal = kamasTotal;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(kamasTotal);
            

}

public override void Deserialize(IDataReader reader)
{

kamasTotal = reader.ReadInt();
            

}


}


}
