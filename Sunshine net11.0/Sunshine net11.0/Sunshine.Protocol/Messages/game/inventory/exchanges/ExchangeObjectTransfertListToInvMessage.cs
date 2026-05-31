
















// Generated on 10/13/2017 02:19:00
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeObjectTransfertListToInvMessage : Message
{

public const uint Id = 6039;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<int> ids;
        

public ExchangeObjectTransfertListToInvMessage()
{
}

public ExchangeObjectTransfertListToInvMessage(IEnumerable<int> ids)
        {
            this.ids = ids;
        }
        

public override void Serialize(IDataWriter writer)
{

var ids_before = writer.Position;
            var ids_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in ids)
            {
                 writer.WriteInt(entry);
                 ids_count++;
            }
            var ids_after = writer.Position;
            writer.Seek((int)ids_before);
            writer.WriteUShort((ushort)ids_count);
            writer.Seek((int)ids_after);

            

}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var ids_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 ids_[i] = reader.ReadInt();
            }
            ids = ids_;
            

}


}


}
