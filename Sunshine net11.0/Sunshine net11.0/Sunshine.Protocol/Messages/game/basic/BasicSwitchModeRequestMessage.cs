
















// Generated on 10/13/2017 02:18:42
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class BasicSwitchModeRequestMessage : Message
{

public const uint Id = 6101;
public override uint MessageId
{
    get { return Id; }
}

public sbyte mode;
        

public BasicSwitchModeRequestMessage()
{
}

public BasicSwitchModeRequestMessage(sbyte mode)
        {
            this.mode = mode;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(mode);
            

}

public override void Deserialize(IDataReader reader)
{

mode = reader.ReadSByte();
            

}


}


}
