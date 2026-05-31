
















// Generated on 10/13/2017 02:18:58
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class InteractiveElementUpdatedMessage : Message
{

public const uint Id = 5708;
public override uint MessageId
{
    get { return Id; }
}

public Types.InteractiveElement interactiveElement;
        

public InteractiveElementUpdatedMessage()
{
}

public InteractiveElementUpdatedMessage(Types.InteractiveElement interactiveElement)
        {
            this.interactiveElement = interactiveElement;
        }
        

public override void Serialize(IDataWriter writer)
{

interactiveElement.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

interactiveElement = new Types.InteractiveElement();
            interactiveElement.Deserialize(reader);
            

}


}


}
