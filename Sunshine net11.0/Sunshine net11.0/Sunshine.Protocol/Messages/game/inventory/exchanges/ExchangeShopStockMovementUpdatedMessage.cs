
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeShopStockMovementUpdatedMessage : Message
{

public const uint Id = 5909;
public override uint MessageId
{
    get { return Id; }
}

public Types.ObjectItemToSell objectInfo;
        

public ExchangeShopStockMovementUpdatedMessage()
{
}

public ExchangeShopStockMovementUpdatedMessage(Types.ObjectItemToSell objectInfo)
        {
            this.objectInfo = objectInfo;
        }
        

public override void Serialize(IDataWriter writer)
{

objectInfo.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

objectInfo = new Types.ObjectItemToSell();
            objectInfo.Deserialize(reader);
            

}


}


}
