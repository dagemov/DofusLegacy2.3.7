
















// Generated on 10/13/2017 02:19:03
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ObjectDeleteMessage : Message
{

public const uint Id = 3022;
public override uint MessageId
{
    get { return Id; }
}

public int objectUID;
        public int quantity;
        

public ObjectDeleteMessage()
{
}

public ObjectDeleteMessage(int objectUID, int quantity)
        {
            this.objectUID = objectUID;
            this.quantity = quantity;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(objectUID);
            writer.WriteInt(quantity);
            

}

public override void Deserialize(IDataReader reader)
{

objectUID = reader.ReadInt();
            if (objectUID < 0)
                throw new Exception("Forbidden value on objectUID = " + objectUID + ", it doesn't respect the following condition : objectUID < 0");
            quantity = reader.ReadInt();
            if (quantity < 0)
                throw new Exception("Forbidden value on quantity = " + quantity + ", it doesn't respect the following condition : quantity < 0");
            

}


}


}
