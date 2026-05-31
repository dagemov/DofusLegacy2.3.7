
















// Generated on 10/13/2017 02:18:43
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class CharacterReplayRequestMessage : Message
{

public const uint Id = 167;
public override uint MessageId
{
    get { return Id; }
}

public int characterId;
        

public CharacterReplayRequestMessage()
{
}

public CharacterReplayRequestMessage(int characterId)
        {
            this.characterId = characterId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(characterId);
            

}

public override void Deserialize(IDataReader reader)
{

characterId = reader.ReadInt();
            if (characterId < 0)
                throw new Exception("Forbidden value on characterId = " + characterId + ", it doesn't respect the following condition : characterId < 0");
            

}


}


}
