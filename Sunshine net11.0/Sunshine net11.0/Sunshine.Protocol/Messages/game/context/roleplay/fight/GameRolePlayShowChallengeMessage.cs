
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameRolePlayShowChallengeMessage : Message
{

public const uint Id = 301;
public override uint MessageId
{
    get { return Id; }
}

public Types.FightCommonInformations commonsInfos;
        

public GameRolePlayShowChallengeMessage()
{
}

public GameRolePlayShowChallengeMessage(Types.FightCommonInformations commonsInfos)
        {
            this.commonsInfos = commonsInfos;
        }
        

public override void Serialize(IDataWriter writer)
{

commonsInfos.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

commonsInfos = new Types.FightCommonInformations();
            commonsInfos.Deserialize(reader);
            

}


}


}
