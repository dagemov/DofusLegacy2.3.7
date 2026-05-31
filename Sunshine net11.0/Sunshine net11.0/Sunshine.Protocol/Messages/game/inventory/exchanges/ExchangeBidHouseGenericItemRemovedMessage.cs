
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeBidHouseGenericItemRemovedMessage : Message
{

public const uint Id = 5948;
public override uint MessageId
{
    get { return Id; }
}

public int objGenericId;
        

public ExchangeBidHouseGenericItemRemovedMessage()
{
}

public ExchangeBidHouseGenericItemRemovedMessage(int objGenericId)
        {
            this.objGenericId = objGenericId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(objGenericId);
            

}

public override void Deserialize(IDataReader reader)
{

objGenericId = reader.ReadInt();
            

}


}


}
