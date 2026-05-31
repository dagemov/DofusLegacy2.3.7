
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class HouseSellFromInsideRequestMessage : HouseSellRequestMessage
{

public const uint Id = 5884;
public override uint MessageId
{
    get { return Id; }
}



public HouseSellFromInsideRequestMessage()
{
}

public HouseSellFromInsideRequestMessage(int amount)
         : base(amount)
        {
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            

}


}


}
