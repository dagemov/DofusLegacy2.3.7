
















// Generated on 10/13/2017 02:17:25
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class PartyMemberGeoPosition
{

public const short Id = 378;
public virtual short TypeId
{
    get { return Id; }
}

public int memberId;
        public short worldX;
        public short worldY;
        public int mapId;
        

public PartyMemberGeoPosition()
{
}

public PartyMemberGeoPosition(int memberId, short worldX, short worldY, int mapId)
        {
            this.memberId = memberId;
            this.worldX = worldX;
            this.worldY = worldY;
            this.mapId = mapId;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteInt(memberId);
            writer.WriteShort(worldX);
            writer.WriteShort(worldY);
            writer.WriteInt(mapId);
            

}

public virtual void Deserialize(IDataReader reader)
{

memberId = reader.ReadInt();
            if (memberId < 0)
                throw new Exception("Forbidden value on memberId = " + memberId + ", it doesn't respect the following condition : memberId < 0");
            worldX = reader.ReadShort();
            if (worldX < -255 || worldX > 255)
                throw new Exception("Forbidden value on worldX = " + worldX + ", it doesn't respect the following condition : worldX < -255 || worldX > 255");
            worldY = reader.ReadShort();
            if (worldY < -255 || worldY > 255)
                throw new Exception("Forbidden value on worldY = " + worldY + ", it doesn't respect the following condition : worldY < -255 || worldY > 255");
            mapId = reader.ReadInt();
            

}



}


}
