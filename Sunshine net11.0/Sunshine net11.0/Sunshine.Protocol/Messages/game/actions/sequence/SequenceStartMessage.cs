
















// Generated on 10/13/2017 02:18:41
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class SequenceStartMessage : Message
{

public const uint Id = 955;
public override uint MessageId
{
    get { return Id; }
}

public sbyte sequenceType;
        public int authorId;
        

public SequenceStartMessage()
{
}

public SequenceStartMessage(sbyte sequenceType, int authorId)
        {
            this.sequenceType = sequenceType;
            this.authorId = authorId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(sequenceType);
            writer.WriteInt(authorId);
            

}

public override void Deserialize(IDataReader reader)
{

sequenceType = reader.ReadSByte();
            authorId = reader.ReadInt();
            

}


}


}
