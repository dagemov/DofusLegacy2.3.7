
















// Generated on 10/13/2017 02:18:54
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class QuestListRequestMessage : Message
{

public const uint Id = 5623;
public override uint MessageId
{
    get { return Id; }
}



public QuestListRequestMessage()
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
