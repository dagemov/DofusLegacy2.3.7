
















// Generated on 10/13/2017 02:18:46
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameFightTurnStartMessage : Message
{

public const uint Id = 714;
public override uint MessageId
{
    get { return Id; }
}

public int id;
        public int waitTime;
        

public GameFightTurnStartMessage()
{
}

public GameFightTurnStartMessage(int id, int waitTime)
        {
            this.id = id;
            this.waitTime = waitTime;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(id);
            writer.WriteInt(waitTime);
            

}

public override void Deserialize(IDataReader reader)
{

id = reader.ReadInt();
            waitTime = reader.ReadInt();
            if (waitTime < 0)
                throw new Exception("Forbidden value on waitTime = " + waitTime + ", it doesn't respect the following condition : waitTime < 0");
            

}


}


}
