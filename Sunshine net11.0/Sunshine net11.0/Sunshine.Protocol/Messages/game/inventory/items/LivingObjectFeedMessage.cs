
















// Generated on 10/13/2017 02:19:03
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class LivingObjectFeedMessage : Message
{

public const uint Id = 5724;
public override uint MessageId
{
    get { return Id; }
}

public int livingUID;
        public byte livingPosition;
        public int foodUID;
        

public LivingObjectFeedMessage()
{
}

public LivingObjectFeedMessage(int livingUID, byte livingPosition, int foodUID)
        {
            this.livingUID = livingUID;
            this.livingPosition = livingPosition;
            this.foodUID = foodUID;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(livingUID);
            writer.WriteByte(livingPosition);
            writer.WriteInt(foodUID);
            

}

public override void Deserialize(IDataReader reader)
{

livingUID = reader.ReadInt();
            if (livingUID < 0)
                throw new Exception("Forbidden value on livingUID = " + livingUID + ", it doesn't respect the following condition : livingUID < 0");
            livingPosition = reader.ReadByte();
            if (livingPosition < 0 || livingPosition > 255)
                throw new Exception("Forbidden value on livingPosition = " + livingPosition + ", it doesn't respect the following condition : livingPosition < 0 || livingPosition > 255");
            foodUID = reader.ReadInt();
            if (foodUID < 0)
                throw new Exception("Forbidden value on foodUID = " + foodUID + ", it doesn't respect the following condition : foodUID < 0");
            

}


}


}
