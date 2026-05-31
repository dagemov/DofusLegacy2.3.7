
















// Generated on 10/13/2017 02:17:26
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class IgnoredInformations
{

public const short Id = 106;
public virtual short TypeId
{
    get { return Id; }
}

public string name;
        public int id;
        

public IgnoredInformations()
{
}

public IgnoredInformations(string name, int id)
        {
            this.name = name;
            this.id = id;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteUTF(name);
            writer.WriteInt(id);
            

}

public virtual void Deserialize(IDataReader reader)
{

name = reader.ReadUTF();
            id = reader.ReadInt();
            if (id < 0)
                throw new Exception("Forbidden value on id = " + id + ", it doesn't respect the following condition : id < 0");
            

}



}


}
