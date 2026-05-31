
















// Generated on 10/13/2017 02:18:36
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class AdminQuietCommandMessage : AdminCommandMessage
{

public const uint Id = 5662;
public override uint MessageId
{
    get { return Id; }
}



public AdminQuietCommandMessage()
{
}

public AdminQuietCommandMessage(string content)
         : base(content)
        {
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            

}


}


}
