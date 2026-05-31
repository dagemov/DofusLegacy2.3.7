
















// Generated on 10/13/2017 02:19:00
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeObjectModifyPricedMessage : ExchangeObjectMovePricedMessage
{

public const uint Id = 6238;
public override uint MessageId
{
    get { return Id; }
}



public ExchangeObjectModifyPricedMessage()
{
}

public ExchangeObjectModifyPricedMessage(int objectUID, int quantity, int price)
         : base(objectUID, quantity, price)
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
