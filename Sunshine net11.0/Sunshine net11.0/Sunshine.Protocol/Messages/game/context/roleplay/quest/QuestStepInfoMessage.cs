
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class QuestStepInfoMessage : Message
{

public const uint Id = 5625;
public override uint MessageId
{
    get { return Id; }
}

public short questId;
        public short stepId;
        public IEnumerable<short> objectivesIds;
        public IEnumerable<bool> objectivesStatus;
        

public QuestStepInfoMessage()
{
}

public QuestStepInfoMessage(short questId, short stepId, IEnumerable<short> objectivesIds, IEnumerable<bool> objectivesStatus)
        {
            this.questId = questId;
            this.stepId = stepId;
            this.objectivesIds = objectivesIds;
            this.objectivesStatus = objectivesStatus;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(questId);
            writer.WriteShort(stepId);
            var objectivesIds_before = writer.Position;
            var objectivesIds_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in objectivesIds)
            {
                 writer.WriteShort(entry);
                 objectivesIds_count++;
            }
            var objectivesIds_after = writer.Position;
            writer.Seek((int)objectivesIds_before);
            writer.WriteUShort((ushort)objectivesIds_count);
            writer.Seek((int)objectivesIds_after);

            var objectivesStatus_before = writer.Position;
            var objectivesStatus_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in objectivesStatus)
            {
                 writer.WriteBoolean(entry);
                 objectivesStatus_count++;
            }
            var objectivesStatus_after = writer.Position;
            writer.Seek((int)objectivesStatus_before);
            writer.WriteUShort((ushort)objectivesStatus_count);
            writer.Seek((int)objectivesStatus_after);

            

}

public override void Deserialize(IDataReader reader)
{

questId = reader.ReadShort();
            if (questId < 0)
                throw new Exception("Forbidden value on questId = " + questId + ", it doesn't respect the following condition : questId < 0");
            stepId = reader.ReadShort();
            if (stepId < 0)
                throw new Exception("Forbidden value on stepId = " + stepId + ", it doesn't respect the following condition : stepId < 0");
            var limit = reader.ReadUShort();
            var objectivesIds_ = new short[limit];
            for (int i = 0; i < limit; i++)
            {
                 objectivesIds_[i] = reader.ReadShort();
            }
            objectivesIds = objectivesIds_;
            limit = reader.ReadUShort();
            var objectivesStatus_ = new bool[limit];
            for (int i = 0; i < limit; i++)
            {
                 objectivesStatus_[i] = reader.ReadBoolean();
            }
            objectivesStatus = objectivesStatus_;
            

}


}


}
