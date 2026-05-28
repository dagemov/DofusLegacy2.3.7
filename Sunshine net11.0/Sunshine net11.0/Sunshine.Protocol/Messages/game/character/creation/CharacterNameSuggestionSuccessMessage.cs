
















// Generated on 10/13/2017 02:18:43
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class CharacterNameSuggestionSuccessMessage : Message
{

public const uint Id = 5544;
public override uint MessageId
{
    get { return Id; }
}

public string suggestion;
        

public CharacterNameSuggestionSuccessMessage()
{
}

public CharacterNameSuggestionSuccessMessage(string suggestion)
        {
            this.suggestion = suggestion;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(suggestion);
            

}

public override void Deserialize(IDataReader reader)
{

suggestion = reader.ReadUTF();
            

}


}


}
