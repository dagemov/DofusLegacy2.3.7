
















// Generated on 10/13/2017 02:18:36
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class AdminCommandMessage : Message
{

public const uint Id = 76;
public override uint MessageId
{
    get { return Id; }
}

public string content;
        

public AdminCommandMessage()
{
}

public AdminCommandMessage(string content)
        {
            this.content = content;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(content);
            

}

public override void Deserialize(IDataReader reader)
{

content = reader.ReadUTF();
            

}


}


}
