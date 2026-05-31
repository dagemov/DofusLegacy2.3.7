
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class PrismFightJoinLeaveRequestMessage : Message
{

public const uint Id = 5843;
public override uint MessageId
{
    get { return Id; }
}

public bool join;
        

public PrismFightJoinLeaveRequestMessage()
{
}

public PrismFightJoinLeaveRequestMessage(bool join)
        {
            this.join = join;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteBoolean(join);
            

}

public override void Deserialize(IDataReader reader)
{

join = reader.ReadBoolean();
            

}


}


}
