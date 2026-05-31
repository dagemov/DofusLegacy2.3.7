
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class CinematicMessage : Message
{

public const uint Id = 6053;
public override uint MessageId
{
    get { return Id; }
}

public short cinematicId;
        

public CinematicMessage()
{
}

public CinematicMessage(short cinematicId)
        {
            this.cinematicId = cinematicId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(cinematicId);
            

}

public override void Deserialize(IDataReader reader)
{

cinematicId = reader.ReadShort();
            if (cinematicId < 0)
                throw new Exception("Forbidden value on cinematicId = " + cinematicId + ", it doesn't respect the following condition : cinematicId < 0");
            

}


}


}
