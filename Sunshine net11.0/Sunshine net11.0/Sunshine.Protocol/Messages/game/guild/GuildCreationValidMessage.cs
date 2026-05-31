
















// Generated on 10/13/2017 02:18:56
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildCreationValidMessage : Message
{

public const uint Id = 5546;
public override uint MessageId
{
    get { return Id; }
}

public string guildName;
        public Types.GuildEmblem guildEmblem;
        

public GuildCreationValidMessage()
{
}

public GuildCreationValidMessage(string guildName, Types.GuildEmblem guildEmblem)
        {
            this.guildName = guildName;
            this.guildEmblem = guildEmblem;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(guildName);
            guildEmblem.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

guildName = reader.ReadUTF();
            guildEmblem = new Types.GuildEmblem();
            guildEmblem.Deserialize(reader);
            

}


}


}
