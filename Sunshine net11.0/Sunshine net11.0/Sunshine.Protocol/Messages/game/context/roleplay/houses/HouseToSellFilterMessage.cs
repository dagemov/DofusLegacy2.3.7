
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class HouseToSellFilterMessage : Message
{

public const uint Id = 6137;
public override uint MessageId
{
    get { return Id; }
}

public int areaId;
        public sbyte atLeastNbRoom;
        public sbyte atLeastNbChest;
        public sbyte skillRequested;
        public int maxPrice;
        

public HouseToSellFilterMessage()
{
}

public HouseToSellFilterMessage(int areaId, sbyte atLeastNbRoom, sbyte atLeastNbChest, sbyte skillRequested, int maxPrice)
        {
            this.areaId = areaId;
            this.atLeastNbRoom = atLeastNbRoom;
            this.atLeastNbChest = atLeastNbChest;
            this.skillRequested = skillRequested;
            this.maxPrice = maxPrice;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(areaId);
            writer.WriteSByte(atLeastNbRoom);
            writer.WriteSByte(atLeastNbChest);
            writer.WriteSByte(skillRequested);
            writer.WriteInt(maxPrice);
            

}

public override void Deserialize(IDataReader reader)
{

areaId = reader.ReadInt();
            atLeastNbRoom = reader.ReadSByte();
            atLeastNbChest = reader.ReadSByte();
            skillRequested = reader.ReadSByte();
            maxPrice = reader.ReadInt();
            if (maxPrice < 0)
                throw new Exception("Forbidden value on maxPrice = " + maxPrice + ", it doesn't respect the following condition : maxPrice < 0");
            

}


}


}
