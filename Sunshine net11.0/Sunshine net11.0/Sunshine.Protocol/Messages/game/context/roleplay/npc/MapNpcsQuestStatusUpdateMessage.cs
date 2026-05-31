
















// Generated on 10/13/2017 02:18:51
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MapNpcsQuestStatusUpdateMessage : Message
{

public const uint Id = 5642;
public override uint MessageId
{
    get { return Id; }
}

public int mapId;
        public IEnumerable<int> npcsIdsCanGiveQuest;
        public IEnumerable<int> npcsIdsCannotGiveQuest;
        

public MapNpcsQuestStatusUpdateMessage()
{
}

public MapNpcsQuestStatusUpdateMessage(int mapId, IEnumerable<int> npcsIdsCanGiveQuest, IEnumerable<int> npcsIdsCannotGiveQuest)
        {
            this.mapId = mapId;
            this.npcsIdsCanGiveQuest = npcsIdsCanGiveQuest;
            this.npcsIdsCannotGiveQuest = npcsIdsCannotGiveQuest;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(mapId);
            var npcsIdsCanGiveQuest_before = writer.Position;
            var npcsIdsCanGiveQuest_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in npcsIdsCanGiveQuest)
            {
                 writer.WriteInt(entry);
                 npcsIdsCanGiveQuest_count++;
            }
            var npcsIdsCanGiveQuest_after = writer.Position;
            writer.Seek((int)npcsIdsCanGiveQuest_before);
            writer.WriteUShort((ushort)npcsIdsCanGiveQuest_count);
            writer.Seek((int)npcsIdsCanGiveQuest_after);

            var npcsIdsCannotGiveQuest_before = writer.Position;
            var npcsIdsCannotGiveQuest_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in npcsIdsCannotGiveQuest)
            {
                 writer.WriteInt(entry);
                 npcsIdsCannotGiveQuest_count++;
            }
            var npcsIdsCannotGiveQuest_after = writer.Position;
            writer.Seek((int)npcsIdsCannotGiveQuest_before);
            writer.WriteUShort((ushort)npcsIdsCannotGiveQuest_count);
            writer.Seek((int)npcsIdsCannotGiveQuest_after);

            

}

public override void Deserialize(IDataReader reader)
{

mapId = reader.ReadInt();
            var limit = reader.ReadUShort();
            var npcsIdsCanGiveQuest_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 npcsIdsCanGiveQuest_[i] = reader.ReadInt();
            }
            npcsIdsCanGiveQuest = npcsIdsCanGiveQuest_;
            limit = reader.ReadUShort();
            var npcsIdsCannotGiveQuest_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 npcsIdsCannotGiveQuest_[i] = reader.ReadInt();
            }
            npcsIdsCannotGiveQuest = npcsIdsCannotGiveQuest_;
            

}


}


}
