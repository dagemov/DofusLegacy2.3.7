
















// Generated on 10/13/2017 02:18:56
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GuildCreationStartedMessage : Message
{

public const uint Id = 5920;
public override uint MessageId
{
    get { return Id; }
}



public GuildCreationStartedMessage()
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
