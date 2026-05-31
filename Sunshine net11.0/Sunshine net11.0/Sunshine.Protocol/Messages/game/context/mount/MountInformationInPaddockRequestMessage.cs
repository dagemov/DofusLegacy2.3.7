
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MountInformationInPaddockRequestMessage : Message
{

public const uint Id = 5975;
public override uint MessageId
{
    get { return Id; }
}

public int mapRideId;
        

public MountInformationInPaddockRequestMessage()
{
}

public MountInformationInPaddockRequestMessage(int mapRideId)
        {
            this.mapRideId = mapRideId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(mapRideId);
            

}

public override void Deserialize(IDataReader reader)
{

mapRideId = reader.ReadInt();
            

}


}


}
