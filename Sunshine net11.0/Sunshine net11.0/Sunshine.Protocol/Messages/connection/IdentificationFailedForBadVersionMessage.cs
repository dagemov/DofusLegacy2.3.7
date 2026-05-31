
















// Generated on 10/13/2017 02:18:37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class IdentificationFailedForBadVersionMessage : IdentificationFailedMessage
{

public const uint Id = 21;
public override uint MessageId
{
    get { return Id; }
}

public Types.Version requiredVersion;
        

public IdentificationFailedForBadVersionMessage()
{
}

public IdentificationFailedForBadVersionMessage(sbyte reason, Types.Version requiredVersion)
         : base(reason)
        {
            this.requiredVersion = requiredVersion;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            requiredVersion.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            requiredVersion = new Types.Version();
            requiredVersion.Deserialize(reader);
            

}


}


}
