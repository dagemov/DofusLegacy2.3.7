
















// Generated on 10/13/2017 02:19:07
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ClientKeyMessage : Message
{

public const uint Id = 5607;
public override uint MessageId
{
    get { return Id; }
}

public string key;
        

public ClientKeyMessage()
{
}

public ClientKeyMessage(string key)
        {
            this.key = key;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(key);
            

}

public override void Deserialize(IDataReader reader)
{

key = reader.ReadUTF();
            

}


}


}
