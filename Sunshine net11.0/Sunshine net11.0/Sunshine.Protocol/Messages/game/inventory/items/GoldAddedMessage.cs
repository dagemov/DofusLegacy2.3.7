
















// Generated on 10/13/2017 02:19:03
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class GoldAddedMessage : Message
{

public const uint Id = 6030;
public override uint MessageId
{
    get { return Id; }
}

public Types.GoldItem gold;
        

public GoldAddedMessage()
{
}

public GoldAddedMessage(Types.GoldItem gold)
        {
            this.gold = gold;
        }
        

public override void Serialize(IDataWriter writer)
{

gold.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

gold = new Types.GoldItem();
            gold.Deserialize(reader);
            

}


}


}
