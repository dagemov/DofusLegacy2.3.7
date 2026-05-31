
















// Generated on 10/13/2017 02:18:44
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ChatSmileyRequestMessage : Message
{

public const uint Id = 800;
public override uint MessageId
{
    get { return Id; }
}

public sbyte smileyId;
        

public ChatSmileyRequestMessage()
{
}

public ChatSmileyRequestMessage(sbyte smileyId)
        {
            this.smileyId = smileyId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(smileyId);
            

}

public override void Deserialize(IDataReader reader)
{

smileyId = reader.ReadSByte();
            if (smileyId < 0)
                throw new Exception("Forbidden value on smileyId = " + smileyId + ", it doesn't respect the following condition : smileyId < 0");
            

}


}


}
