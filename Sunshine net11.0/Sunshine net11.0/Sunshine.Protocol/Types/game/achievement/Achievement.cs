
















// Generated on 10/13/2017 02:17:21
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class Achievement
{

public const short Id = 363;
public virtual short TypeId
{
    get { return Id; }
}

public short id;
        

public Achievement()
{
}

public Achievement(short id)
        {
            this.id = id;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteShort(id);
            

}

public virtual void Deserialize(IDataReader reader)
{

id = reader.ReadShort();
            if (id < 0)
                throw new Exception("Forbidden value on id = " + id + ", it doesn't respect the following condition : id < 0");
            

}



}


}
