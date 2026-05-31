
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MountInformationRequestMessage : Message
{

public const uint Id = 5972;
public override uint MessageId
{
    get { return Id; }
}

public double id;
        public double time;
        

public MountInformationRequestMessage()
{
}

public MountInformationRequestMessage(double id, double time)
        {
            this.id = id;
            this.time = time;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteDouble(id);
            writer.WriteDouble(time);
            

}

public override void Deserialize(IDataReader reader)
{

id = reader.ReadDouble();
            time = reader.ReadDouble();
            

}


}


}
