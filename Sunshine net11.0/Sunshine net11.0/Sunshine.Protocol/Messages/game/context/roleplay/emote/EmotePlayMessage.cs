
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class EmotePlayMessage : EmotePlayAbstractMessage
{

public const uint Id = 5683;
public override uint MessageId
{
    get { return Id; }
}

public int actorId;
        public int accountId;
        

public EmotePlayMessage()
{
}

public EmotePlayMessage(sbyte emoteId, byte duration, int actorId, int accountId)
         : base(emoteId, duration)
        {
            this.actorId = actorId;
            this.accountId = accountId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(actorId);
            writer.WriteInt(accountId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            actorId = reader.ReadInt();
            accountId = reader.ReadInt();
            

}


}


}
