
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class TeleportRequestMessage : Message
{

public const uint Id = 5961;
public override uint MessageId
{
    get { return Id; }
}

public sbyte teleporterType;
        public int mapId;
        

public TeleportRequestMessage()
{
}

public TeleportRequestMessage(sbyte teleporterType, int mapId)
        {
            this.teleporterType = teleporterType;
            this.mapId = mapId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(teleporterType);
            writer.WriteInt(mapId);
            

}

public override void Deserialize(IDataReader reader)
{

teleporterType = reader.ReadSByte();
            if (teleporterType < 0)
                throw new Exception("Forbidden value on teleporterType = " + teleporterType + ", it doesn't respect the following condition : teleporterType < 0");
            mapId = reader.ReadInt();
            if (mapId < 0)
                throw new Exception("Forbidden value on mapId = " + mapId + ", it doesn't respect the following condition : mapId < 0");
            

}


}


}
