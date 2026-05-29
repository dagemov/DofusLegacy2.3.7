
















// Generated on 10/13/2017 02:18:49
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameRolePlayPVPArenaFightRequestedMessage : Message
{

public const uint Id = 6258;
public override uint MessageId
{
    get { return Id; }
}

public int fightId;
        public IEnumerable<int> fightersIDs;
        

public GameRolePlayPVPArenaFightRequestedMessage()
{
}

public GameRolePlayPVPArenaFightRequestedMessage(int fightId, IEnumerable<int> fightersIDs)
        {
            this.fightId = fightId;
            this.fightersIDs = fightersIDs;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(fightId);
            var fightersIDs_before = writer.Position;
            var fightersIDs_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in fightersIDs)
            {
                 writer.WriteInt(entry);
                 fightersIDs_count++;
            }
            var fightersIDs_after = writer.Position;
            writer.Seek((int)fightersIDs_before);
            writer.WriteUShort((ushort)fightersIDs_count);
            writer.Seek((int)fightersIDs_after);

            

}

public override void Deserialize(IDataReader reader)
{

fightId = reader.ReadInt();
            if (fightId < 0)
                throw new Exception("Forbidden value on fightId = " + fightId + ", it doesn't respect the following condition : fightId < 0");
            var limit = reader.ReadUShort();
            var fightersIDs_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 fightersIDs_[i] = reader.ReadInt();
            }
            fightersIDs = fightersIDs_;
            

}


}


}
