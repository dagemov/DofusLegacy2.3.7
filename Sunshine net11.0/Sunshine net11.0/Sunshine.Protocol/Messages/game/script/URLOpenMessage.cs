
















// Generated on 10/13/2017 02:19:06
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{

public class URLOpenMessage : Message
{

public const uint Id = 6266;
public override uint MessageId
{
    get { return Id; }
}

public int urlId;
        

public URLOpenMessage()
{
}

public URLOpenMessage(int urlId)
        {
            this.urlId = urlId;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteInt(urlId);
            

}

public override void Deserialize(IDataReader reader)
{

urlId = reader.ReadInt();
            if (urlId < 0)
                throw new Exception("Forbidden value on urlId = " + urlId + ", it doesn't respect the following condition : urlId < 0");
            

}


}


}
