
















// Generated on 10/13/2017 02:18:55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class IgnoredDeleteRequestMessage : Message
{

public const uint Id = 5680;
public override uint MessageId
{
    get { return Id; }
}

public string name;
        public bool session;
        

public IgnoredDeleteRequestMessage()
{
}

public IgnoredDeleteRequestMessage(string name, bool session)
        {
            this.name = name;
            this.session = session;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(name);
            writer.WriteBoolean(session);
            

}

public override void Deserialize(IDataReader reader)
{

name = reader.ReadUTF();
            session = reader.ReadBoolean();
            

}


}


}
