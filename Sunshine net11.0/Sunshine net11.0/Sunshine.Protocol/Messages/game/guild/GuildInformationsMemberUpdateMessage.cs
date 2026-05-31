
















// Generated on 10/13/2017 02:18:56
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildInformationsMemberUpdateMessage : Message
{

public const uint Id = 5597;
public override uint MessageId
{
    get { return Id; }
}

public Types.GuildMember member;
        

public GuildInformationsMemberUpdateMessage()
{
}

public GuildInformationsMemberUpdateMessage(Types.GuildMember member)
        {
            this.member = member;
        }
        

public override void Serialize(IDataWriter writer)
{

member.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

member = new Types.GuildMember();
            member.Deserialize(reader);
            

}


}


}
