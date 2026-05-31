
















// Generated on 10/13/2017 02:18:41
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class AtlasPointInformationsMessage : Message
{

public const uint Id = 5956;
public override uint MessageId
{
    get { return Id; }
}

public Types.AtlasPointsInformations type;
        

public AtlasPointInformationsMessage()
{
}

public AtlasPointInformationsMessage(Types.AtlasPointsInformations type)
        {
            this.type = type;
        }
        

public override void Serialize(IDataWriter writer)
{

type.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

type = new Types.AtlasPointsInformations();
            type.Deserialize(reader);
            

}


}


}
