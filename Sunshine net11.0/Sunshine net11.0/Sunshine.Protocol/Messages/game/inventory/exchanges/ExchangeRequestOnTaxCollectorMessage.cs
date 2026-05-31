
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeRequestOnTaxCollectorMessage : Message
{

public const uint Id = 5779;
public override uint MessageId
{
    get { return Id; }
}

public int taxCollectorId;
        

public ExchangeRequestOnTaxCollectorMessage()
{
}

public ExchangeRequestOnTaxCollectorMessage(int taxCollectorId)
        {
            this.taxCollectorId = taxCollectorId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(taxCollectorId);
            

}

public override void Deserialize(IDataReader reader)
{

taxCollectorId = reader.ReadInt();
            

}


}


}
