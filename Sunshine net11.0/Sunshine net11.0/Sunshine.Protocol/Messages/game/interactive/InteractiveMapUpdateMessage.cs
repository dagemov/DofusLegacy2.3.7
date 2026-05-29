
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class InteractiveMapUpdateMessage : Message
{

public const uint Id = 5002;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<Types.InteractiveElement> interactiveElements;
        

public InteractiveMapUpdateMessage()
{
}

public InteractiveMapUpdateMessage(IEnumerable<Types.InteractiveElement> interactiveElements)
        {
            this.interactiveElements = interactiveElements;
        }
        

public override void Serialize(IDataWriter writer)
{

var interactiveElements_before = writer.Position;
            var interactiveElements_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in interactiveElements)
            {
                 entry.Serialize(writer);
                 interactiveElements_count++;
            }
            var interactiveElements_after = writer.Position;
            writer.Seek((int)interactiveElements_before);
            writer.WriteUShort((ushort)interactiveElements_count);
            writer.Seek((int)interactiveElements_after);

            

}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var interactiveElements_ = new Types.InteractiveElement[limit];
            for (int i = 0; i < limit; i++)
            {
                 interactiveElements_[i] = new Types.InteractiveElement();
                 interactiveElements_[i].Deserialize(reader);
            }
            interactiveElements = interactiveElements_;
            

}


}


}
