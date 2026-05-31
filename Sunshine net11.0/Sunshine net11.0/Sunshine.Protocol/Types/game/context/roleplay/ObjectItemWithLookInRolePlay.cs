
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class ObjectItemWithLookInRolePlay : ObjectItemInRolePlay
{

public const short Id = 197;
public override short TypeId
{
    get { return Id; }
}

public Types.EntityLook entityLook;
        

public ObjectItemWithLookInRolePlay()
{
}

public ObjectItemWithLookInRolePlay(short cellId, short objectGID, Types.EntityLook entityLook)
         : base(cellId, objectGID)
        {
            this.entityLook = entityLook;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            entityLook.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            entityLook = new Types.EntityLook();
            entityLook.Deserialize(reader);
            

}



}


}
