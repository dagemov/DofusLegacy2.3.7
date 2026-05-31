
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeBidHouseItemAddOkMessage : Message
{

public const uint Id = 5945;
public override uint MessageId
{
    get { return Id; }
}

public Types.ObjectItemToSellInBid itemInfo;
        

public ExchangeBidHouseItemAddOkMessage()
{
}

public ExchangeBidHouseItemAddOkMessage(Types.ObjectItemToSellInBid itemInfo)
        {
            this.itemInfo = itemInfo;
        }
        

public override void Serialize(IDataWriter writer)
{

itemInfo.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

itemInfo = new Types.ObjectItemToSellInBid();
            itemInfo.Deserialize(reader);
            

}


}


}
