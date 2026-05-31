
















// Generated on 10/13/2017 02:18:57
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class TaxCollectorFireRequestMessage : Message
{

public const uint Id = 5682;
public override uint MessageId
{
    get { return Id; }
}

public int collectorId;
        

public TaxCollectorFireRequestMessage()
{
}

public TaxCollectorFireRequestMessage(int collectorId)
        {
            this.collectorId = collectorId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(collectorId);
            

}

public override void Deserialize(IDataReader reader)
{

collectorId = reader.ReadInt();
            

}


}


}
