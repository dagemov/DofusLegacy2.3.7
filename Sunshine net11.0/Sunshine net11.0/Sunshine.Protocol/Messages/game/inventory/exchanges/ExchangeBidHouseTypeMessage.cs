
















// Generated on 10/13/2017 02:18:59
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeBidHouseTypeMessage : Message
{

public const uint Id = 5803;
public override uint MessageId
{
    get { return Id; }
}

public int type;
        

public ExchangeBidHouseTypeMessage()
{
}

public ExchangeBidHouseTypeMessage(int type)
        {
            this.type = type;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(type);
            

}

public override void Deserialize(IDataReader reader)
{

type = reader.ReadInt();
            if (type < 0)
                throw new Exception("Forbidden value on type = " + type + ", it doesn't respect the following condition : type < 0");
            

}


}


}
