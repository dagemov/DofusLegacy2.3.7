
















// Generated on 10/13/2017 02:18:44
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class MoodSmileyResultMessage : Message
{

public const uint Id = 6196;
public override uint MessageId
{
    get { return Id; }
}

public sbyte resultCode;
        public sbyte smileyId;
        

public MoodSmileyResultMessage()
{
}

public MoodSmileyResultMessage(sbyte resultCode, sbyte smileyId)
        {
            this.resultCode = resultCode;
            this.smileyId = smileyId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(resultCode);
            writer.WriteSByte(smileyId);
            

}

public override void Deserialize(IDataReader reader)
{

resultCode = reader.ReadSByte();
            if (resultCode < 0)
                throw new Exception("Forbidden value on resultCode = " + resultCode + ", it doesn't respect the following condition : resultCode < 0");
            smileyId = reader.ReadSByte();
            

}


}


}
