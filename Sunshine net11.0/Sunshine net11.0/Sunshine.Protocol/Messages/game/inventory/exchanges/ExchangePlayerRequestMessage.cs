
















// Generated on 10/13/2017 02:19:01
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangePlayerRequestMessage : ExchangeRequestMessage
{

public const uint Id = 5773;
public override uint MessageId
{
    get { return Id; }
}

public int target;
        

public ExchangePlayerRequestMessage()
{
}

public ExchangePlayerRequestMessage(sbyte exchangeType, int target)
         : base(exchangeType)
        {
            this.target = target;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(target);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            target = reader.ReadInt();
            if (target < 0)
                throw new Exception("Forbidden value on target = " + target + ", it doesn't respect the following condition : target < 0");
            

}


}


}
