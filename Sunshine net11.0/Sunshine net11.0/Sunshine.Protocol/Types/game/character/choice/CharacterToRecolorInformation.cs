
















// Generated on 10/13/2017 02:17:22
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;

namespace Sunshine.Protocol.Types
{

public class CharacterToRecolorInformation
{

public const short Id = 212;
public virtual short TypeId
{
    get { return Id; }
}

public int id;
        public IEnumerable<int> colors;
        

public CharacterToRecolorInformation()
{
}

public CharacterToRecolorInformation(int id, IEnumerable<int> colors)
        {
            this.id = id;
            this.colors = colors;
        }
        

public virtual void Serialize(IDataWriter writer)
{

writer.WriteInt(id);
            var colors_before = writer.Position;
            var colors_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in colors)
            {
                 writer.WriteInt(entry);
                 colors_count++;
            }
            var colors_after = writer.Position;
            writer.Seek((int)colors_before);
            writer.WriteUShort((ushort)colors_count);
            writer.Seek((int)colors_after);

            

}

public virtual void Deserialize(IDataReader reader)
{

id = reader.ReadInt();
            if (id < 0)
                throw new Exception("Forbidden value on id = " + id + ", it doesn't respect the following condition : id < 0");
            var limit = reader.ReadUShort();
            var colors_ = new int[limit];
            for (int i = 0; i < limit; i++)
            {
                 colors_[i] = reader.ReadInt();
            }
            colors = colors_;
            

}



}


}
