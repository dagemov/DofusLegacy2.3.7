
















// Generated on 10/13/2017 02:18:51
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class LockableCodeResultMessage : Message
{

public const uint Id = 5672;
public override uint MessageId
{
    get { return Id; }
}

public bool success;
        

public LockableCodeResultMessage()
{
}

public LockableCodeResultMessage(bool success)
        {
            this.success = success;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(success);
            

}

public override void Deserialize(IDataReader reader)
{

success = reader.ReadBoolean();
            

}


}


}
