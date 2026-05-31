
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class HouseBuyRequestMessage : Message
{

public const uint Id = 5738;
public override uint MessageId
{
    get { return Id; }
}

public int proposedPrice;
        

public HouseBuyRequestMessage()
{
}

public HouseBuyRequestMessage(int proposedPrice)
        {
            this.proposedPrice = proposedPrice;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(proposedPrice);
            

}

public override void Deserialize(IDataReader reader)
{

proposedPrice = reader.ReadInt();
            if (proposedPrice < 0)
                throw new Exception("Forbidden value on proposedPrice = " + proposedPrice + ", it doesn't respect the following condition : proposedPrice < 0");
            

}


}


}
