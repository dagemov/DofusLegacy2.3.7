
















// Generated on 10/13/2017 02:19:07
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ShortcutBarRemoveErrorMessage : Message
{

public const uint Id = 6222;
public override uint MessageId
{
    get { return Id; }
}

public sbyte error;
        

public ShortcutBarRemoveErrorMessage()
{
}

public ShortcutBarRemoveErrorMessage(sbyte error)
        {
            this.error = error;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(error);
            

}

public override void Deserialize(IDataReader reader)
{

error = reader.ReadSByte();
            if (error < 0)
                throw new Exception("Forbidden value on error = " + error + ", it doesn't respect the following condition : error < 0");
            

}


}


}
