
















// Generated on 10/13/2017 02:19:05
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class StorageObjectRemoveMessage : Message
{

public const uint Id = 5648;
public override uint MessageId
{
    get { return Id; }
}

public int objectUID;
        

public StorageObjectRemoveMessage()
{
}

public StorageObjectRemoveMessage(int objectUID)
        {
            this.objectUID = objectUID;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(objectUID);
            

}

public override void Deserialize(IDataReader reader)
{

objectUID = reader.ReadInt();
            if (objectUID < 0)
                throw new Exception("Forbidden value on objectUID = " + objectUID + ", it doesn't respect the following condition : objectUID < 0");
            

}


}


}
