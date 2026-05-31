
















// Generated on 10/13/2017 02:18:48
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class StopToListenRunningFightRequestMessage : Message
{

public const uint Id = 6124;
public override uint MessageId
{
    get { return Id; }
}



public StopToListenRunningFightRequestMessage()
{
}



public override void Serialize(IDataWriter writer)
{



}

public override void Deserialize(IDataReader reader)
{



}


}


}
