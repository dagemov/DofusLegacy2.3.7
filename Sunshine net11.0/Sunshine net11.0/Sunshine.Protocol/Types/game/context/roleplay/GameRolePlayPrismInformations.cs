
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class GameRolePlayPrismInformations : GameRolePlayActorInformations
{

public const short Id = 161;
public override short TypeId
{
    get { return Id; }
}

public Types.ActorAlignmentInformations alignInfos;
        

public GameRolePlayPrismInformations()
{
}

public GameRolePlayPrismInformations(int contextualId, Types.EntityLook look, Types.EntityDispositionInformations disposition, Types.ActorAlignmentInformations alignInfos)
         : base(contextualId, look, disposition)
        {
            this.alignInfos = alignInfos;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            alignInfos.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            alignInfos = new Types.ActorAlignmentInformations();
            alignInfos.Deserialize(reader);
            

}



}


}
