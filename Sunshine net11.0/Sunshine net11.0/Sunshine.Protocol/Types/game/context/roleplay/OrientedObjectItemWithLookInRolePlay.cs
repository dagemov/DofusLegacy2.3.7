
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class OrientedObjectItemWithLookInRolePlay : ObjectItemWithLookInRolePlay
{

public const short Id = 199;
public override short TypeId
{
    get { return Id; }
}

public sbyte direction;
        

public OrientedObjectItemWithLookInRolePlay()
{
}

public OrientedObjectItemWithLookInRolePlay(short cellId, short objectGID, Types.EntityLook entityLook, sbyte direction)
         : base(cellId, objectGID, entityLook)
        {
            this.direction = direction;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteSByte(direction);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            direction = reader.ReadSByte();
            if (direction < 0)
                throw new Exception("Forbidden value on direction = " + direction + ", it doesn't respect the following condition : direction < 0");
            

}



}


}
