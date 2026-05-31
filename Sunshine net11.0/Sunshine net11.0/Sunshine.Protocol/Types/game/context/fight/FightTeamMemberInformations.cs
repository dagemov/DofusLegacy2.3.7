
















// Generated on 10/13/2017 02:17:23
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class FightTeamMemberInformations
{

public const short Id = 44;
public virtual short TypeId
{
    get { return Id; }
}

public int id;
        

public FightTeamMemberInformations()
{
}

public FightTeamMemberInformations(int id)
        {
            this.id = id;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteInt(id);
            

}

public virtual void Deserialize(IDataReader reader)
{

id = reader.ReadInt();
            

}



}


}
