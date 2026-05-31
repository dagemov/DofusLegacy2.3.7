
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameRolePlayPVPArenaFightAnsweredMessage : Message
{

public const uint Id = 6255;
public override uint MessageId
{
    get { return Id; }
}

public int fightId;
        public int targetId;
        

public GameRolePlayPVPArenaFightAnsweredMessage()
{
}

public GameRolePlayPVPArenaFightAnsweredMessage(int fightId, int targetId)
        {
            this.fightId = fightId;
            this.targetId = targetId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(fightId);
            writer.WriteInt(targetId);
            

}

public override void Deserialize(IDataReader reader)
{

fightId = reader.ReadInt();
            targetId = reader.ReadInt();
            if (targetId < 0)
                throw new Exception("Forbidden value on targetId = " + targetId + ", it doesn't respect the following condition : targetId < 0");
            

}


}


}
