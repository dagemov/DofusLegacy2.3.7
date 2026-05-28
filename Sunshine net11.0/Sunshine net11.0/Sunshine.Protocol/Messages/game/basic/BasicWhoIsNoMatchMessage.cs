
















// Generated on 10/13/2017 02:18:42
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class BasicWhoIsNoMatchMessage : Message
{

public const uint Id = 179;
public override uint MessageId
{
    get { return Id; }
}

public string search;
        

public BasicWhoIsNoMatchMessage()
{
}

public BasicWhoIsNoMatchMessage(string search)
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
