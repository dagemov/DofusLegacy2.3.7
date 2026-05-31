
















// Generated on 10/13/2017 02:18:55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class FriendAddFailureMessage : Message
{

public const uint Id = 5600;
public override uint MessageId
{
    get { return Id; }
}

public sbyte reason;
        

public FriendAddFailureMessage()
{
}

public FriendAddFailureMessage(sbyte reason)
        {
            this.reason = reason;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(reason);
            

}

public override void Deserialize(IDataReader reader)
{

reason = reader.ReadSByte();
            if (reason < 0)
                throw new Exception("Forbidden value on reason = " + reason + ", it doesn't respect the following condition : reason < 0");
            

}


}


}
