
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyLocateMembersMessage : Message
{

public const uint Id = 5595;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<Types.PartyMemberGeoPosition> geopositions;
        

public PartyLocateMembersMessage()
{
}

public PartyLocateMembersMessage(IEnumerable<Types.PartyMemberGeoPosition> geopositions)
        {
            this.geopositions = geopositions;
        }
        

public override void Serialize(IDataWriter writer)
{

var geopositions_before = writer.Position;
            var geopositions_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in geopositions)
            {
                 entry.Serialize(writer);
                 geopositions_count++;
            }
            var geopositions_after = writer.Position;
            writer.Seek((int)geopositions_before);
            writer.WriteUShort((ushort)geopositions_count);
            writer.Seek((int)geopositions_after);

            

}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var geopositions_ = new Types.PartyMemberGeoPosition[limit];
            for (int i = 0; i < limit; i++)
            {
                 geopositions_[i] = new Types.PartyMemberGeoPosition();
                 geopositions_[i].Deserialize(reader);
            }
            geopositions = geopositions_;
            

}


}


}
