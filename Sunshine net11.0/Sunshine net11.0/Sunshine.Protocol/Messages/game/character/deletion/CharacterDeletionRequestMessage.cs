
















// Generated on 10/13/2017 02:18:43
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class CharacterDeletionRequestMessage : Message
{

public const uint Id = 165;
public override uint MessageId
{
    get { return Id; }
}

public int characterId;
        public string secretAnswerHash;
        

public CharacterDeletionRequestMessage()
{
}

public CharacterDeletionRequestMessage(int characterId, string secretAnswerHash)
        {
            this.characterId = characterId;
            this.secretAnswerHash = secretAnswerHash;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(characterId);
            writer.WriteUTF(secretAnswerHash);
            

}

public override void Deserialize(IDataReader reader)
{

characterId = reader.ReadInt();
            if (characterId < 0)
                throw new Exception("Forbidden value on characterId = " + characterId + ", it doesn't respect the following condition : characterId < 0");
            secretAnswerHash = reader.ReadUTF();
            

}


}


}
