
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ZaapListMessage : TeleportDestinationsListMessage
{

public const uint Id = 1604;
public override uint MessageId
{
    get { return Id; }
}

public int spawnMapId;
        

public ZaapListMessage()
{
}

public ZaapListMessage(sbyte teleporterType, IEnumerable<int> mapIds, IEnumerable<short> subareaIds, IEnumerable<short> costs, int spawnMapId)
         : base(teleporterType, mapIds, subareaIds, costs)
        {
            this.spawnMapId = spawnMapId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(spawnMapId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            spawnMapId = reader.ReadInt();
            if (spawnMapId < 0)
                throw new Exception("Forbidden value on spawnMapId = " + spawnMapId + ", it doesn't respect the following condition : spawnMapId < 0");
            

}


}


}
