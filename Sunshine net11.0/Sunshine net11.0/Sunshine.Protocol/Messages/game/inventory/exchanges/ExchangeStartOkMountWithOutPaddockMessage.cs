
















// Generated on 10/13/2017 02:19:02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeStartOkMountWithOutPaddockMessage : Message
{

public const uint Id = 5991;
public override uint MessageId
{
    get { return Id; }
}

public IEnumerable<Types.MountClientData> stabledMountsDescription;
        

public ExchangeStartOkMountWithOutPaddockMessage()
{
}

public ExchangeStartOkMountWithOutPaddockMessage(IEnumerable<Types.MountClientData> stabledMountsDescription)
        {
            this.stabledMountsDescription = stabledMountsDescription;
        }
        

public override void Serialize(IDataWriter writer)
{

var stabledMountsDescription_before = writer.Position;
            var stabledMountsDescription_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in stabledMountsDescription)
            {
                 entry.Serialize(writer);
                 stabledMountsDescription_count++;
            }
            var stabledMountsDescription_after = writer.Position;
            writer.Seek((int)stabledMountsDescription_before);
            writer.WriteUShort((ushort)stabledMountsDescription_count);
            writer.Seek((int)stabledMountsDescription_after);

            

}

public override void Deserialize(IDataReader reader)
{

var limit = reader.ReadUShort();
            var stabledMountsDescription_ = new Types.MountClientData[limit];
            for (int i = 0; i < limit; i++)
            {
                 stabledMountsDescription_[i] = new Types.MountClientData();
                 stabledMountsDescription_[i].Deserialize(reader);
            }
            stabledMountsDescription = stabledMountsDescription_;
            

}


}


}
