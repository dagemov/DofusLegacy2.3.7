
















// Generated on 10/13/2017 02:17:25
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class ObjectEffect
{

public const short Id = 76;
public virtual short TypeId
{
    get { return Id; }
}

public short actionId;
        

public ObjectEffect()
{
}

public ObjectEffect(short actionId)
        {
            this.actionId = actionId;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteShort(actionId);
            

}

public virtual void Deserialize(IDataReader reader)
{

actionId = reader.ReadShort();
            if (actionId < 0)
                throw new Exception("Forbidden value on actionId = " + actionId + ", it doesn't respect the following condition : actionId < 0");
            

}



}


}
