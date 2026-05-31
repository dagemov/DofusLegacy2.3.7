
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeShopStockMovementRemovedMessage : Message
{

public const uint Id = 5907;
public override uint MessageId
{
    get { return Id; }
}

public int objectId;
        

public ExchangeShopStockMovementRemovedMessage()
{
}

public ExchangeShopStockMovementRemovedMessage(int objectId)
        {
            this.objectId = objectId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(objectId);
            

}

public override void Deserialize(IDataReader reader)
{

objectId = reader.ReadInt();
            if (objectId < 0)
                throw new Exception("Forbidden value on objectId = " + objectId + ", it doesn't respect the following condition : objectId < 0");
            

}


}


}
