
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MountRenamedMessage : Message
{

public const uint Id = 5983;
public override uint MessageId
{
    get { return Id; }
}

public double mountId;
        public string name;
        

public MountRenamedMessage()
{
}

public MountRenamedMessage(double mountId, string name)
        {
            this.mountId = mountId;
            this.name = name;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteDouble(mountId);
            writer.WriteUTF(name);
            

}

public override void Deserialize(IDataReader reader)
{

mountId = reader.ReadDouble();
            name = reader.ReadUTF();
            

}


}


}
