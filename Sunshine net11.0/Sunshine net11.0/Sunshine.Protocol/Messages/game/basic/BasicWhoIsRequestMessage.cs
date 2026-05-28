
















// Generated on 10/13/2017 02:18:42
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class BasicWhoIsRequestMessage : Message
{

public const uint Id = 181;
public override uint MessageId
{
    get { return Id; }
}

public string search;
        

public BasicWhoIsRequestMessage()
{
}

public BasicWhoIsRequestMessage(string search)
        {
            this.search = search;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(search);
            

}

public override void Deserialize(IDataReader reader)
{

search = reader.ReadUTF();
            

}


}


}
