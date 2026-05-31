
















// Generated on 10/13/2017 02:18:57
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildPaddockRemovedMessage : Message
{

public const uint Id = 5955;
public override uint MessageId
{
    get { return Id; }
}

public short worldX;
        public short worldY;
        

public GuildPaddockRemovedMessage()
{
}

public GuildPaddockRemovedMessage(short worldX, short worldY)
        {
            this.worldX = worldX;
            this.worldY = worldY;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(worldX);
            writer.WriteShort(worldY);
            

}

public override void Deserialize(IDataReader reader)
{

worldX = reader.ReadShort();
            if (worldX < -255 || worldX > 255)
                throw new Exception("Forbidden value on worldX = " + worldX + ", it doesn't respect the following condition : worldX < -255 || worldX > 255");
            worldY = reader.ReadShort();
            if (worldY < -255 || worldY > 255)
                throw new Exception("Forbidden value on worldY = " + worldY + ", it doesn't respect the following condition : worldY < -255 || worldY > 255");
            

}


}


}
