
















// Generated on 10/13/2017 02:18:38
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class AchievementFinishedMessage : Message
{

public const uint Id = 6208;
public override uint MessageId
{
    get { return Id; }
}

public short achievementId;
        

public AchievementFinishedMessage()
{
}

public AchievementFinishedMessage(short achievementId)
        {
            this.achievementId = achievementId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(achievementId);
            

}

public override void Deserialize(IDataReader reader)
{

achievementId = reader.ReadShort();
            if (achievementId < 0)
                throw new Exception("Forbidden value on achievementId = " + achievementId + ", it doesn't respect the following condition : achievementId < 0");
            

}


}


}
