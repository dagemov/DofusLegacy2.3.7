
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyUpdateMessage : Message
{

public const uint Id = 5575;
public override uint MessageId
{
    get { return Id; }
}

public Types.PartyMemberInformations memberInformations;
        

public PartyUpdateMessage()
{
}

public PartyUpdateMessage(Types.PartyMemberInformations memberInformations)
        {
            this.memberInformations = memberInformations;
        }
        

public override void Serialize(IDataWriter writer)
{

memberInformations.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

memberInformations = new Types.PartyMemberInformations();
            memberInformations.Deserialize(reader);
            

}


}


}
