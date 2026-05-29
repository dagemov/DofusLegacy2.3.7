
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MountRenameRequestMessage : Message
{

public const uint Id = 5987;
public override uint MessageId
{
    get { return Id; }
}

public string name;
        public double mountId;
        

public MountRenameRequestMessage()
{
}

public MountRenameRequestMessage(string name, double mountId)
        {
            this.name = name;
            this.mountId = mountId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(name);
            writer.WriteDouble(mountId);
            

}

public override void Deserialize(IDataReader reader)
{

name = reader.ReadUTF();
            mountId = reader.ReadDouble();
            

}


}


}
