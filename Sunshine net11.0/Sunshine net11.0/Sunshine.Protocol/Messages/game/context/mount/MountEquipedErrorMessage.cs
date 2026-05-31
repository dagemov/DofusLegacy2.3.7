
















// Generated on 10/13/2017 02:18:47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MountEquipedErrorMessage : Message
{

public const uint Id = 5963;
public override uint MessageId
{
    get { return Id; }
}

public sbyte errorType;
        

public MountEquipedErrorMessage()
{
}

public MountEquipedErrorMessage(sbyte errorType)
        {
            this.errorType = errorType;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(errorType);
            

}

public override void Deserialize(IDataReader reader)
{

errorType = reader.ReadSByte();
            if (errorType < 0)
                throw new Exception("Forbidden value on errorType = " + errorType + ", it doesn't respect the following condition : errorType < 0");
            

}


}


}
