
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PVPActivationCostMessage : Message
{

public const uint Id = 1801;
public override uint MessageId
{
    get { return Id; }
}

public short cost;
        

public PVPActivationCostMessage()
{
}

public PVPActivationCostMessage(short cost)
        {
            this.cost = cost;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(cost);
            

}

public override void Deserialize(IDataReader reader)
{

cost = reader.ReadShort();
            if (cost < 0)
                throw new Exception("Forbidden value on cost = " + cost + ", it doesn't respect the following condition : cost < 0");
            

}


}


}
