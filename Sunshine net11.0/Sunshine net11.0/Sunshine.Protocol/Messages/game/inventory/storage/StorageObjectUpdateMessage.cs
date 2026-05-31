
















// Generated on 10/13/2017 02:19:05
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class StorageObjectUpdateMessage : Message
{

public const uint Id = 5647;
public override uint MessageId
{
    get { return Id; }
}

public Types.ObjectItem @object;
        

public StorageObjectUpdateMessage()
{
}

public StorageObjectUpdateMessage(Types.ObjectItem @object)
        {
            this.@object = @object;
        }
        

public override void Serialize(IDataWriter writer)
{

@object.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

@object = new Types.ObjectItem();
            @object.Deserialize(reader);
            

}


}


}
