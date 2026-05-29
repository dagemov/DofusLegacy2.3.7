
















// Generated on 10/13/2017 02:18:39
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionFightInvisibleTacklerMessage : AbstractGameActionMessage
{

public const uint Id = 6233;
public override uint MessageId
{
    get { return Id; }
}

public int extraActionPointLoss;
        public int extraMouvementPointLost;
        

public GameActionFightInvisibleTacklerMessage()
{
}

public GameActionFightInvisibleTacklerMessage(short actionId, int sourceId, int extraActionPointLoss, int extraMouvementPointLost)
         : base(actionId, sourceId)
        {
            this.extraActionPointLoss = extraActionPointLoss;
            this.extraMouvementPointLost = extraMouvementPointLost;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(extraActionPointLoss);
            writer.WriteInt(extraMouvementPointLost);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            extraActionPointLoss = reader.ReadInt();
            if (extraActionPointLoss < 0)
                throw new Exception("Forbidden value on extraActionPointLoss = " + extraActionPointLoss + ", it doesn't respect the following condition : extraActionPointLoss < 0");
            extraMouvementPointLost = reader.ReadInt();
            if (extraMouvementPointLost < 0)
                throw new Exception("Forbidden value on extraMouvementPointLost = " + extraMouvementPointLost + ", it doesn't respect the following condition : extraMouvementPointLost < 0");
            

}


}


}
