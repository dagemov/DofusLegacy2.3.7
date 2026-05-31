
















// Generated on 10/13/2017 02:19:07
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class TrustStatusMessage : Message
{

public const uint Id = 6267;
public override uint MessageId
{
    get { return Id; }
}

public bool trusted;
        

public TrustStatusMessage()
{
}

public TrustStatusMessage(bool trusted)
        {
            this.trusted = trusted;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(trusted);
            

}

public override void Deserialize(IDataReader reader)
{

trusted = reader.ReadBoolean();
            

}


}


}
