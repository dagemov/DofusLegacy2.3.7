
















// Generated on 10/13/2017 02:18:44
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GameContextCreateMessage : Message
{

public const uint Id = 200;
public override uint MessageId
{
    get { return Id; }
}

public sbyte context;
        

public GameContextCreateMessage()
{
}

public GameContextCreateMessage(sbyte context)
        {
            this.context = context;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(context);
            

}

public override void Deserialize(IDataReader reader)
{

context = reader.ReadSByte();
            if (context < 0)
                throw new Exception("Forbidden value on context = " + context + ", it doesn't respect the following condition : context < 0");
            

}


}


}
