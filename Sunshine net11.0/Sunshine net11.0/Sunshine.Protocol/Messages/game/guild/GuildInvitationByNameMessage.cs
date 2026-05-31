
















// Generated on 10/13/2017 02:18:56
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildInvitationByNameMessage : Message
{

public const uint Id = 6115;
public override uint MessageId
{
    get { return Id; }
}

public string name;
        

public GuildInvitationByNameMessage()
{
}

public GuildInvitationByNameMessage(string name)
        {
            this.name = name;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(name);
            

}

public override void Deserialize(IDataReader reader)
{

name = reader.ReadUTF();
            

}


}


}
