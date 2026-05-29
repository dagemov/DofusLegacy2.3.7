
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PrismInfoValidMessage : Message
{

public const uint Id = 5858;
public override uint MessageId
{
    get { return Id; }
}

public Types.ProtectedEntityWaitingForHelpInfo waitingForHelpInfo;
        

public PrismInfoValidMessage()
{
}

public PrismInfoValidMessage(Types.ProtectedEntityWaitingForHelpInfo waitingForHelpInfo)
        {
            this.waitingForHelpInfo = waitingForHelpInfo;
        }
        

public override void Serialize(IDataWriter writer)
{

waitingForHelpInfo.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

waitingForHelpInfo = new Types.ProtectedEntityWaitingForHelpInfo();
            waitingForHelpInfo.Deserialize(reader);
            

}


}


}
