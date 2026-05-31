// Generated on 10/13/2017 02:18:42
using System;
using System.Collections.Generic;
using Sunshine.Protocol.IO;
using Sunshine.Protocol.Types;

namespace Sunshine.Protocol.Messages
{
    public class CharactersListWithModificationsMessage : CharactersListMessage
    {
        public const uint Id = 6120;
        public override uint MessageId { get { return Id; } }

        public IEnumerable<Types.CharacterToRecolorInformation> charactersToRecolor;
        public IEnumerable<int> charactersToRename;
        public IEnumerable<int> unusableCharacters;

        public CharactersListWithModificationsMessage()
        {
        }

        public CharactersListWithModificationsMessage(
            bool hasStartupActions,
            IEnumerable<Types.CharacterBaseInformations> characters,
            IEnumerable<Types.CharacterToRecolorInformation> charactersToRecolor,
            IEnumerable<int> charactersToRename,
            IEnumerable<int> unusableCharacters)
            : base(hasStartupActions, characters)
        {
            this.charactersToRecolor = charactersToRecolor ?? new Types.CharacterToRecolorInformation[0];
            this.charactersToRename = charactersToRename ?? new int[0];
            this.unusableCharacters = unusableCharacters ?? new int[0];
        }

        public override void Serialize(IDataWriter writer)
        {
            base.Serialize(writer);

            var charactersToRecolor_before = writer.Position;
            var charactersToRecolor_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in charactersToRecolor ?? new Types.CharacterToRecolorInformation[0])
            {
                if (entry == null)
                    continue;

                writer.WriteShort(entry.TypeId);
                entry.Serialize(writer);
                charactersToRecolor_count++;
            }
            var charactersToRecolor_after = writer.Position;
            writer.Seek((int)charactersToRecolor_before);
            writer.WriteUShort((ushort)charactersToRecolor_count);
            writer.Seek((int)charactersToRecolor_after);

            var charactersToRename_before = writer.Position;
            var charactersToRename_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in charactersToRename ?? new int[0])
            {
                writer.WriteInt(entry);
                charactersToRename_count++;
            }
            var charactersToRename_after = writer.Position;
            writer.Seek((int)charactersToRename_before);
            writer.WriteUShort((ushort)charactersToRename_count);
            writer.Seek((int)charactersToRename_after);

            var unusableCharacters_before = writer.Position;
            var unusableCharacters_count = 0;
            writer.WriteUShort(0);
            foreach (var entry in unusableCharacters ?? new int[0])
            {
                writer.WriteInt(entry);
                unusableCharacters_count++;
            }
            var unusableCharacters_after = writer.Position;
            writer.Seek((int)unusableCharacters_before);
            writer.WriteUShort((ushort)unusableCharacters_count);
            writer.Seek((int)unusableCharacters_after);
        }

        public override void Deserialize(IDataReader reader)
        {
            base.Deserialize(reader);

            var limit = reader.ReadUShort();
            var charactersToRecolor_ = new Types.CharacterToRecolorInformation[limit];
            for (int i = 0; i < limit; i++)
            {
                charactersToRecolor_[i] = Types.ProtocolTypeManager.GetInstance<Types.CharacterToRecolorInformation>(reader.ReadShort());
                charactersToRecolor_[i].Deserialize(reader);
            }
            charactersToRecolor = charactersToRecolor_;

            limit = reader.ReadUShort();
            var charactersToRename_ = new int[limit];
            for (int i = 0; i < limit; i++)
                charactersToRename_[i] = reader.ReadInt();
            charactersToRename = charactersToRename_;

            limit = reader.ReadUShort();
            var unusableCharacters_ = new int[limit];
            for (int i = 0; i < limit; i++)
                unusableCharacters_[i] = reader.ReadInt();
            unusableCharacters = unusableCharacters_;
        }
    }
}
