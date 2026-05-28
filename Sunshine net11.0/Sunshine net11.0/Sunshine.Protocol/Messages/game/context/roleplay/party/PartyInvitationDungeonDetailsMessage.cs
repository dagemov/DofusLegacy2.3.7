
















// Generated on 10/13/2017 02:18:53
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PartyInvitationDungeonDetailsMessage : PartyInvitationDetailsMessage
{

public const uint Id = 6262;
public override uint MessageId
{
    get { return Id; }
}

public short dungeonId;
        public IEnumerable<bool> playersDungeonReady;
        

public PartyInvitationDungeonDetailsMessage()
{
}

public PartyInvitationDungeonDetailsMessage(int partyId, int fromId, string fromName, int leaderId, IEnumerable<Types.PartyInvitationMemberInformations> members, short dungeonId, IEnumerable<bool> playersDungeonReady)
         : base(partyId, fromId, fromName, leaderId, members)
        {
            this.dungeonId = dungeonId;
            this.playersDungeonReady = playersDungeonReady;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteShort(dungeonId);
            var playersDungeonReady_before = writer.Position;
            var playersDungeonReady_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in playersDungeonReady)
            {
                 writer.WriteBoolean(entry);
                 playersDungeonReady_count++;
            }
            var playersDungeonReady_after = writer.Position;
            writer.Seek((int)playersDungeonReady_before);
            writer.WriteUShort((ushort)playersDungeonReady_count);
            writer.Seek((int)playersDungeonReady_after);

            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            dungeonId = reader.ReadShort();
            if (dungeonId < 0)
                throw new Exception("Forbidden value on dungeonId = " + dungeonId + ", it doesn't respect the following condition : dungeonId < 0");
            var limit = reader.ReadUShort();
            var playersDungeonReady_ = new bool[limit];
            for (int i = 0; i < limit; i++)
            {
                 playersDungeonReady_[i] = reader.ReadBoolean();
            }
            playersDungeonReady = playersDungeonReady_;
            

}


}


}
