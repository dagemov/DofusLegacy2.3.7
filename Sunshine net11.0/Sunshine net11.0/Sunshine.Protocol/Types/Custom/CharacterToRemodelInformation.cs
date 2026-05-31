using Sunshine.Protocol.IO;
using System.Collections.Generic;

namespace Sunshine.Protocol.Types
{
    public class CharacterToRemodelInformation : CharacterRemodelingInformation
    {
        public const short Id = 477;
        public override short TypeId => Id;

        public sbyte possibleChangeMask;
        public sbyte mandatoryChangeMask;

        public CharacterToRemodelInformation()
        {
        }

        public CharacterToRemodelInformation(int id, string name, sbyte breed, bool sex, short cosmeticId, IEnumerable<int> colors, sbyte possibleChangeMask, sbyte mandatoryChangeMask)
            : base(id, name, breed, sex, cosmeticId, colors)
        {
            this.possibleChangeMask = possibleChangeMask;
            this.mandatoryChangeMask = mandatoryChangeMask;
        }

        public override void Serialize(IDataWriter writer)
        {
            base.Serialize(writer);
            writer.WriteSByte(possibleChangeMask);
            writer.WriteSByte(mandatoryChangeMask);
        }

        public override void Deserialize(IDataReader reader)
        {
            base.Deserialize(reader);
            possibleChangeMask = reader.ReadSByte();
            mandatoryChangeMask = reader.ReadSByte();
        }
    }
}
