
















// Generated on 10/13/2017 02:17:23
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class GameFightFighterNamedInformations : GameFightFighterInformations
{

public const short Id = 158;
public override short TypeId
{
    get { return Id; }
}

public string name;
        

public GameFightFighterNamedInformations()
{
}

public GameFightFighterNamedInformations(int contextualId, Types.EntityLook look, Types.EntityDispositionInformations disposition, sbyte teamId, bool alive, Types.GameFightMinimalStats stats, string name)
         : base(contextualId, look, disposition, teamId, alive, stats)
        {
            this.name = name;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteUTF(name);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            name = reader.ReadUTF();
            

}



}


}
