
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class HumanWithGuildInformations : HumanInformations
{

public const short Id = 153;
public override short TypeId
{
    get { return Id; }
}

public Types.GuildInformations guildInformations;
        

public HumanWithGuildInformations()
{
}

public HumanWithGuildInformations(IEnumerable<Types.EntityLook> followingCharactersLook, sbyte emoteId, ushort emoteEndTime, Types.ActorRestrictionsInformations restrictions, short titleId, string titleParam, Types.GuildInformations guildInformations)
         : base(followingCharactersLook, emoteId, emoteEndTime, restrictions, titleId, titleParam)
        {
            this.guildInformations = guildInformations;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            guildInformations.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            guildInformations = new Types.GuildInformations();
            guildInformations.Deserialize(reader);
            

}



}


}
