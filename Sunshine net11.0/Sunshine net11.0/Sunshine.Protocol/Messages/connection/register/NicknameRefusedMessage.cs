
















// Generated on 10/13/2017 02:18:38
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class NicknameRefusedMessage : Message
{

public const uint Id = 5638;
public override uint MessageId
{
    get { return Id; }
}

public sbyte reason;
        

public NicknameRefusedMessage()
{
}

public NicknameRefusedMessage(sbyte reason)
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
