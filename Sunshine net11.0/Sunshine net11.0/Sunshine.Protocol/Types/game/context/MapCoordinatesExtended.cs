
















// Generated on 10/13/2017 02:17:22
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class MapCoordinatesExtended : MapCoordinates
{

public const short Id = 176;
public override short TypeId
{
    get { return Id; }
}

public int mapId;
        

public MapCoordinatesExtended()
{
}

public MapCoordinatesExtended(short worldX, short worldY, int mapId)
         : base(worldX, worldY)
        {
            this.mapId = mapId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(mapId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            mapId = reader.ReadInt();
            

}



}


}
