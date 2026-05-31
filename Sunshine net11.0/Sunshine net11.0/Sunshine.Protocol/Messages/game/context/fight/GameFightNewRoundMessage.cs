
















// Generated on 10/13/2017 02:18:45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameFightNewRoundMessage : Message
{

public const uint Id = 6239;
public override uint MessageId
{
    get { return Id; }
}

public int roundNumber;
        

public GameFightNewRoundMessage()
{
}

public GameFightNewRoundMessage(int roundNumber)
        {
            this.roundNumber = roundNumber;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(roundNumber);
            

}

public override void Deserialize(IDataReader reader)
{

roundNumber = reader.ReadInt();
            if (roundNumber < 0)
                throw new Exception("Forbidden value on roundNumber = " + roundNumber + ", it doesn't respect the following condition : roundNumber < 0");
            

}


}


}
