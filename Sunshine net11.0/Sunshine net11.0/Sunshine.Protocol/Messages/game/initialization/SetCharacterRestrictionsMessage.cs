
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class SetCharacterRestrictionsMessage : Message
{

public const uint Id = 170;
public override uint MessageId
{
    get { return Id; }
}

public Types.ActorRestrictionsInformations restrictions;
        

public SetCharacterRestrictionsMessage()
{
}

public SetCharacterRestrictionsMessage(Types.ActorRestrictionsInformations restrictions)
        {
            this.restrictions = restrictions;
        }
        

public override void Serialize(IDataWriter writer)
{

restrictions.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

restrictions = new Types.ActorRestrictionsInformations();
            restrictions.Deserialize(reader);
            

}


}


}
