
















// Generated on 10/13/2017 02:18:48
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameRolePlayShowActorMessage : Message
{

public const uint Id = 5632;
public override uint MessageId
{
    get { return Id; }
}

public Types.GameRolePlayActorInformations informations;
        

public GameRolePlayShowActorMessage()
{
}

public GameRolePlayShowActorMessage(Types.GameRolePlayActorInformations informations)
        {
            this.informations = informations;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(informations.TypeId);
            informations.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

informations = Types.ProtocolTypeManager.GetInstance<Types.GameRolePlayActorInformations>(reader.ReadShort());
            informations.Deserialize(reader);
            

}


}


}
