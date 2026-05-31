
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class EmotePlayMassiveMessage : EmotePlayAbstractMessage
{

public const uint Id = 5691;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<int> actorIds;
        

public EmotePlayMassiveMessage()
{
}

public EmotePlayMassiveMessage(sbyte emoteId, byte duration, IEnumerable<int> actorIds)
         : base(emoteId, duration)
        {
            this.actorIds = actorIds;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            var actorIds_before = writer.Position;
            var actorIds_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in actorIds)
            {
                 writer.WriteInt(entry);
                 actorIds_count++;
            }
            var actorIds_after = writer.Position;
            writer.Seek((int)actorIds_before);
            writer.WriteUShort((ushort)actorIds_count);
            writer.Seek((int)actorIds_after);

            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            var limit = reader.ReadUShort();
            var actorIds_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 actorIds_[i] = reader.ReadInt();
            }
            actorIds = actorIds_;
            

}


}


}
