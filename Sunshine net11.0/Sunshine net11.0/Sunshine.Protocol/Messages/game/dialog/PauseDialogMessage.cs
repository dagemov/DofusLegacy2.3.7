
















// Generated on 10/13/2017 02:18:55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PauseDialogMessage : Message
{

public const uint Id = 6012;
public override uint MessageId
{
    get { return Id; }
}



public PauseDialogMessage()
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
