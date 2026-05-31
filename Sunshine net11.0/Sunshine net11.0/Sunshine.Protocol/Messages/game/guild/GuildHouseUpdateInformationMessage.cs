
















// Generated on 10/13/2017 02:18:56
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildHouseUpdateInformationMessage : Message
{

public const uint Id = 6181;
public override uint MessageId
{
    get { return Id; }
}

public Types.HouseInformationsForGuild housesInformations;
        

public GuildHouseUpdateInformationMessage()
{
}

public GuildHouseUpdateInformationMessage(Types.HouseInformationsForGuild housesInformations)
        {
            this.housesInformations = housesInformations;
        }
        

public override void Serialize(IDataWriter writer)
{

housesInformations.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

housesInformations = new Types.HouseInformationsForGuild();
            housesInformations.Deserialize(reader);
            

}


}


}
