
















// Generated on 10/13/2017 02:18:50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class HouseToSellListRequestMessage : Message
{

public const uint Id = 6139;
public override uint MessageId
{
    get { return Id; }
}

public short pageIndex;
        

public HouseToSellListRequestMessage()
{
}

public HouseToSellListRequestMessage(short pageIndex)
        {
            this.pageIndex = pageIndex;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteShort(pageIndex);
            

}

public override void Deserialize(IDataReader reader)
{

pageIndex = reader.ReadShort();
            if (pageIndex < 0)
                throw new Exception("Forbidden value on pageIndex = " + pageIndex + ", it doesn't respect the following condition : pageIndex < 0");
            

}


}


}
