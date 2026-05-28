
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeBidHouseInListRemovedMessage : Message
{

public const uint Id = 5950;
public override uint MessageId
{
    get { return Id; }
}

public int itemUID;
        

public ExchangeBidHouseInListRemovedMessage()
{
}

public ExchangeBidHouseInListRemovedMessage(int itemUID)
        {
            this.itemUID = itemUID;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(itemUID);
            

}

public override void Deserialize(IDataReader reader)
{

itemUID = reader.ReadInt();
            

}


}


}
