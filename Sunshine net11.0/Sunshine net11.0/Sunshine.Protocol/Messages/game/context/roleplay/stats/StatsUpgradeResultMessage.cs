
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class StatsUpgradeResultMessage : Message
{

public const uint Id = 5609;
public override uint MessageId
{
    get { return Id; }
}

public short nbCharacBoost;
        

public StatsUpgradeResultMessage()
{
}

public StatsUpgradeResultMessage(short nbCharacBoost)
        {
            this.nbCharacBoost = nbCharacBoost;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(nbCharacBoost);
            

}

public override void Deserialize(IDataReader reader)
{

nbCharacBoost = reader.ReadShort();
            if (nbCharacBoost < 0)
                throw new Exception("Forbidden value on nbCharacBoost = " + nbCharacBoost + ", it doesn't respect the following condition : nbCharacBoost < 0");
            

}


}


}
