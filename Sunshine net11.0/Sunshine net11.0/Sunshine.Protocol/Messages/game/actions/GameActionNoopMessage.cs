
















// Generated on 10/13/2017 02:18:38
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameActionNoopMessage : Message
{

public const uint Id = 1002;
public override uint MessageId
{
    get { return Id; }
}



public GameActionNoopMessage()
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
