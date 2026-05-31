
















// Generated on 10/13/2017 02:19:08
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GetPartsListMessage : Message
{

public const uint Id = 1501;
public override uint MessageId
{
    get { return Id; }
}



public GetPartsListMessage()
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
