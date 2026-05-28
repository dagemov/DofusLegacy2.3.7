
















// Generated on 10/13/2017 02:18:41
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class AuthenticationTicketWithClientPacksMessage : AuthenticationTicketMessage
{

public const uint Id = 6190;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<int> packs;
        

public AuthenticationTicketWithClientPacksMessage()
{
}

public AuthenticationTicketWithClientPacksMessage(string lang, string ticket, IEnumerable<int> packs)
         : base(lang, ticket)
        {
            this.packs = packs;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            var packs_before = writer.Position;
            var packs_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in packs)
            {
                 writer.WriteInt(entry);
                 packs_count++;
            }
            var packs_after = writer.Position;
            writer.Seek((int)packs_before);
            writer.WriteUShort((ushort)packs_count);
            writer.Seek((int)packs_after);

            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            var limit = reader.ReadUShort();
            var packs_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 packs_[i] = reader.ReadInt();
            }
            packs = packs_;
            

}


}


}
