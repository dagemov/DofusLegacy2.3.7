
















// Generated on 10/13/2017 02:18:59
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class ExchangeCraftResultWithObjectIdMessage : ExchangeCraftResultMessage
{

public const uint Id = 6000;
public override uint MessageId
{
    get { return Id; }
}

public int objectGenericId;
        

public ExchangeCraftResultWithObjectIdMessage()
{
}

public ExchangeCraftResultWithObjectIdMessage(sbyte craftResult, int objectGenericId)
         : base(craftResult)
        {
            this.objectGenericId = objectGenericId;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(objectGenericId);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            objectGenericId = reader.ReadInt();
            if (objectGenericId < 0)
                throw new Exception("Forbidden value on objectGenericId = " + objectGenericId + ", it doesn't respect the following condition : objectGenericId < 0");
            

}


}


}
