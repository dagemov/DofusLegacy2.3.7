
















// Generated on 10/13/2017 02:18:52
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyInvitationDetailsMessage : Message
{

public const uint Id = 6263;
public override uint MessageId
{
    get { return Id; }
}

public int partyId;
        public int fromId;
        public string fromName;
        public int leaderId;
        public IEnumerable<Types.PartyInvitationMemberInformations> members;
        

public PartyInvitationDetailsMessage()
{
}

public PartyInvitationDetailsMessage(int partyId, int fromId, string fromName, int leaderId, IEnumerable<Types.PartyInvitationMemberInformations> members)
        {
            this.partyId = partyId;
            this.fromId = fromId;
            this.fromName = fromName;
            this.leaderId = leaderId;
            this.members = members;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(partyId);
            writer.WriteInt(fromId);
            writer.WriteUTF(fromName);
            writer.WriteInt(leaderId);
            var members_before = writer.Position;
            var members_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in members)
            {
                 writer.WriteShort(entry.TypeId);
                 entry.Serialize(writer);
                 members_count++;
            }
            var members_after = writer.Position;
            writer.Seek((int)members_before);
            writer.WriteUShort((ushort)members_count);
            writer.Seek((int)members_after);

            

}

public override void Deserialize(IDataReader reader)
{

partyId = reader.ReadInt();
            if (partyId < 0)
                throw new Exception("Forbidden value on partyId = " + partyId + ", it doesn't respect the following condition : partyId < 0");
            fromId = reader.ReadInt();
            if (fromId < 0)
                throw new Exception("Forbidden value on fromId = " + fromId + ", it doesn't respect the following condition : fromId < 0");
            fromName = reader.ReadUTF();
            leaderId = reader.ReadInt();
            if (leaderId < 0)
                throw new Exception("Forbidden value on leaderId = " + leaderId + ", it doesn't respect the following condition : leaderId < 0");
            var limit = reader.ReadUShort();
            var members_ = new Types.PartyInvitationMemberInformations[limit];
            for (int i = 0; i < limit; i++)
            {
                 members_[i] = Types.ProtocolTypeManager.GetInstance<Types.PartyInvitationMemberInformations>(reader.ReadShort());
                 members_[i].Deserialize(reader);
            }
            members = members_;
            

}


}


}
