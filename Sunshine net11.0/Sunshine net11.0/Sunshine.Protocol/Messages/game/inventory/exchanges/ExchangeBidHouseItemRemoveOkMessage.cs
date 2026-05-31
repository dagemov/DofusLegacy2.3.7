
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeBidHouseItemRemoveOkMessage : Message
{

public const uint Id = 5946;
public override uint MessageId
{
    get { return Id; }
}

public int sellerId;
        

public ExchangeBidHouseItemRemoveOkMessage()
{
}

public ExchangeBidHouseItemRemoveOkMessage(int sellerId)
        {
            this.sellerId = sellerId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(sellerId);
            

}

public override void Deserialize(IDataReader reader)
{

sellerId = reader.ReadInt();
            

}


}


}
