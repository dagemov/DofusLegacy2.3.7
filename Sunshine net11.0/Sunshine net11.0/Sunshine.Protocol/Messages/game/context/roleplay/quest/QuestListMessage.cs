
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class QuestListMessage : Message
{

public const uint Id = 5626;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<short> finishedQuestsIds;
        public IEnumerable<short> activeQuestsIds;
        

public QuestListMessage()
{
}

public QuestListMessage(IEnumerable<short> finishedQuestsIds, IEnumerable<short> activeQuestsIds)
        {
            this.finishedQuestsIds = finishedQuestsIds;
            this.activeQuestsIds = activeQuestsIds;
        }
        

public override void Serialize(IDataWriter writer)
{

var finishedQuestsIds_before = writer.Position;
            var finishedQuestsIds_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in finishedQuestsIds)
            {
                 writer.WriteShort(entry);
                 finishedQuestsIds_count++;
            }
            var finishedQuestsIds_after = writer.Position;
            writer.Seek((int)finishedQuestsIds_before);
            writer.WriteUShort((ushort)finishedQuestsIds_count);
            writer.Seek((int)finishedQuestsIds_after);

            var activeQuestsIds_before = writer.Position;
            var activeQuestsIds_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in activeQuestsIds)
            {
                 writer.WriteShort(entry);
                 activeQuestsIds_count++;
            }
            var activeQuestsIds_after = writer.Position;
            writer.Seek((int)activeQuestsIds_before);
            writer.WriteUShort((ushort)activeQuestsIds_count);
            writer.Seek((int)activeQuestsIds_after);

            

}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var finishedQuestsIds_ = new short[limit];
            for (int i = 0; i < limit; i++)
            {
                 finishedQuestsIds_[i] = reader.ReadShort();
            }
            finishedQuestsIds = finishedQuestsIds_;
            limit = reader.ReadUShort();
            var activeQuestsIds_ = new short[limit];
            for (int i = 0; i < limit; i++)
            {
                 activeQuestsIds_[i] = reader.ReadShort();
            }
            activeQuestsIds = activeQuestsIds_;
            

}


}


}
