
















// Generated on 10/13/2017 02:18:43
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Enums;

namespace Sunshine.Protocol.Messages
{

public class CharacterCreationRequestMessage : Message
{

public const uint Id = 160;
public override uint MessageId
{
    get { return Id; }
}

public string name;
        public sbyte breed;
        public bool sex;
        public IEnumerable<int> colors;
        

public CharacterCreationRequestMessage()
{
}

public CharacterCreationRequestMessage(string name, sbyte breed, bool sex, IEnumerable<int> colors)
        {
            this.name = name;
            this.breed = breed;
            this.sex = sex;
            this.colors = colors;
        }
        

public override void Serialize(IDataWriter writer)
{

writer.WriteUTF(name);
            writer.WriteSByte(breed);
            writer.WriteBoolean(sex);
            foreach (var entry in colors)
            {
                 writer.WriteInt(entry);
            }
            

}

public override void Deserialize(IDataReader reader)
{

name = reader.ReadUTF();
            breed = reader.ReadSByte();
            if (breed < (byte)BreedEnum.Feca || breed > (byte)BreedEnum.Zobal)
                throw new Exception("Forbidden value on breed = " + breed + ", it doesn't respect the following condition : breed < (byte)Enums.BreedEnum.Feca || breed > (byte)Enums.BreedEnum.Zobal");
            sex = reader.ReadBoolean();
            var colors_ = new int[6];
            for (int i = 0; i < 6; i++)
            {
                 colors_[i] = reader.ReadInt();
            }
            colors = colors_;
            

}


}


}
