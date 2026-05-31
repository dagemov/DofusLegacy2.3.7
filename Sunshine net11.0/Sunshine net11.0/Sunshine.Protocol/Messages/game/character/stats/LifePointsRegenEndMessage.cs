
















// Generated on 10/13/2017 02:18:43
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class LifePointsRegenEndMessage : UpdateLifePointsMessage
{

public const uint Id = 5686;
public override uint MessageId
{
    get { return Id; }
}

public int lifePointsGained;
        

public LifePointsRegenEndMessage()
{
}

public LifePointsRegenEndMessage(int lifePoints, int maxLifePoints, int lifePointsGained)
         : base(lifePoints, maxLifePoints)
        {
            this.lifePointsGained = lifePointsGained;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(lifePointsGained);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            lifePointsGained = reader.ReadInt();
            if (lifePointsGained < 0)
                throw new Exception("Forbidden value on lifePointsGained = " + lifePointsGained + ", it doesn't respect the following condition : lifePointsGained < 0");
            

}


}


}
