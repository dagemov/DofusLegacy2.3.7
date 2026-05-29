
















// Generated on 10/13/2017 02:18:59
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeItemAutoCraftRemainingMessage : Message
{

public const uint Id = 6015;
public override uint MessageId
{
    get { return Id; }
}

public int count;
        

public ExchangeItemAutoCraftRemainingMessage()
{
}

public ExchangeItemAutoCraftRemainingMessage(int count)
        {
            this.count = count;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(count);
            

}

public override void Deserialize(IDataReader reader)
{

count = reader.ReadInt();
            if (count < 0)
                throw new Exception("Forbidden value on count = " + count + ", it doesn't respect the following condition : count < 0");
            

}


}


}
