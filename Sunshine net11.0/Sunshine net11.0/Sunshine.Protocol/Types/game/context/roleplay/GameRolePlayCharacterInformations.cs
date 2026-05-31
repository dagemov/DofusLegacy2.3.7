
















// Generated on 10/13/2017 02:17:24
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class GameRolePlayCharacterInformations : GameRolePlayHumanoidInformations
{

public const short Id = 36;
public override short TypeId
{
    get { return Id; }
}

public Types.ActorAlignmentInformations alignmentInfos;
        

public GameRolePlayCharacterInformations()
{
}

public GameRolePlayCharacterInformations(int contextualId, Types.EntityLook look, Types.EntityDispositionInformations disposition, string name, Types.HumanInformations humanoidInfo, Types.ActorAlignmentInformations alignmentInfos)
         : base(contextualId, look, disposition, name, humanoidInfo)
        {
            this.alignmentInfos = alignmentInfos;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            alignmentInfos.Serialize(writer);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            alignmentInfos = new Types.ActorAlignmentInformations();
            alignmentInfos.Deserialize(reader);
            

}



}


}
