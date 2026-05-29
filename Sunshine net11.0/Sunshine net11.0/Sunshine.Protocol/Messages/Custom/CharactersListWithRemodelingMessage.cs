using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;
using System;
using System.Collections.Generic;

namespace Sunshine.Protocol.Messages
{
    public class CharactersListWithRemodelingMessage : CharactersListMessage
    {
        public const uint Id = 6550;
        public override uint MessageId => Id;

        public IEnumerable<CharacterToRemodelInformation> charactersToRemodel;

        public CharactersListWithRemodelingMessage()
        {
            charactersToRemodel = Array.Empty<CharacterToRemodelInformation>();
        }

        public CharactersListWithRemodelingMessage(bool hasStartupActions, IEnumerable<CharacterBaseInformations> characters, IEnumerable<CharacterToRemodelInformation> charactersToRemodel)
            : base(hasStartupActions, characters)
        {
            this.charactersToRemodel = charactersToRemodel ?? Array.Empty<CharacterToRemodelInformation>();
        }

        public override void Serialize(IDataWriter writer)
        {
            base.Serialize(writer);

            var entries = charactersToRemodel ?? Array.Empty<CharacterToRemodelInformation>();
            var before = writer.Position;
            short count = 0;
            writer.WriteShort(0);
            foreach (var entry in entries)
            {
                entry.Serialize(writer);
                count++;
            }
            var after = writer.Position;
            writer.Seek((int)before);
            writer.WriteShort(count);
            writer.Seek((int)after);
        }

        public override void Deserialize(IDataReader reader)
        {
            base.Deserialize(reader);
            var limit = reader.ReadShort();
            var values = new CharacterToRemodelInformation[limit];
            for (int i = 0; i < limit; i++)
            {
                values[i] = new CharacterToRemodelInformation();
                values[i].Deserialize(reader);
            }
            charactersToRemodel = values;
        }
    }
}
