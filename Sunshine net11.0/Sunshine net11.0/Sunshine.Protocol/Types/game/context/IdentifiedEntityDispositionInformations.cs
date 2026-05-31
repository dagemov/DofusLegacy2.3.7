
















// Generated on 10/13/2017 02:17:22
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class IdentifiedEntityDispositionInformations : EntityDispositionInformations
{

public const short Id = 107;
public override short TypeId
{
    get { return Id; }
}

public int id;
        

public IdentifiedEntityDispositionInformations()
{
}

public IdentifiedEntityDispositionInformations(short cellId, sbyte direction, int id)
         : base(cellId, direction)
        {
            this.id = id;
        }
        

public override void Serialize(IDataWriter writer)
{

base.Serialize(writer);
            writer.WriteInt(id);
            

}

public override void Deserialize(IDataReader reader)
{

base.Deserialize(reader);
            id = reader.ReadInt();
            

}



}


}
