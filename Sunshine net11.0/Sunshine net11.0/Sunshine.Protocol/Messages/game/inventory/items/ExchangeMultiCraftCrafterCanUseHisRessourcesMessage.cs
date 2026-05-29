
















// Generated on 10/13/2017 02:19:03
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeMultiCraftCrafterCanUseHisRessourcesMessage : Message
{

public const uint Id = 6020;
public override uint MessageId
{
    get { return Id; }
}

public bool allowed;
        

public ExchangeMultiCraftCrafterCanUseHisRessourcesMessage()
{
}

public ExchangeMultiCraftCrafterCanUseHisRessourcesMessage(bool allowed)
        {
            this.allowed = allowed;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(allowed);
            

}

public override void Deserialize(IDataReader reader)
{

allowed = reader.ReadBoolean();
            

}


}


}
