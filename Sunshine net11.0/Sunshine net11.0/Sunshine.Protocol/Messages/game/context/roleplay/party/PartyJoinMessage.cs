
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyJoinMessage : Message
{

public const uint Id = 5576;
public override uint MessageId
{
    get { return Id; }
}

public int partyId;
        public int partyLeaderId;
        public IEnumerable<Types.PartyMemberInformations> members;
        public IEnumerable<Types.PartyGuestInformations> guests;
        public bool restricted;
        

public PartyJoinMessage()
{
}

public PartyJoinMessage(int partyId, int partyLeaderId, IEnumerable<Types.PartyMemberInformations> members, IEnumerable<Types.PartyGuestInformations> guests, bool restricted)
        {
            this.partyId = partyId;
            this.partyLeaderId = partyLeaderId;
            this.members = members;
            this.guests = guests;
            this.restricted = restricted;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(partyId);
            writer.WriteInt(partyLeaderId);
            var members_before = writer.Position;
            var members_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in members)
            {
                 entry.Serialize(writer);
                 members_count++;
            }
            var members_after = writer.Position;
            writer.Seek((int)members_before);
            writer.WriteUShort((ushort)members_count);
            writer.Seek((int)members_after);

            var guests_before = writer.Position;
            var guests_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in guests)
            {
                 entry.Serialize(writer);
                 guests_count++;
            }
            var guests_after = writer.Position;
            writer.Seek((int)guests_before);
            writer.WriteUShort((ushort)guests_count);
            writer.Seek((int)guests_after);

            writer.WriteBoolean(restricted);
            

}

public override void Deserialize(IDataReader reader)
{

partyId = reader.ReadInt();
            if (partyId < 0)
                throw new Exception("Forbidden value on partyId = " + partyId + ", it doesn't respect the following condition : partyId < 0");
            partyLeaderId = reader.ReadInt();
            if (partyLeaderId < 0)
                throw new Exception("Forbidden value on partyLeaderId = " + partyLeaderId + ", it doesn't respect the following condition : partyLeaderId < 0");
            var limit = reader.ReadUShort();
            var members_ = new Types.PartyMemberInformations[limit];
            for (int i = 0; i < limit; i++)
            {
                 members_[i] = new Types.PartyMemberInformations();
                 members_[i].Deserialize(reader);
            }
            members = members_;
            limit = reader.ReadUShort();
            var guests_ = new Types.PartyGuestInformations[limit];
            for (int i = 0; i < limit; i++)
            {
                 guests_[i] = new Types.PartyGuestInformations();
                 guests_[i].Deserialize(reader);
            }
            guests = guests_;
            restricted = reader.ReadBoolean();
            

}


}


}
