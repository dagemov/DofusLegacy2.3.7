
















// Generated on 10/13/2017 02:19:03
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeMultiCraftSetCrafterCanUseHisRessourcesMessage : Message
{

public const uint Id = 6021;
public override uint MessageId
{
    get { return Id; }
}

public bool allow;
        

public ExchangeMultiCraftSetCrafterCanUseHisRessourcesMessage()
{
}

public ExchangeMultiCraftSetCrafterCanUseHisRessourcesMessage(bool allow)
        {
            this.allow = allow;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(allow);
            

}

public override void Deserialize(IDataReader reader)
{

allow = reader.ReadBoolean();
            

}


}


}
