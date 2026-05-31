
















// Generated on 10/13/2017 02:19:05
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PopupWarningMessage : Message
{

public const uint Id = 6134;
public override uint MessageId
{
    get { return Id; }
}

public byte lockDuration;
        public string author;
        public string content;
        

public PopupWarningMessage()
{
}

public PopupWarningMessage(byte lockDuration, string author, string content)
        {
            this.lockDuration = lockDuration;
            this.author = author;
            this.content = content;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteByte(lockDuration);
            writer.WriteUTF(author);
            writer.WriteUTF(content);
            

}

public override void Deserialize(IDataReader reader)
{

lockDuration = reader.ReadByte();
            if (lockDuration < 0 || lockDuration > 255)
                throw new Exception("Forbidden value on lockDuration = " + lockDuration + ", it doesn't respect the following condition : lockDuration < 0 || lockDuration > 255");
            author = reader.ReadUTF();
            content = reader.ReadUTF();
            

}


}


}
