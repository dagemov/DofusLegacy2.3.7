
















// Generated on 10/13/2017 02:19:05
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PackRestrictedSubAreaMessage : Message
{

public const uint Id = 6186;
public override uint MessageId
{
    get { return Id; }
}

public int subAreaId;
        

public PackRestrictedSubAreaMessage()
{
}

public PackRestrictedSubAreaMessage(int subAreaId)
        {
            this.subAreaId = subAreaId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(subAreaId);
            

}

public override void Deserialize(IDataReader reader)
{

subAreaId = reader.ReadInt();
            if (subAreaId < 0)
                throw new Exception("Forbidden value on subAreaId = " + subAreaId + ", it doesn't respect the following condition : subAreaId < 0");
            

}


}


}
