
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeRequestMessage : Message
{

public const uint Id = 5505;
public override uint MessageId
{
    get { return Id; }
}

public sbyte exchangeType;
        

public ExchangeRequestMessage()
{
}

public ExchangeRequestMessage(sbyte exchangeType)
        {
            this.exchangeType = exchangeType;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(exchangeType);
            

}

public override void Deserialize(IDataReader reader)
{

exchangeType = reader.ReadSByte();
            

}


}


}
