
















// Generated on 10/13/2017 02:18:59
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeErrorMessage : Message
{

public const uint Id = 5513;
public override uint MessageId
{
    get { return Id; }
}

public sbyte errorType;
        

public ExchangeErrorMessage()
{
}

public ExchangeErrorMessage(sbyte errorType)
        {
            this.errorType = errorType;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteSByte(errorType);
            

}

public override void Deserialize(IDataReader reader)
{

errorType = reader.ReadSByte();
            

}


}


}
