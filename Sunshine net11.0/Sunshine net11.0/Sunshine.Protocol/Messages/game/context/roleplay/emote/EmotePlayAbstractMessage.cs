
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class EmotePlayAbstractMessage : Message
{

public const uint Id = 5690;
public override uint MessageId
{
    get { return Id; }
}

public sbyte emoteId;
        public byte duration;
        

public EmotePlayAbstractMessage()
{
}

public EmotePlayAbstractMessage(sbyte emoteId, byte duration)
        {
            this.emoteId = emoteId;
            this.duration = duration;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(emoteId);
            writer.WriteByte(duration);
            

}

public override void Deserialize(IDataReader reader)
{

emoteId = reader.ReadSByte();
            if (emoteId < 0)
                throw new Exception("Forbidden value on emoteId = " + emoteId + ", it doesn't respect the following condition : emoteId < 0");
            duration = reader.ReadByte();
            if (duration < 0 || duration > 255)
                throw new Exception("Forbidden value on duration = " + duration + ", it doesn't respect the following condition : duration < 0 || duration > 255");
            

}


}


}
