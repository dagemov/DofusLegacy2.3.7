
















// Generated on 10/13/2017 02:18:52
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PaddockPropertiesMessage : Message
{

public const uint Id = 5824;
public override uint MessageId
{
    get { return Id; }
}

public Types.PaddockInformations properties;
        

public PaddockPropertiesMessage()
{
}

public PaddockPropertiesMessage(Types.PaddockInformations properties)
        {
            this.properties = properties;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(properties.TypeId);
            properties.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

properties = Types.ProtocolTypeManager.GetInstance<Types.PaddockInformations>(reader.ReadShort());
            properties.Deserialize(reader);
            

}


}


}
