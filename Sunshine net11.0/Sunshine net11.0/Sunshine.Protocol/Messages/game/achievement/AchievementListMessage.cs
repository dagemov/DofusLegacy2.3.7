
















// Generated on 10/13/2017 02:18:38
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class AchievementListMessage : Message
{

public const uint Id = 6205;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<Types.Achievement> startedAchievements;
        public IEnumerable<short> finishedAchievementsIds;
        

public AchievementListMessage()
{
}

public AchievementListMessage(IEnumerable<Types.Achievement> startedAchievements, IEnumerable<short> finishedAchievementsIds)
        {
            this.startedAchievements = startedAchievements;
            this.finishedAchievementsIds = finishedAchievementsIds;
        }
        

public override void Serialize(IDataWriter writer)
{

var startedAchievements_before = writer.Position;
            var startedAchievements_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in startedAchievements)
            {
                 writer.WriteShort(entry.TypeId);
                 entry.Serialize(writer);
                 startedAchievements_count++;
            }
            var startedAchievements_after = writer.Position;
            writer.Seek((int)startedAchievements_before);
            writer.WriteUShort((ushort)startedAchievements_count);
            writer.Seek((int)startedAchievements_after);

            var finishedAchievementsIds_before = writer.Position;
            var finishedAchievementsIds_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in finishedAchievementsIds)
            {
                 writer.WriteShort(entry);
                 finishedAchievementsIds_count++;
            }
            var finishedAchievementsIds_after = writer.Position;
            writer.Seek((int)finishedAchievementsIds_before);
            writer.WriteUShort((ushort)finishedAchievementsIds_count);
            writer.Seek((int)finishedAchievementsIds_after);

            

}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var startedAchievements_ = new Types.Achievement[limit];
            for (int i = 0; i < limit; i++)
            {
                 startedAchievements_[i] = Types.ProtocolTypeManager.GetInstance<Types.Achievement>(reader.ReadShort());
                 startedAchievements_[i].Deserialize(reader);
            }
            startedAchievements = startedAchievements_;
            limit = reader.ReadUShort();
            var finishedAchievementsIds_ = new short[limit];
            for (int i = 0; i < limit; i++)
            {
                 finishedAchievementsIds_[i] = reader.ReadShort();
            }
            finishedAchievementsIds = finishedAchievementsIds_;
            

}


}


}
