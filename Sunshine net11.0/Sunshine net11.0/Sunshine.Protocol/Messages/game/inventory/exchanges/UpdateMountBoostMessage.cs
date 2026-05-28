
















// Generated on 10/13/2017 02:19:02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class UpdateMountBoostMessage : Message
{

public const uint Id = 6179;
public override uint MessageId
{
    get { return Id; }
}

public double rideId;
        public IEnumerable<Types.UpdateMountBoost> boostToUpdateList;
        

public UpdateMountBoostMessage()
{
}

public UpdateMountBoostMessage(double rideId, IEnumerable<Types.UpdateMountBoost> boostToUpdateList)
        {
            this.rideId = rideId;
            this.boostToUpdateList = boostToUpdateList;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteDouble(rideId);
            var boostToUpdateList_before = writer.Position;
            var boostToUpdateList_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in boostToUpdateList)
            {
                 writer.WriteShort(entry.TypeId);
                 entry.Serialize(writer);
                 boostToUpdateList_count++;
            }
            var boostToUpdateList_after = writer.Position;
            writer.Seek((int)boostToUpdateList_before);
            writer.WriteUShort((ushort)boostToUpdateList_count);
            writer.Seek((int)boostToUpdateList_after);

            

}

public override void Deserialize(IDataReader reader)
{

rideId = reader.ReadDouble();
            var limit = reader.ReadUShort();
            var boostToUpdateList_ = new Types.UpdateMountBoost[limit];
            for (int i = 0; i < limit; i++)
            {
                 boostToUpdateList_[i] = Types.ProtocolTypeManager.GetInstance<Types.UpdateMountBoost>(reader.ReadShort());
                 boostToUpdateList_[i].Deserialize(reader);
            }
            boostToUpdateList = boostToUpdateList_;
            

}


}


}
