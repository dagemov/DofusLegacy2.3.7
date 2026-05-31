
















// Generated on 10/13/2017 02:18:45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameMapChangeOrientationMessage : Message
{

public const uint Id = 946;
public override uint MessageId
{
    get { return Id; }
}

public Types.ActorOrientation orientation;
        

public GameMapChangeOrientationMessage()
{
}

public GameMapChangeOrientationMessage(Types.ActorOrientation orientation)
        {
            this.orientation = orientation;
        }
        

public override void Serialize(IDataWriter writer)
{

orientation.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

orientation = new Types.ActorOrientation();
            orientation.Deserialize(reader);
            

}


}


}
