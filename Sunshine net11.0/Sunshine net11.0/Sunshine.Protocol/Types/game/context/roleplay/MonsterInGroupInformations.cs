
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class MonsterInGroupInformations
{

public const short Id = 144;
public virtual short TypeId
{
    get { return Id; }
}

public int creatureGenericId;
        public sbyte grade;
        public Types.EntityLook look;
        

public MonsterInGroupInformations()
{
}

public MonsterInGroupInformations(int creatureGenericId, sbyte grade, Types.EntityLook look)
        {
            this.creatureGenericId = creatureGenericId;
            this.grade = grade;
            this.look = look;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteInt(creatureGenericId);
            writer.WriteSByte(grade);
            look.Serialize(writer);
            

}

public virtual void Deserialize(IDataReader reader)
{

creatureGenericId = reader.ReadInt();
            grade = reader.ReadSByte();
            if (grade < 0)
                throw new Exception("Forbidden value on grade = " + grade + ", it doesn't respect the following condition : grade < 0");
            look = new Types.EntityLook();
            look.Deserialize(reader);
            

}



}


}
