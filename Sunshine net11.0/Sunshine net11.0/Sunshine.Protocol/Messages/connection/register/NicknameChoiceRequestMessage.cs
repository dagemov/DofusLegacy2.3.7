
















// Generated on 10/13/2017 02:18:38
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class NicknameChoiceRequestMessage : Message
{

public const uint Id = 5639;
public override uint MessageId
{
    get { return Id; }
}

public string nickname;
        

public NicknameChoiceRequestMessage()
{
}

public NicknameChoiceRequestMessage(string nickname)
        {
            this.nickname = nickname;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(nickname);
            

}

public override void Deserialize(IDataReader reader)
{

nickname = reader.ReadUTF();
            

}


}


}
