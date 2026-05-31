
















// Generated on 10/13/2017 02:18:38
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionAcknowledgementMessage : Message
{

public const uint Id = 957;
public override uint MessageId
{
    get { return Id; }
}

public bool valid;
        public sbyte actionId;
        

public GameActionAcknowledgementMessage()
{
}

public GameActionAcknowledgementMessage(bool valid, sbyte actionId)
        {
            this.valid = valid;
            this.actionId = actionId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(valid);
            writer.WriteSByte(actionId);
            

}

public override void Deserialize(IDataReader reader)
{

valid = reader.ReadBoolean();
            actionId = reader.ReadSByte();
            

}


}


}
