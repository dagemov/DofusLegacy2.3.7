
















// Generated on 10/13/2017 02:18:59
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeClearPaymentForCraftMessage : Message
{

public const uint Id = 6145;
public override uint MessageId
{
    get { return Id; }
}

public sbyte paymentType;
        

public ExchangeClearPaymentForCraftMessage()
{
}

public ExchangeClearPaymentForCraftMessage(sbyte paymentType)
        {
            this.paymentType = paymentType;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(paymentType);
            

}

public override void Deserialize(IDataReader reader)
{

paymentType = reader.ReadSByte();
            

}


}


}
