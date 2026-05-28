
















// Generated on 10/13/2017 02:19:04
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class InventoryPresetItemUpdateErrorMessage : Message
{

public const uint Id = 6211;
public override uint MessageId
{
    get { return Id; }
}

public sbyte code;
        

public InventoryPresetItemUpdateErrorMessage()
{
}

public InventoryPresetItemUpdateErrorMessage(sbyte code)
        {
            this.code = code;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(code);
            

}

public override void Deserialize(IDataReader reader)
{

code = reader.ReadSByte();
            if (code < 0)
                throw new Exception("Forbidden value on code = " + code + ", it doesn't respect the following condition : code < 0");
            

}


}


}
