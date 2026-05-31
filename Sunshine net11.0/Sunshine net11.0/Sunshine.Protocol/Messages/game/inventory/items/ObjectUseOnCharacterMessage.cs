
















// Generated on 10/13/2017 02:19:04
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ObjectUseOnCharacterMessage : ObjectUseMessage
{

public const uint Id = 3003;
public override uint MessageId
{
    get { return Id; }
}

public int characterId;
        

public ObjectUseOnCharacterMessage()
{
}

public ObjectUseOnCharacterMessage(int objectUID, int characterId)
         : base(objectUID)
        {
            this.characterId = characterId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(characterId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            characterId = reader.ReadInt();
            if (characterId < 0)
                throw new Exception("Forbidden value on characterId = " + characterId + ", it doesn't respect the following condition : characterId < 0");
            

}


}


}
