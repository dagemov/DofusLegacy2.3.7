
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class NetworkDataContainerMessage : Message
{

public const uint Id = 2;
public override uint MessageId
{
    get { return Id; }
}



public NetworkDataContainerMessage()
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
